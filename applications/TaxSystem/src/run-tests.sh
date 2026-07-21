#!/usr/bin/env bash

# ══════════════════════════════════════════════════════════════════════════════
# TaxSystem Test Runner (Linux/WSL)
#
# Builds the solution, runs service-level tests on bare metal,
# then deploys to Minikube and runs E2E tests.
#
# Usage:
#   ./run-tests.sh              # Run everything
#   ./run-tests.sh --skip-e2e   # Only service-level tests
#   ./run-tests.sh --skip-unit  # Only E2E tests
#   ./run-tests.sh --no-build   # Skip dotnet build
# ══════════════════════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
TAXSYSTEM_MANIFEST_DIR="$REPO_ROOT/applications/TaxSystem"

case "$SCRIPT_DIR" in
  /mnt/*|/media/*)
    echo "ERROR: Run this script from the WSL filesystem, not from $SCRIPT_DIR."
    echo "Copy the project under ~/src and run it there; .NET must set Unix file"
    echo "permissions for apphost files, which Windows-mounted paths do not support."
    exit 1
    ;;
esac

SKIP_UNIT=false
SKIP_E2E=false
NO_BUILD=false

for arg in "$@"; do
  case "$arg" in
    --skip-unit) SKIP_UNIT=true ;;
    --skip-e2e)  SKIP_E2E=true ;;
    --no-build)  NO_BUILD=true ;;
    -h|--help)
      echo "Usage: $0 [--skip-unit] [--skip-e2e] [--no-build]"
      exit 0
      ;;
    *) echo "Unknown option: $arg"; exit 1 ;;
  esac
done

if [ "$USER" = "root" ]; then
  echo "⚠ ERROR: Do not run this script as root. Use a normal user account."
  exit 1
fi

echo ""
echo "========================================"
echo "  TaxSystem Test Runner"
echo "========================================"
echo ""

# ─── Step 1: Build & Publish ─────────────────────────────────────────────────

PUBLISH_DIR="$SCRIPT_DIR/publish"

PUBLISH_PROJECTS=(
  TaxSystem.Client
  TaxSystem.CitizenService
  TaxSystem.CompanyService
  TaxSystem.BankService
  TaxSystem.StatementGeneratorService
)

if [ "$NO_BUILD" = false ]; then
  echo "[1/7] Building solution and publishing services..."

  # Single solution build
  if ! dotnet build "$SCRIPT_DIR/TaxSystem.sln" --configuration Release; then
    echo "✗ BUILD FAILED"; exit 1
  fi

  # Publish each service (--no-build reuses the build above)
  rm -rf "$PUBLISH_DIR"
  for proj in "${PUBLISH_PROJECTS[@]}"; do
    echo "  Publishing $proj..."
    if ! dotnet publish "$SCRIPT_DIR/$proj/$proj.csproj" \
      --configuration Release --no-build \
      -o "$PUBLISH_DIR/$proj" /p:UseAppHost=false; then
      echo "✗ PUBLISH FAILED: $proj"; exit 1
    fi
  done

  echo "✓ Build & publish succeeded."
else
  echo "[1/7] Skipping build (--no-build)."
fi

# ─── Step 2: Service-Level Tests ─────────────────────────────────────────────

if [ "$SKIP_UNIT" = false ]; then
  echo ""
  echo "[2/7] Running service-level tests..."
  if ! dotnet test "$SCRIPT_DIR/TaxSystem.Tests/TaxSystem.Tests.csproj" \
    --configuration Release --no-build --logger "console;verbosity=normal"; then
    echo "✗ SERVICE-LEVEL TESTS FAILED"; exit 1
  fi
  echo "✓ Service-level tests passed."
else
  echo "[2/7] Skipping service-level tests (--skip-unit)."
fi

if [ "$SKIP_E2E" = true ]; then
  echo ""
  echo "Done (E2E skipped)."
  exit 0
fi

# ─── Step 3: Ensure Minikube is running ──────────────────────────────────────

echo ""
echo "[3/7] Ensuring Minikube is running..."

if ! minikube status --format='{{.Host}}' 2>/dev/null | grep -q "Running"; then
  echo "  Starting Minikube..."
  if ! minikube start --driver=docker; then
    echo "✗ FAILED to start Minikube"; exit 1
  fi
fi
echo "✓ Minikube is running."

# ─── Step 4: Build Docker images inside Minikube (parallel) ──────────────────

echo ""
echo "[4/7] Building Docker images inside Minikube..."

eval $(minikube docker-env)

declare -A SERVICES=(
  ["taxsystem-client"]="TaxSystem.Client/Dockerfile"
  ["taxsystem-citizen-service"]="TaxSystem.CitizenService/Dockerfile"
  ["taxsystem-company-service"]="TaxSystem.CompanyService/Dockerfile"
  ["taxsystem-bank-service"]="TaxSystem.BankService/Dockerfile"
  ["taxsystem-statementgenerator-service"]="TaxSystem.StatementGeneratorService/Dockerfile"
)

BUILD_PIDS=()
BUILD_NAMES=()
BUILD_FAILED=false

for name in "${!SERVICES[@]}"; do
  dockerfile="${SERVICES[$name]}"
  echo "  Starting build: $name"
  docker build -q -f "$SCRIPT_DIR/$dockerfile" -t "$name:latest" "$SCRIPT_DIR" &
  BUILD_PIDS+=($!)
  BUILD_NAMES+=("$name")
done

for i in "${!BUILD_PIDS[@]}"; do
  if ! wait "${BUILD_PIDS[$i]}"; then
    echo "✗ DOCKER BUILD FAILED: ${BUILD_NAMES[$i]}"
    BUILD_FAILED=true
  fi
done

if [ "$BUILD_FAILED" = true ]; then
  exit 1
fi

echo "✓ All images built (parallel)."

# ─── Step 5: Deploy to Minikube ──────────────────────────────────────────────

echo ""
echo "[5/7] Deploying K8s manifests..."

TMP_MANIFEST_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_MANIFEST_DIR"' EXIT

# Clean up existing deployments
kubectl delete deployments --all -n taxsystem --ignore-not-found --wait=true
kubectl delete statefulsets --all -n taxsystem --ignore-not-found --wait=true
kubectl delete cluster taxsystem-db -n taxsystem --ignore-not-found --wait=true 2>/dev/null || true

# Wait for all CNPG pods to fully terminate before redeploying
echo "  Waiting for old database pods to terminate..."
kubectl wait --for=delete pod -l cnpg.io/cluster=taxsystem-db -n taxsystem --timeout=60s 2>/dev/null || true

# Install CloudNativePG operator (idempotent)
echo "  Installing CloudNativePG operator..."
kubectl apply --server-side -f https://raw.githubusercontent.com/cloudnative-pg/cloudnative-pg/release-1.25/releases/cnpg-1.25.1.yaml 2>/dev/null
kubectl wait --for=condition=Available deployment/cnpg-controller-manager -n cnpg-system --timeout=60s
if [ $? -ne 0 ]; then echo "✗ CloudNativePG operator not ready"; exit 1; fi
echo "  ✓ CloudNativePG operator ready."

# Ensure namespace exists
kubectl create namespace taxsystem --dry-run=client -o yaml | kubectl apply -f -
echo "  Waiting for namespace taxsystem..."
kubectl wait --for=jsonpath='{.status.phase}'=Active namespace/taxsystem --timeout=15s

echo "  Installing RabbitMQ Helm chart..."
helm repo add bitnami https://charts.bitnami.com/bitnami >/dev/null 2>&1 || true
helm repo update bitnami >/dev/null
if ! helm upgrade --install rabbitmq bitnami/rabbitmq \
  --namespace taxsystem \
  --version 16.0.14 \
  --set global.security.allowInsecureImages=true \
  --set image.registry=docker.io \
  --set image.repository=bitnamilegacy/rabbitmq \
  --set image.tag=4.1.3-debian-12-r1 \
  --set auth.username=taxsystem \
  --set auth.password=taxsystem-dev \
  --set auth.erlangCookie=taxsystem-dev-cookie \
  --set persistence.enabled=false \
  --set resourcesPreset=small \
  --set service.type=ClusterIP \
  --wait \
  --timeout 120s; then
  echo "✗ RabbitMQ Helm install failed"
  kubectl get pods,statefulset,svc,endpoints -n taxsystem
  kubectl describe statefulset rabbitmq -n taxsystem 2>&1 || true
  kubectl logs statefulset/rabbitmq -n taxsystem --tail=100 2>&1 || true
  exit 1
fi

kubectl rollout status statefulset/rabbitmq -n taxsystem --timeout=120s
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=rabbitmq -n taxsystem --timeout=120s
echo "  ✓ RabbitMQ ready."

# Apply credentials first (needed by cluster and services)
kubectl apply -f "$TAXSYSTEM_MANIFEST_DIR/postgres-credentials.yaml"

# Deploy PostgreSQL cluster
echo "  Pre-pulling CloudNativePG PostgreSQL image..."
docker pull ghcr.io/cloudnative-pg/postgresql:16.4 || true
echo "  Deploying PostgreSQL cluster..."
kubectl apply -f "$TAXSYSTEM_MANIFEST_DIR/postgres-cluster.yaml"
echo "  Waiting for PostgreSQL cluster to be ready (up to 120s)..."
kubectl wait --for=condition=Ready cluster/taxsystem-db -n taxsystem --timeout=120s
if [ $? -ne 0 ]; then
  echo "✗ PostgreSQL cluster not ready"
  kubectl get cluster -n taxsystem
  kubectl get pods -n taxsystem -l cnpg.io/cluster=taxsystem-db
  exit 1
fi
echo "  ✓ PostgreSQL cluster ready."

# Apply root manifests with Minikube-local images.
kubectl kustomize "$TAXSYSTEM_MANIFEST_DIR" \
  | sed \
      -e 's#image: ghcr.io/.*/taxsystem-client:.*#image: taxsystem-client:latest#' \
      -e 's#image: ghcr.io/.*/taxsystem-citizen-service:.*#image: taxsystem-citizen-service:latest#' \
      -e 's#image: ghcr.io/.*/taxsystem-company-service:.*#image: taxsystem-company-service:latest#' \
      -e 's#image: ghcr.io/.*/taxsystem-bank-service:.*#image: taxsystem-bank-service:latest#' \
      -e 's#image: ghcr.io/.*/taxsystem-statementgenerator-service:.*#image: taxsystem-statementgenerator-service:latest#' \
      -e 's/imagePullPolicy: IfNotPresent/imagePullPolicy: Never/g' \
  > "$TMP_MANIFEST_DIR/taxsystem.yaml"

