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

kubectl apply -f "$SCRIPT_DIR/k8s/storage.yaml"
if [ $? -ne 0 ]; then echo "✗ KUBECTL APPLY FAILED: storage"; exit 1; fi

kubectl wait --for=jsonpath='{.status.phase}'=Active namespace/tax-system --timeout=30s
if [ $? -ne 0 ]; then echo "✗ NAMESPACE DID NOT BECOME ACTIVE"; exit 1; fi

for manifest in \
  rabbitmq.yaml \
  bank-service.yaml \
  citizen-service.yaml \
  client.yaml \
  company-service.yaml \
  statementgenerator-service.yaml; do
  kubectl apply -f "$SCRIPT_DIR/k8s/$manifest"
  if [ $? -ne 0 ]; then echo "✗ KUBECTL APPLY FAILED: $manifest"; exit 1; fi
done
echo "✓ Manifests applied."

# ─── Step 6: Wait for pods ───────────────────────────────────────────────────

echo ""
echo "[6/7] Waiting for pods to be ready..."

echo "  Waiting for RabbitMQ..."
if ! kubectl wait --namespace tax-system --for=condition=ready pod -l app=rabbitmq --timeout=120s; then
  echo "✗ RabbitMQ did not become ready"
  kubectl get pods --namespace tax-system
  exit 1
fi

echo "  Waiting for Client..."
kubectl wait --namespace tax-system --for=condition=ready pod -l app=client --timeout=120s || \
  echo "  ⚠ Client pod not ready (services may not be implemented yet)"

echo "✓ Pods ready."

# ─── Step 7: Run E2E tests ───────────────────────────────────────────────────

echo ""
echo "[7/7] Running E2E tests..."

# Get Client URL via Minikube
CLIENT_BASE_URL=$(minikube service --namespace tax-system client --url)
export CLIENT_BASE_URL
echo "  CLIENT_BASE_URL = $CLIENT_BASE_URL"

# Port-forward RabbitMQ in background
kubectl port-forward --namespace tax-system service/rabbitmq 35672:5672 &
PF_PID=$!
sleep 2

export RABBITMQ_HOST="localhost"
export RABBITMQ_PORT="35672"
export RABBITMQ_USERNAME="guest"
export RABBITMQ_PASSWORD="guest"
echo "  RABBITMQ = $RABBITMQ_HOST:$RABBITMQ_PORT"

cleanup() {
  kill $PF_PID 2>/dev/null || true
  wait $PF_PID 2>/dev/null || true
}
trap cleanup EXIT

dotnet test "$SCRIPT_DIR/TaxSystem.Tests.E2E/TaxSystem.Tests.E2E.csproj" \
  --configuration Release --no-build --logger "console;verbosity=normal"

if [ $? -ne 0 ]; then
  echo ""
  echo "✗ E2E TESTS FAILED"
  kubectl get pods
  kubectl logs -l app=client --tail=30 2>/dev/null || true
  exit 1
fi

echo ""
echo "========================================"
echo "  ✓ ALL TESTS PASSED"
echo "========================================"

