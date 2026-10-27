# Migration image: creates the database and the API's login if needed, then runs Flyway. Build from the
# repository root (the context needs database/ and server/certs/), filtered by database/Dockerfile.dockerignore:
#   docker build -t movielogger-migrator -f database/Dockerfile .
# (or `make docker-build-migrator`). See docs/migrations.md for how to run it.

# go-sqlcmd runs the bootstrap script. The Flyway image has no curl/tar/bzip2, so it's fetched and checked
# here and only the binary is copied across. The checksum is GitHub's published digest for this asset.
FROM alpine:3.22 AS sqlcmd
ARG SQLCMD_VERSION=1.10.0
ARG SQLCMD_SHA256=92516d98c63d99b0994de5b61350c91f6915f9b76f139a59039fbcb225c2e987
ADD --checksum=sha256:${SQLCMD_SHA256} \
    https://github.com/microsoft/go-sqlcmd/releases/download/v${SQLCMD_VERSION}/sqlcmd-linux-amd64.tar.bz2 /tmp/sqlcmd.tar.bz2
RUN tar -xjf /tmp/sqlcmd.tar.bz2 -C /tmp sqlcmd

# Same Flyway version as the CLI used for local development (docs/migrations.md).
FROM flyway/flyway:13.7.0

COPY --from=sqlcmd /tmp/sqlcmd /usr/local/bin/sqlcmd

# Trust the Amazon RDS root CAs (server/certs/aws-rds/README.md) so both tools connect with full certificate
# validation: the JDBC driver uses the JRE trust store, and go-sqlcmd uses the PEM file named by SSL_CERT_FILE,
# which contains only the RDS roots.
COPY server/certs/aws-rds/*.crt /migrator/certs/
RUN for cert in /migrator/certs/*.crt; do \
        "$JAVA_HOME/bin/keytool" -importcert -noprompt -cacerts -storepass changeit \
            -alias "$(basename "$cert" .crt)" -file "$cert"; \
    done \
    && cat /migrator/certs/*.crt > /migrator/certs/rds-roots.pem
ENV SSL_CERT_FILE=/migrator/certs/rds-roots.pem \
    REDGATE_DISABLE_TELEMETRY=true

COPY database/migrations/ /flyway/sql/
COPY database/migrator/bootstrap.sql database/migrator/migrate.sh /migrator/
RUN chmod 0555 /migrator/migrate.sh

# Run as an unprivileged user; the image only reads its files.
RUN useradd --system --uid 10001 --no-create-home --home-dir /nonexistent migrator
USER 10001

ENTRYPOINT ["/migrator/migrate.sh"]