kubectl apply -f "$TMP_MANIFEST_DIR/taxsystem.yaml"
kubectl patch service client -n taxsystem --type merge -p '{"spec":{"type":"NodePort"}}'
echo "✓ Manifests applied."

# ─── Step 6: Wait for pods ───────────────────────────────────────────────────

echo ""
echo "[6/7] Waiting for all pods to be ready..."

# Wait for our application deployments + database pods to be ready
# (ignores transient CNPG init/join job pods)
APP_LABELS="app=client,app=citizen-service,app=company-service,app=bank-service,app=statementgenerator-service,app=rabbitmq"
DB_LABEL="cnpg.io/cluster=taxsystem-db,cnpg.io/instanceRole"

echo "  Waiting for application pods..."
if ! kubectl wait --for=condition=ready pod -l app -n taxsystem --timeout=90s 2>/dev/null; then
  # Fallback: try individual waits
  for app in client citizen-service company-service bank-service statementgenerator-service rabbitmq; do
    if ! kubectl wait --for=condition=ready pod -l "app=$app" -n taxsystem --timeout=60s; then
      echo "✗ Pod for $app is not ready"
      kubectl get pods -n taxsystem -o wide
      kubectl logs -l "app=$app" -n taxsystem --tail=30 2>&1 || true
      exit 1
    fi
  done
