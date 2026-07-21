# AGENTS.md - TaxSystem Development Guide

## Architecture Overview

**TaxSystem** is an event-driven microservices architecture student project for tax processing:
- **Client** (ASP.NET Core): API gateway and entry point; publishes commands or routes requests into the service flow
- **Microservices**: CitizenService, CompanyService, BankService, StatementGeneratorService
- **TaxSystem.Shared**: Central models, typed MassTransit contracts, RabbitMQ configuration, and filesystem persistence helper
- **Communication**: MassTransit over RabbitMQ using typed command/event records from `TaxSystem.Shared/Messaging/Contracts`
- **Persistence**: Temporary JSON filesystem persistence through `TaxSystem.Shared.Persistance.FileSystemRepository`

## Critical Data Flows

1. **Report Salary** (ReportSalary.feature):
   - Client receives salary report (company CVR, employee CPR, amount)
   - Publishes or routes a typed salary-reporting command/event
   - CompanyService and StatementGeneratorService participate in the statement generation flow

2. **Request Statement** (RequestStatement.feature):
   - Client requests tax statement for citizen
   - Services aggregate data, generate final statement

**Key**: Inter-service communication uses typed MassTransit records. Do not reintroduce the old `Event` class, topic/argument arrays, or `IMessageQueue` abstraction.

## Messaging Pattern - Know This

Messaging is based on MassTransit:
- Contracts live in `TaxSystem.Shared/Messaging/Contracts/` as public sealed records.
- Services call `builder.Services.AddTaxSystemRabbitMq(builder.Configuration)` in `Program.cs`.
- Consumer classes implementing `IConsumer<TContract>` are auto-registered from the entry assembly by the shared MassTransit registration.
- Tests can use MassTransit in-memory transport for service-level message tests.
- Production and E2E messaging uses RabbitMQ.

RabbitMQ configuration:
- Non-secret config is read from `appsettings.json` under `RabbitMq` (`Host`, `Port`, `VirtualHost`).
- Credentials must come from environment variables: `RABBITMQ_USERNAME` and `RABBITMQ_PASSWORD`.
- Do not hardcode RabbitMQ credentials in code or appsettings.

Example contract:
```csharp
namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record CitizenRegistered(string Cpr, string Name);
```

## Testing Strategy

**TaxSystem.Tests** (in-process, fast feedback):
- Uses Reqnroll for BDD scenarios
- `SharedStepDefinitions.cs`: Common "Given" steps (company setup, employee setup)
- Feature-specific step files: ReportSalaryStepDefinitions.cs, RequestStatementStepDefinitions.cs
- Test state shared via `ScenarioContext` across step classes
- Uses MassTransit in-memory transport for message tests where needed
- Repository persistence tests use temporary filesystem folders and `FileSystemRepository`
- Run: `dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj`

**TaxSystem.Tests.E2E** (full stack, requires Minikube):
- HTTP requests to Client API gateway
- Real RabbitMQ broker through MassTransit
- Requires environment variables: CLIENT_BASE_URL, RABBITMQ_HOST/PORT/USERNAME/PASSWORD
- Run: `dotnet test TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj`

## Kubernetes & Docker

**Pattern**: Multi-stage builds (SDK → build → runtime-only final image):
- Base image: `mcr.microsoft.com/dotnet/runtime:10.0` (services) or `aspnet:10.0` (Client)
- Production/GitOps manifests live in `applications/TaxSystem/`; the test runner renders them with Minikube-local image overrides when running E2E tests.
- Services discovered via Kubernetes DNS: `{service-name}` (e.g., `citizen-service:5000`)
- RabbitMQ runs as service `rabbitmq`; in cluster deployments the host is usually `rabbitmq.taxsystem.svc.cluster.local`
- Persistent service data is mounted at `/var/lib/taxsystem` with `TAXSYSTEM_DATA_PATH=/var/lib/taxsystem`

## Service Structure Conventions

All service projects follow this layout:
```
TaxSystem.{ServiceName}/
├── Program.cs              # Host setup, MassTransit, repository DI
├── Repositories/           # Data persistence layer
│   ├── IRead{Entity}Repository.cs
│   ├── IWrite{Entity}Repository.cs
│   └── {Entity}Repository.cs
├── Services/               # Business logic
│   └── {Service}.cs
└── Dockerfile              # Multi-stage build
```

Repository pattern:
- Each service has separate read/write interfaces.
- One concrete repository can implement both interfaces.
- Repositories currently wrap `FileSystemRepository` and store JSON under service-specific subdirectories such as `citizens`, `companies`, `bank-transfers`, and `statements`.
- This is temporary persistence and should be replaceable later by DI with PostgreSQL/CloudNativePG-backed implementations.

## Build & Deployment Commands

```bash
# Build all projects
dotnet build TaxSystem.sln

# Run service-level tests (no infrastructure)
dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj

# Run E2E tests (requires Minikube + services running)
export CLIENT_BASE_URL=http://localhost:8080
export RABBITMQ_HOST=localhost
export RABBITMQ_PORT=5672
export RABBITMQ_USERNAME=guest
export RABBITMQ_PASSWORD=guest
dotnet test TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj

# Deploy to Minikube using the root manifests with local-image overrides
./run-tests.sh --skip-unit
```

## Project Dependencies & Versions

- **.NET 10.0** (latest LTS)
- **Reqnroll 3.3.4**: BDD test framework (Gherkin)
- **MassTransit.RabbitMQ**: RabbitMQ integration through MassTransit
- **NUnit 4.5.1**: Test runner
- **ASP.NET Core 10.0**: Client gateway

## Key File Locations

| Purpose | File |
|---------|------|
| Messaging setup | `TaxSystem.Shared/Messaging/MassTransitRabbitMqRegistration.cs` |
| Message contracts | `TaxSystem.Shared/Messaging/Contracts/` |
| RabbitMQ options | `TaxSystem.Shared/Messaging/RabbitMqOptions.cs` |
| Filesystem persistence | `TaxSystem.Shared/Persistance/FileSystemRepository.cs` |
| Domain models | `TaxSystem.Shared/Models/` |
| Test workflows | `TaxSystem.Tests/Features/` (ReportSalary.feature, RequestStatement.feature) |
| K8s deployments | `../` from `src` (`applications/TaxSystem/*.yaml`) |
| CI pipeline | repo-root `.github/workflows/` |
| Test config | `TESTING.md` (comprehensive test documentation) |

## Common Pitfalls

1. **Old messaging abstractions**: Do not use or recreate `Event`, `IMessageQueue`, `MessageQueueSync`, `MessageQueueAsync`, or `RabbitMqQueue`. Use MassTransit typed contracts.
2. **RabbitMQ secrets**: Keep username/password in `RABBITMQ_USERNAME` and `RABBITMQ_PASSWORD`, never in appsettings.
3. **ScenarioContext usage**: In Reqnroll step classes, always store/retrieve test state via ScenarioContext, not static fields.
4. **K8s service discovery**: Use Kubernetes service names from inside containers, not localhost.
5. **Feature file regex**: Reqnroll step binding uses regex (e.g., `@"a company with CVR ""(.*)"""`). Regex must match the .feature file step text exactly.
6. **Dockerfile context**: Docker builds expect the source root at `/src`, with individual project paths relative to that.

