# TaxSystem

TaxSystem is an event-driven .NET microservice application deployed to Kubernetes through Flux.

The application models a tax processing flow where citizens and companies are registered, companies report salaries, deductions are reported, and tax statements are generated.

## Services

Source code lives under `src/`.

- `TaxSystem.Client`: ASP.NET Core API gateway and public HTTP entry point.
- `TaxSystem.CitizenService`: citizen registration, deregistration, and lookup.
- `TaxSystem.CompanyService`: company registration, updates, and salary reporting.
- `TaxSystem.BankService`: bank transfer scheduling and lookup.
- `TaxSystem.StatementGeneratorService`: tax statement generation.
- `TaxSystem.Shared`: shared domain models, MassTransit contracts, RabbitMQ setup, and PostgreSQL helpers.

## Messaging

Services communicate through MassTransit over RabbitMQ.

- Message contracts are defined in `src/TaxSystem.Shared/Messaging/Contracts/`.
- Services register RabbitMQ through `AddTaxSystemRabbitMq`.
- Consumers are auto-registered from each service assembly.
- RabbitMQ credentials are supplied through environment variables and Kubernetes secrets.

## Persistence

The deployed persistence layer is PostgreSQL managed by CloudNativePG.

- The database cluster is defined in `postgres-cluster.yaml`.
- Service connection strings are defined in `postgres-credentials.yaml`.
- Write traffic uses `taxsystem-db-rw` through `TAXSYSTEM_DB_CONNECTION_WRITE`.
- Read traffic uses `taxsystem-db-ro` through `TAXSYSTEM_DB_CONNECTION_READ`.
- Each service owns its own database: `citizen_db`, `company_db`, `bank_db`, and `statement_db`.

Each service creates its tables on startup with Entity Framework `EnsureCreated`. If the CloudNativePG cluster is reset, restart the services after the database is ready.

## Kubernetes

Manifests in this directory are the production/GitOps source of truth.

- `client.yaml`: client Deployment and Service.
- `citizen-service.yaml`: CitizenService StatefulSet and Services.
- `company-service.yaml`: CompanyService StatefulSet and Services.
- `bank-service.yaml`: BankService StatefulSet and Services.
- `statementgenerator-service.yaml`: StatementGeneratorService StatefulSet and Services.
- `postgres-cluster.yaml`: CloudNativePG PostgreSQL cluster.
- `postgres-credentials.yaml`: Kubernetes secrets for PostgreSQL credentials and connection strings.
- `network-policies.yaml`: RabbitMQ and PostgreSQL ingress restrictions.
- `ingress.yaml`: Traefik ingress for `taxsystem.kvikit.dk`.
- `kustomization.yaml`: Kustomize entry point for TaxSystem.

## Local Development

Run commands from `src/`.

Build:

```bash
dotnet build TaxSystem.sln
```

Run service-level tests:

```bash
dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj
```

Run the full local Minikube test flow:

```bash
./run-tests.sh
```

The test runner builds and publishes services, builds local container images, deploys the TaxSystem stack to Minikube, waits for RabbitMQ/PostgreSQL/application readiness, and runs E2E tests.

## E2E Test Configuration

Direct E2E test execution requires a running stack and these environment variables:

```bash
export CLIENT_BASE_URL=http://localhost:8080
export RABBITMQ_HOST=localhost
export RABBITMQ_PORT=5672
export RABBITMQ_USERNAME=taxsystem
export RABBITMQ_PASSWORD=taxsystem-dev

dotnet test TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj
```

In normal local and CI workflows, prefer `./run-tests.sh` because it prepares the Kubernetes environment automatically.

## Deployment

Images are built by GitHub Actions and pushed to GitHub Container Registry. The workflow updates the image tags in these manifests and commits the result to `main`; Flux then reconciles the changes into the cluster.

Force a reconciliation manually:

```bash
flux reconcile kustomization applications -n flux-system --with-source
```

Check the deployed application:

```bash
kubectl -n taxsystem get deployments,statefulsets,pods,svc,ingress
kubectl -n taxsystem get cluster taxsystem-db
```