fi

echo "  Waiting for database pods..."
kubectl wait --for=condition=ready pod -l "cnpg.io/cluster=taxsystem-db" -n taxsystem --timeout=30s 2>/dev/null || true

echo "✓ All pods ready."

echo "  Waiting for MassTransit buses to be connected..."
for app in client citizen-service company-service bank-service statementgenerator-service; do
  if ! kubectl wait --for=condition=ready pod -l app="$app" -n taxsystem --timeout=15s >/dev/null; then
    echo "✗ Pod for $app is not ready"
    exit 1
  fi

  pod=$(kubectl get pod -l app="$app" -n taxsystem -o jsonpath='{.items[0].metadata.name}')
  bus_ready=false
  for attempt in {1..30}; do
    if kubectl logs "$pod" -n taxsystem --tail=80 2>/dev/null | grep -q "Bus started:"; then
      bus_ready=true
      break
    fi

    sleep 1
  done

  if [ "$bus_ready" = false ]; then
    echo "✗ MassTransit bus did not start for $app"
    kubectl logs "$pod" -n taxsystem --tail=80 2>&1 || true
    exit 1
  fi
done

echo "✓ MassTransit buses connected."

# ─── Step 7: Run E2E tests ───────────────────────────────────────────────────

echo ""
echo "[7/7] Running E2E tests..."

# Port-forward Client and RabbitMQ in background
kubectl port-forward service/client 38080:8080 -n taxsystem &
PF_CLIENT_PID=$!
kubectl port-forward service/rabbitmq 35672:5672 -n taxsystem &
PF_RABBITMQ_PID=$!
sleep 2

export CLIENT_BASE_URL="http://localhost:38080"
echo "  CLIENT_BASE_URL = $CLIENT_BASE_URL"

export RABBITMQ_HOST="localhost"
export RABBITMQ_PORT="35672"
export RABBITMQ_USERNAME="taxsystem"
export RABBITMQ_PASSWORD="taxsystem-dev"
echo "  RABBITMQ = $RABBITMQ_HOST:$RABBITMQ_PORT"

cleanup() {
  kill $PF_CLIENT_PID $PF_RABBITMQ_PID 2>/dev/null || true
  wait $PF_CLIENT_PID $PF_RABBITMQ_PID 2>/dev/null || true
}
trap cleanup EXIT

dotnet test "$SCRIPT_DIR/TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj" \
  --configuration Release --no-build --logger "console;verbosity=normal"

if [ $? -ne 0 ]; then
  echo ""
  echo "✗ E2E TESTS FAILED"
  kubectl get pods --namespace taxsystem
  kubectl logs --namespace taxsystem -l app=client --tail=30 2>/dev/null || true
  exit 1
fi

echo ""
echo "========================================"
echo "  ✓ ALL TESTS PASSED"
echo "========================================"
