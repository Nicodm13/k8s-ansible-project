# AGENTS.md - TaxSystem Development Guide

## Architecture Overview

TaxSystem is an event-driven .NET microservice system for tax processing.

- `TaxSystem.Client`: ASP.NET Core API gateway and public HTTP entry point.
- `TaxSystem.CitizenService`: citizen registration, deregistration, and lookup.
- `TaxSystem.CompanyService`: company registration, updates, and salary reporting.
- `TaxSystem.BankService`: bank transfer scheduling and lookup.
- `TaxSystem.StatementGeneratorService`: deductible reporting and tax statement generation.
- `TaxSystem.Shared`: shared domain models, MassTransit contracts, RabbitMQ setup, and PostgreSQL registration helpers.

Services communicate through typed MassTransit contracts over RabbitMQ. Service data is persisted in PostgreSQL databases managed by CloudNativePG in Kubernetes.

## Current Runtime Model

Production/GitOps manifests live one directory above this source tree in `applications/TaxSystem/`.

- `client` is a Kubernetes `Deployment`.
- `citizen-service`, `company-service`, `bank-service`, and `statementgenerator-service` are Kubernetes `StatefulSet` workloads.
- RabbitMQ is installed by the Flux-managed HelmRelease under `infrastructure/messaging/rabbitmq`.
- PostgreSQL is managed by the CloudNativePG `Cluster` named `taxsystem-db`.
- Traefik exposes the client through `taxsystem.kvikit.dk`.

The application images are built by GitHub Actions, pushed to GitHub Container Registry, and written back into the Kubernetes manifests for Flux to deploy.

## Messaging Rules

Messaging is based on MassTransit.

- Contracts live in `TaxSystem.Shared/Messaging/Contracts/` as public sealed records.
- Services call `builder.Services.AddTaxSystemRabbitMq(builder.Configuration)` in `Program.cs`.
- Consumer classes implementing `IConsumer<TContract>` are auto-registered from the entry assembly.
- Production and E2E tests use RabbitMQ.
- Service-level tests may use MassTransit in-memory transport when infrastructure should not be required.

Do not reintroduce the old `Event` class, topic/argument arrays, `IMessageQueue`, `MessageQueueSync`, `MessageQueueAsync`, or `RabbitMqQueue` abstractions.

RabbitMQ configuration:

- Non-secret defaults are read from `appsettings.json` under `RabbitMq`.
- Credentials must come from `RABBITMQ_USERNAME` and `RABBITMQ_PASSWORD`.
- Do not hardcode RabbitMQ credentials in C# code or appsettings files.

Example contract:

```csharp
namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record CitizenRegistered(string Cpr, string Name);
```

## Persistence Rules

The current deployed persistence path is PostgreSQL through Entity Framework Core.

- Shared registration lives in `TaxSystem.Shared/Persistance/PostgresRegistration.cs`.
- Services call `builder.Services.AddTaxSystemPostgres<TDbContext>()`.
- Each service owns its own PostgreSQL database.
- CloudNativePG creates the databases in `applications/TaxSystem/postgres-cluster.yaml`.
- Connection strings are supplied through Kubernetes secrets and environment variables.

Connection environment variables:

- `TAXSYSTEM_DB_CONNECTION_WRITE`: write connection, routed to `taxsystem-db-rw`.
- `TAXSYSTEM_DB_CONNECTION_READ`: read connection, routed to `taxsystem-db-ro`.
- `TAXSYSTEM_DB_CONNECTION`: fallback for single-connection local/dev setups.

Repository convention:

- Keep separate read/write interfaces, for example `IReadCitizenRepository` and `IWriteCitizenRepository`.
- PostgreSQL repository implementations are the active persistence path.
- `FileSystemRepository` still exists as shared utility/test-era code, but do not make it the production persistence path again.

Each service calls `Database.EnsureCreatedAsync()` on startup. After resetting the CloudNativePG cluster, restart the services so they recreate their tables.

## Critical Data Flows

Report salary:

- Client receives a salary report request.
- Client publishes or requests typed MassTransit messages.
- CompanyService records salary information.
- StatementGeneratorService consumes tax information and uses it for statement generation.

Request statement:

- Client requests a statement for a citizen.
- StatementGeneratorService gathers stored tax information.
- If enough information exists, a statement is generated and returned through the request flow.

Register citizen/company:

- Client receives HTTP requests.
- The relevant service consumes typed registration messages.
- The service persists data in its PostgreSQL database.

## Testing Strategy

`TaxSystem.Tests` provides service-level, fast feedback tests.

- Uses Reqnroll for BDD scenarios.
- Feature files live under `TaxSystem.Tests/Features/`.
- Step definitions use `ScenarioContext` for scenario state.
- Run with `dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj`.

`TaxSystem.Tests.E2E` provides full-stack tests.

- Uses HTTP requests against `TaxSystem.Client`.
- Requires a running client endpoint and RabbitMQ.
- In normal local/CI use, run `./run-tests.sh` instead of invoking E2E tests directly.
- Direct E2E execution uses `CLIENT_BASE_URL`, `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_USERNAME`, and `RABBITMQ_PASSWORD`.

## Local Commands

Run from `applications/TaxSystem/src`.

```bash
dotnet build TaxSystem.sln
dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj
./run-tests.sh
```

`run-tests.sh` builds and publishes services, runs service-level tests, deploys the application stack to Minikube, waits for PostgreSQL/RabbitMQ/application readiness, and then runs E2E tests.

The script must run from a Linux filesystem, not a Windows-mounted WSL path, because .NET and Docker need Unix file permissions.

## Docker And Images

The service Dockerfiles expect pre-published output under `publish/<ProjectName>/`.

The normal build flow is:

1. `dotnet build TaxSystem.sln --configuration Release`
2. `dotnet publish ... -o publish/<ProjectName> /p:UseAppHost=false`
3. `docker build -f <Project>/Dockerfile -t <image> .`

Do not assume the Dockerfiles perform a full SDK build inside the image. They copy published output into .NET runtime/ASP.NET runtime images.

## Key File Locations

| Purpose | File |
|---------|------|
| Solution | `TaxSystem.sln` |
| Messaging setup | `TaxSystem.Shared/Messaging/MassTransitRabbitMqRegistration.cs` |
| Message contracts | `TaxSystem.Shared/Messaging/Contracts/` |
| RabbitMQ options | `TaxSystem.Shared/Messaging/RabbitMqOptions.cs` |
| PostgreSQL registration | `TaxSystem.Shared/Persistance/PostgresRegistration.cs` |
| Domain models | `TaxSystem.Shared/Models/` |
| Production manifests | `../*.yaml` from this directory |
| Local/CI test runner | `run-tests.sh` |
| GitHub workflows | repo-root `.github/workflows/` |

## Common Pitfalls

1. Do not change Kubernetes StatefulSet immutable fields casually. Existing StatefulSets may need explicit recreation if fields outside the allowed update set change.
2. Do not use localhost for service-to-service communication inside Kubernetes. Use Kubernetes service names.
3. Do not hardcode RabbitMQ or PostgreSQL credentials in C# code or appsettings files.
4. Keep Reqnroll step regexes aligned with the exact feature-file text.
5. Keep test state in `ScenarioContext`, not static fields.
6. Keep production changes in Git manifests so Flux can reconcile them.
7. If CloudNativePG is reset, restart the TaxSystem services so EF can recreate tables.
