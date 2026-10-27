# Amazon RDS root certificate authorities (eu-north-1)

These are the root CAs that sign Amazon RDS database server certificates in **Europe (Stockholm), `eu-north-1`**. `server/Dockerfile` adds them to the container's system trust store, so the API can connect to RDS SQL Server with `Encrypt=True` and full certificate validation (no `TrustServerCertificate`).

| File | CA identifier | Common name | Valid until (UTC) | SHA-256 fingerprint |
| --- | --- | --- | --- | --- |
| `eu-north-1-rds-ca-rsa2048-g1.crt` | `rds-ca-rsa2048-g1` (the RDS default for new instances) | Amazon RDS eu-north-1 Root CA RSA2048 G1 | 2061-05-24 | `4A:3E:1F:8C:D4:CC:FA:64:38:3F:FD:81:34:4D:EF:E6:9D:97:C6:61:DB:DE:51:A7:0D:AD:C0:B1:F6:88:56:1B` |
| `eu-north-1-rds-ca-rsa4096-g1.crt` | `rds-ca-rsa4096-g1` | Amazon RDS eu-north-1 Root CA RSA4096 G1 | 2121-05-24 | `A9:18:30:F1:06:8B:9F:ED:F5:A3:E1:38:05:E0:EF:AF:DC:6D:AB:2C:49:03:EB:03:2F:52:88:4F:EE:21:10:A7` |
| `eu-north-1-rds-ca-ecc384-g1.crt` | `rds-ca-ecc384-g1` | Amazon RDS eu-north-1 Root CA ECC384 G1 | 2121-05-24 | `7E:0F:13:14:7F:65:B8:45:26:8F:62:C4:46:F2:78:50:40:A0:07:F8:E9:2F:B0:E4:29:87:E9:B8:48:06:8D:A8` |

## Source

Downloaded on 2026-10-27 from the regional bundle AWS documents in [Using SSL/TLS to encrypt a connection to a DB instance](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html):

```
https://truststore.pki.rds.amazonaws.com/eu-north-1/eu-north-1-bundle.pem
```

The bundle held exactly these three certificates, all self-signed roots. AWS advises registering only root CAs, never intermediates, because RDS rotates the server certificates it issues from them. Each certificate is stored in its own `.crt` file because `update-ca-certificates` expects one certificate per file.

## Updating

- **Another region:** replace the files with that region's bundle (same URL pattern), split one certificate per `.crt` file.
- **Checking a file:** `openssl x509 -in <file> -noout -subject -issuer -dates -fingerprint -sha256`.
- These roots are valid for decades and RDS rotates the server certificates beneath them automatically, so routine updates aren't expected. If AWS publishes new roots, add them alongside these.
