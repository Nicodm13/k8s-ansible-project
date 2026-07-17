# AGENTS.md - TaxSystem Development Guide

## Architecture Overview

**TaxSystem** is an event-driven microservices architecture student project for tax processing:
- **Client** (ASP.NET Core): API gateway and entry point; routes requests to services
- **Microservices**: AuditService, CitizenService, CompanyService, InfoCollectorService, StatementGeneratorService
- **TaxSystem.Shared**: Central models (Citizen, Company, Audit, Statement, Deductible) and messaging abstractions
- **Communication**: RabbitMQ for async inter-service events; Services publish Event objects with topic + arguments

## Critical Data Flows

1. **Report Salary** (ReportSalary.feature):
   - Client receives salary report (company CVR, employee CPR, amount)
   - Triggers event-driven chain: CompanyService → InfoCollectorService → StatementGeneratorService
   - AuditService listens and logs all operations

2. **Request Statement** (RequestStatement.feature):
   - Client requests tax statement for citizen
   - Services aggregate data, generate final statement

**Key**: All inter-service communication uses `Event` class (topic + arguments array). Services subscribe via `IMessageQueue.AddHandler(topic, handler)`.

## Messaging Pattern - Know This

The event system uses three `IMessageQueue` implementations:
- **MessageQueueSync** (in-memory, TaxSystem.Tests): Synchronous pub/sub for fast unit tests
- **MessageQueueAsync** (in-memory async): For async/await patterns
- **RabbitMqQueue** (production): Real RabbitMQ broker; configured via environment variables (RABBITMQ_HOST, RABBITMQ_PORT, RABBITMQ_USERNAME, RABBITMQ_PASSWORD)

**Event structure**:
```csharp
new Event("topic.name", arg1, arg2, arg3)  // Arguments serialized to JSON by default
event.GetArgument<ModelType>(index)          // Deserialize specific argument
```

## Testing Strategy

**TaxSystem.Tests** (in-process, fast feedback):
- Uses Reqnroll for BDD scenarios
- `SharedStepDefinitions.cs`: Common "Given" steps (company setup, employee setup)
- Feature-specific step files: ReportSalaryStepDefinitions.cs, RequestStatementStepDefinitions.cs
- Test state shared via `ScenarioContext` across step classes
- Uses `MessageQueueSync` (no Docker/network needed)
- Run: `dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj`

**TaxSystem.Tests.E2E** (full stack, requires Minikube):
- HTTP requests to Client API gateway
- Real RabbitMQ broker
- Requires environment variables: CLIENT_BASE_URL, RABBITMQ_HOST/PORT/USERNAME/PASSWORD
- Run: `dotnet test TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj`

## Kubernetes & Docker

**Pattern**: Multi-stage builds (SDK → build → runtime-only final image):
- Base image: `mcr.microsoft.com/dotnet/runtime:10.0` (services) or `aspnet:10.0` (Client)
- Manifests in `k8s/` use `imagePullPolicy: Never` (images built locally in Minikube)
- Services discovered via Kubernetes DNS: `{service-name}` (e.g., `citizen-service:5000`)
- Configuration via environment variables, not config files

## Service Structure Conventions

All service projects follow this layout:
```
TaxSystem.{ServiceName}/
├── Program.cs              # Minimal DI setup (mostly empty stubs currently)
├── Repositories/           # Data persistence layer
│   ├── Read{Entity}Repository.cs
│   └── Write{Entity}Repository.cs
├── Services/               # Business logic
│   └── {Service}.cs
└── Dockerfile              # Multi-stage build
```

Repository pattern: Write operations return domain objects; Read operations query state. This abstraction allows swapping RabbitMQ handlers with HTTP clients or database backends.

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

# Deploy to Minikube (CI does: minikube docker-env eval, then build images, then kubectl apply)
kubectl apply -f k8s/
```

## Project Dependencies & Versions

- **.NET 10.0** (latest LTS)
- **Reqnroll 3.3.4**: BDD test framework (Gherkin)
- **RabbitMQ.Client 6.8.1**: For RabbitMQ integration
- **NUnit 4.5.1**: Test runner
- **ASP.NET Core 10.0**: Client gateway

## Key File Locations

| Purpose | File |
|---------|------|
| Event abstraction | `TaxSystem.Shared/Messaging/Event.cs`, `IMessageQueue.cs` |
| Domain models | `TaxSystem.Shared/Models/` (Citizen, Company, Audit, Statement, Deductible) |
| Test workflows | `TaxSystem.Tests/Features/` (ReportSalary.feature, RequestStatement.feature) |
| K8s deployments | `k8s/` (all YAML manifests) |
| CI pipeline | `.github/workflows/tests.yml` (must be at repo root) |
| Test config | `TESTING.md` (comprehensive test documentation) |

## Common Pitfalls

1. **IMessageQueue interface switching**: Tests use MessageQueueSync; production uses RabbitMqQueue. Register the correct implementation in DI.
2. **ScenarioContext usage**: In Reqnroll step classes, always store/retrieve test state via ScenarioContext, not static fields.
3. **K8s service discovery**: Use `hostname:port` (e.g., `rabbitmq:5672`), NOT localhost, in containerized services.
4. **Feature file regex**: Reqnroll step binding uses regex (e.g., `@"a company with CVR ""(.*)"""`). Regex must match the .feature file step text exactly.
5. **Dockerfile context**: Docker builds expect the source root at `/src`, with individual project paths relative to that.

