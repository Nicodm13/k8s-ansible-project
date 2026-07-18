# TaxSystem Test Infrastructure

## Project Structure

```
TaxSystem.Tests/                         (Service-level tests — no infrastructure needed)
├── Features/
│   ├── ReportSalary.feature
│   └── RequestStatement.feature
├── StepDefinitions/
│   ├── SharedStepDefinitions.cs         # Common Given steps (shared via ScenarioContext)
│   ├── ReportSalaryStepDefinitions.cs   # When/Then for Report Salary
│   └── RequestStatementStepDefinitions.cs # When/Then for Request Statement
├── Messaging/
│   └── MessageQueueTests.cs            # In-memory MessageQueueSync test
└── TaxSystem.Tests.csproj

TaxSystem.Tests.E2E/                     (End-to-end tests — requires running cluster)
├── Features/
│   ├── ReportSalary.feature
│   └── RequestStatement.feature
├── StepDefinitions/
│   └── E2EStepDefinitions.cs           # HTTP-based steps against deployed services
├── Messaging/
│   └── MessageQueueTests.cs            # RabbitMQ integration test
└── TaxSystem.Tests.E2E.csproj
```

## Running Tests Locally

### Service-Level Tests (no infrastructure needed)
```bash
dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj
```

### E2E Tests (requires running cluster)
```bash
# Set environment variables pointing to your cluster
export CLIENT_BASE_URL=http://localhost:8080
export RABBITMQ_HOST=localhost
export RABBITMQ_PORT=5672
export RABBITMQ_USERNAME=guest
export RABBITMQ_PASSWORD=guest

dotnet test TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj
```

## How It Works

### Service-Level Tests (`TaxSystem.Tests`)
- Test services **in isolation**, in-process
- Use `MessageQueueSync` (in-memory `IMessageQueue`) to verify event flow
- Step definitions are split per feature, with shared Given steps in `SharedStepDefinitions.cs`
- State is shared between step definition classes via Reqnroll's `ScenarioContext`
- No Docker, no network, no external dependencies — fast feedback loop
- References all service projects so it can instantiate service classes directly

### E2E Tests (`TaxSystem.Tests.E2E`)
- Send real HTTP requests to the **Client API gateway**
- Exercise the full stack: Client → microservices → RabbitMQ → microservices
- Include a RabbitMQ integration test that publishes/consumes via the real broker
- Require the cluster to be running (Minikube locally, or CI)
- Only reference `TaxSystem.Shared` (for message types) — no service project references

## CI Pipeline (GitHub Actions)

> **IMPORTANT**: The `.github/workflows/tests.yml` file must be placed at the
> **repository root** (i.e., `k8s-ansible-project/.github/workflows/tests.yml`).
> Copy it from `applications/TaxSystem/TaxSystem/.github/workflows/tests.yml`.

### Jobs

1. **service-tests** — Runs on `ubuntu-latest`. Builds and runs `TaxSystem.Tests`. No infrastructure needed.
2. **e2e-tests** — Spins up Minikube, builds Docker images inside Minikube's Docker daemon
   (no registry needed), deploys all services via `kubectl apply -f k8s/`, then runs `TaxSystem.Tests.E2E`.

### Minikube Strategy
- Uses `medyagh/setup-minikube` GitHub Action
- Images built with `eval $(minikube docker-env)` so they exist inside Minikube's Docker
- K8s manifests use `imagePullPolicy: Never` to use local images
- `minikube service --url` exposes the Client NodePort for test access

## K8s Manifests

Located in `k8s/`:
- `rabbitmq.yaml` — RabbitMQ broker with readiness probe
- `client.yaml` — API gateway (NodePort exposed)
- `citizen-service.yaml`
- `company-service.yaml`
- `audit-service.yaml`
- `statementgenerator-service.yaml`

## Container Image Size

- Service images do **not** include any test project
- Each Dockerfile uses multi-stage builds (SDK for build, runtime-only for final image)
- Services use `mcr.microsoft.com/dotnet/runtime:10.0` (or `aspnet:10.0` for Client)
- Test projects are never containerized — they run from the CI host against the cluster
