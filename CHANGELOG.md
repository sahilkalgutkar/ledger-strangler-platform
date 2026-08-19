# Changelog

## v0.1.0

Initial release: the legacy monolith, the YARP strangler facade, and the first two services strangled off it.

- `LegacyMonolith` - Accounts and Statements in one ASP.NET Core process against Postgres
- `Gateway` - YARP-based strangler facade routing per-path to legacy or new services
- `AccountsService` - Cassandra-backed, lightweight-transaction balance updates, publishes to RabbitMQ
- `NotificationsService` - consumes `AccountBalanceChangedEvent`, the first behavior that only exists because of the migration
- Centralized logging via Serilog, Filebeat, Logstash, Elasticsearch, and Kibana
- Terraform for the target Azure environment (AKS, ACR, Log Analytics)
- Kubernetes manifests and an ArgoCD `Application` for GitOps deploys
- CI via GitHub Actions with Codecov coverage reporting
