# AWS deployment runbook

Architecture and costs: [ADR-016](../docs/adr/ADR-016-host-on-aws-with-cloudfront-ecs-fargate-and-rds.md). Run from the repository root in Git Bash, signed in to the AWS CLI. Deployment is from this machine: the account's service control policies deny creating IAM OIDC providers, so GitHub Actions can't assume a role.

```bash
export AWS_REGION=eu-north-1 AWS_PAGER="" MSYS_NO_PATHCONV=1   # stops Git Bash rewriting /movielogger/... into Windows paths
ACCOUNT=845322645179
REG=$ACCOUNT.dkr.ecr.$AWS_REGION.amazonaws.com
TAG=$(git rev-parse --short HEAD)
gen() { node -e "process.stdout.write(require('crypto').randomBytes(24).toString('base64url'))"; }
out() { aws cloudformation list-exports --query "Exports[?Name=='movielogger:$1'].Value" --output text; }
```

## 1. Budget (free; Budgets is global, created through us-east-1)

```bash
aws budgets create-budget --region us-east-1 --account-id $ACCOUNT --budget file://infra/budget/budget.json \
  --notifications-with-subscribers "$(sed 's/ALERT_EMAIL/<your email>/' infra/budget/notifications.json)"
```

## 2. Base stack and secrets (nothing billed by the hour)

```bash
aws cloudformation deploy --stack-name movielogger-base --template-file infra/cloudformation/base.yml \
  --parameter-overrides CloudFrontPrefixListId=pl-fab65393
aws ssm put-parameter --name /movielogger/db-admin-password --type SecureString --value="$(gen)"
aws ssm put-parameter --name /movielogger/db-app-password --type SecureString --value="$(gen)"
aws ssm put-parameter --name /movielogger/jwt-key --type SecureString \
  --value="$(node -e "process.stdout.write(require('crypto').randomBytes(48).toString('base64'))")"
```

## 3. Images

```bash
aws ecr get-login-password | docker login --username AWS --password-stdin $REG
docker build -t $REG/movielogger-api:$TAG -f server/Dockerfile server && docker push $REG/movielogger-api:$TAG
docker build -t $REG/movielogger-migrator:$TAG -f database/Dockerfile . && docker push $REG/movielogger-migrator:$TAG
```

## 4. Database (starts billing, ~15-25 minutes)

```bash
aws cloudformation deploy --stack-name movielogger-database --template-file infra/cloudformation/database.yml --capabilities CAPABILITY_IAM
APP_PW=$(aws ssm get-parameter --name /movielogger/db-app-password --with-decryption --query Parameter.Value --output text)
aws ssm put-parameter --name /movielogger/api-connection-string --type SecureString \
  --value="Server=$(out DatabaseEndpoint),1433;Database=MovieLoggerDb;User Id=movielogger_app;Password=$APP_PW;Encrypt=True"
unset APP_PW
```

## 5. Application with the API scaled to 0, migrate, then scale to 1

```bash
aws cloudformation deploy --stack-name movielogger-app --template-file infra/cloudformation/app.yml --capabilities CAPABILITY_IAM \
  --parameter-overrides ApiImageTag=$TAG MigratorImageTag=$TAG ApiDesiredCount=0

TASK=$(aws ecs run-task --cluster movielogger --launch-type FARGATE --task-definition movielogger-migrator \
  --network-configuration "awsvpcConfiguration={subnets=[$(out PublicSubnetIds)],securityGroups=[$(out MigratorSecurityGroupId)],assignPublicIp=ENABLED}" \
  --query "tasks[0].taskArn" --output text)
aws ecs wait tasks-stopped --cluster movielogger --tasks $TASK
aws ecs describe-tasks --cluster movielogger --tasks $TASK --query "tasks[0].containers[0].exitCode"   # must be 0
aws logs tail /movielogger/migrator --since 30m

aws cloudformation deploy --stack-name movielogger-app --template-file infra/cloudformation/app.yml --capabilities CAPABILITY_IAM \
  --parameter-overrides ApiDesiredCount=1
```

## 6. Frontend

```bash
(cd client && npm ci && VITE_API_BASE_URL= npm run build)
aws s3 sync client/dist s3://$(out SiteBucketName)
DIST=$(aws cloudformation describe-stacks --stack-name movielogger-app --query "Stacks[0].Outputs[?OutputKey=='DistributionId'].OutputValue" --output text)
aws cloudfront create-invalidation --distribution-id $DIST --paths "/index.html"
SITE=$(aws cloudformation describe-stacks --stack-name movielogger-app --query "Stacks[0].Outputs[?OutputKey=='SiteUrl'].OutputValue" --output text)
curl -s -o /dev/null -w "%{http_code}\n" $SITE/ ; curl -s $SITE/api/genres | head -c 200
```

## Redeploying the API

Build and push with a new `TAG`, run the migration task again if there are new migrations, then
`aws cloudformation deploy ... --parameter-overrides ApiImageTag=$TAG MigratorImageTag=$TAG`.

## Teardown

Nothing below is deleted until the backup has been restored and checked locally.

```bash
# 1. Native backup to the backup bucket, run as the admin from inside the VPC
BACKUP_BUCKET=$(aws cloudformation describe-stacks --stack-name movielogger-database --query "Stacks[0].Outputs[?OutputKey=='BackupBucketName'].OutputValue" --output text)
dbadmin() {
  local task=$(aws ecs run-task --cluster movielogger --launch-type FARGATE --task-definition movielogger-dbadmin \
    --network-configuration "awsvpcConfiguration={subnets=[$(out PublicSubnetIds)],securityGroups=[$(out MigratorSecurityGroupId)],assignPublicIp=ENABLED}" \
    --overrides "$(node -e 'console.log(JSON.stringify({containerOverrides:[{name:"sqlcmd",command:["-N","mandatory","-b","-d","master","-Q",process.argv[1]]}]}))' "$1")" \
    --query "tasks[0].taskArn" --output text)
  aws ecs wait tasks-stopped --cluster movielogger --tasks $task
  aws logs tail /movielogger/migrator --since 10m --filter-pattern "" | tail -20
}
dbadmin "exec msdb.dbo.rds_backup_database @source_db_name='MovieLoggerDb', @s3_arn_to_backup_to='arn:aws:s3:::$BACKUP_BUCKET/MovieLoggerDb.bak', @type='FULL'"
dbadmin "exec msdb.dbo.rds_task_status @db_name='MovieLoggerDb'"   # repeat until lifecycle is SUCCESS

# 2. Restore locally under another name and compare row counts with the live site
aws s3 cp s3://$BACKUP_BUCKET/MovieLoggerDb.bak ./MovieLoggerDb-rds.bak
#   RESTORE DATABASE MovieLoggerDb_RdsCheck FROM DISK = '<full path>\MovieLoggerDb-rds.bak' WITH MOVE ... (see RESTORE FILELISTONLY)
#   then SELECT COUNT(*) from Users, Movies and MovieWatches, and keep the .bak file somewhere safe
```

3. Only after that, and with explicit approval: scale the API to 0, empty the site bucket, delete `movielogger-app`, turn off deletion protection (`aws rds modify-db-instance --db-instance-identifier <id> --no-deletion-protection --apply-immediately`), delete `movielogger-database` (this takes a final snapshot; delete it once the local restore is confirmed, as it keeps billing), delete `movielogger-base`, the backup bucket (retained by the stack), the `/movielogger/*` parameters and the budget.
