# ADR-016: Host on AWS with CloudFront, ECS Fargate and RDS SQL Server

## Status
Accepted. The AWS infrastructure is not yet provisioned. Refines the hosting assumption in [ADR-015](ADR-015-package-the-api-as-a-container-configured-through-the-environment.md), which expected the frontend on AWS Amplify on a separate origin.

## Context
MovieLogger is being deployed to AWS for about two weeks, as a working demo and to practise containers, managed databases, networking, IAM and CI/CD. Then it will be torn down. The constraints that shaped the design:

- **Cost.** The account is on the AWS Free plan with $100 of credits. On that plan AWS doesn't charge the payment method; if the credits run out the account closes instead. The design should use a small fraction of the credits and avoid anything that bills by the hour without being needed (NAT gateways, interface VPC endpoints, idle public IPv4 addresses).
- **Account restrictions.** The account was created through AWS's newer sign-up experience, so AWS-managed service control policies apply. Only `us-east-1`, `us-west-2` and the selected region (`eu-north-1`, Stockholm) can be used. Services such as Organizations and Transit Gateway are blocked. ECS (including Fargate), ECR, RDS, Elastic Load Balancing, EC2/VPC, CloudFront, S3, CloudWatch Logs, Systems Manager, KMS, CloudFormation and IAM are allowed.
- **No custom domain.** Buying one isn't worth it for two weeks, so HTTPS has to work on AWS-provided hostnames.
- **Keep SQL Server.** The application, its Flyway migrations (T-SQL) and its integration tests ([ADR-014](ADR-014-use-a-dedicated-sql-server-database-for-api-integration-tests.md)) all target SQL Server.
- **The database must not be reachable from the internet.**

## Decision
All resources are in `eu-north-1` except CloudFront, which is global:

```
Browser --HTTPS--> CloudFront (https://<id>.cloudfront.net, AWS-provided certificate)
                     /*      -> S3 bucket (private; React build; origin access control)
                     /api/*  -> VPC origin -> internal Application Load Balancer (HTTP)
                                               -> ECS Fargate task, 0.25 vCPU / 0.5 GB, port 8080
                                                    -> RDS for SQL Server Express (db.t3.micro, single-AZ, private)
```

- **One origin for the site and the API.** CloudFront serves the React build from S3 and forwards `/api/*` to the load balancer. The browser only ever talks to `<id>.cloudfront.net`, so the frontend is built with an empty `VITE_API_BASE_URL` and the API needs no CORS configuration. CloudFront provides HTTPS on its own hostname, which solves the no-domain problem.
- **CloudFront reaches the load balancer through a VPC origin**, so the load balancer is internal and has no public address. A load balancer is needed at all because a Fargate task's IP changes on every deployment, and CloudFront needs a stable origin with health checks behind it.
- **ECS on Fargate, one task, in a public subnet with a public IP.** The public IP is only for outbound traffic (pulling the image from ECR, sending logs, reading parameters). The task's security group accepts port 8080 only from the load balancer's security group. This avoids a NAT gateway or interface VPC endpoints, either of which would cost more than the task itself.
- **RDS for SQL Server Express**, single-AZ, 20 GB gp3, 1-day automated backups, in private subnets. It accepts port 1433 only from the API and migration tasks' security groups. `rds.force_ssl=1` makes it refuse unencrypted connections. The API connects with `Encrypt=True` and full certificate validation: the image trusts the Amazon RDS `eu-north-1` root CAs, committed in `server/certs/aws-rds`.
- **Configuration and secrets.** The connection string and JWT key are SecureString parameters in SSM Parameter Store, injected into the task by ECS. The other settings are plain task environment variables, including `ForwardedHeaders__ForwardLimit=2` (CloudFront then the load balancer).
- **Migrations** run as a separate one-off ECS task running Flyway inside the VPC, as a gated deployment step. The API still never migrates the database ([ADR-012](ADR-012-use-flyway-for-schema-migrations.md)).
- **Logs** go to CloudWatch Logs with 7-day retention.
- **Infrastructure as code** uses CloudFormation, so deleting the stacks is most of the teardown.
- **Abuse protection** for a public demo: per-client-IP rate limiting, a `Registration:Enabled` switch, and only a movie's creator can edit or delete it.

### Alternatives considered
- **AWS Amplify for the frontend** (ADR-015's original plan). It would work, but it puts the site and the API on different origins, which needs CORS. S3 plus CloudFront also covers the API under one HTTPS hostname.
- **A public load balancer**, restricted to CloudFront's address ranges. Simpler to configure, but the load balancer would have public IPs (billed) and be addressable from the internet. A VPC origin removes both problems at no extra charge.
- **No load balancer: API Gateway HTTP API with a VPC link and Cloud Map.** About $8 cheaper over two weeks, but more moving parts and a less common pattern for ECS. Not worth the saving.
- **Private subnets for the task with NAT or VPC endpoints.** More secure in principle, but a NAT gateway or four interface endpoints across two Availability Zones would cost more than everything else combined. Security groups give the needed isolation.
- **Secrets Manager.** $0.40 per secret per month. It offers rotation, which a two-week demo doesn't need. Parameter Store SecureStrings are free.
- **RDS for PostgreSQL.** About $4 cheaper over two weeks, but it would need a new EF Core provider, rewritten migrations and a new test setup.

## Consequences
- **Estimated cost** is about $26 for 14 days (about $1.85 a day) at on-demand `eu-north-1` prices from October 2026: the database ~$10.85 including storage, the load balancer ~$8.35, Fargate ~$4.60, the task's public IPv4 address ~$1.70, and under $0.50 for ECR, S3, logs and parameters. CloudFront traffic falls within its always-free allowance. The load balancer and the database storage bill for as long as they exist, even if idle or stopped, so they must be deleted, not stopped.
- **No high availability.** A single task in one Availability Zone and a single-AZ database. A restart or deployment can briefly interrupt the demo. Rate limits are held in memory per task.
- **The API sees `http` as the request scheme**, because the load balancer reports its own HTTP listener. CloudFront redirects viewers to HTTPS. `ASPNETCORE_HTTPS_PORT` must stay unset, or the API would redirect requests that CloudFront already received over HTTPS, in a loop.
- **Trusting forwarded headers from any proxy** (ADR-015) depends on the network rules: the task accepts traffic only from the load balancer, and the load balancer only from CloudFront.
- **CI keeps working after teardown.** Builds and tests run on GitHub's runners with a throwaway SQL Server container; only the deployment workflow needs AWS.
- **Teardown:** empty the S3 buckets, delete the CloudFormation stacks, then delete the Parameter Store parameters, which CloudFormation can't create as SecureStrings and so are made separately. Before deleting the database, take a native SQL Server backup and check that it restores locally. Without a snapshot or backup, deleting the RDS instance loses its data permanently.
