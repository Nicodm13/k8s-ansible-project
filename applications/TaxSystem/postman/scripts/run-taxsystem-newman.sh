#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${1:-https://taxsystem.kvikit.dk}"
PACKAGE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATA_FILE="$PACKAGE_ROOT/data/taxsystem-test-data.csv"
SEED_COLLECTION="$PACKAGE_ROOT/collections/TaxSystem CSV Seed Test Data.postman_collection.json"
STRESS_COLLECTION="$PACKAGE_ROOT/collections/TaxSystem CSV E2E Stress Test.postman_collection.json"

if ! command -v newman >/dev/null 2>&1; then
  echo "Newman was not found. Install Node.js, then run: npm install -g newman" >&2
  exit 1
fi

ITERATIONS="$(($(wc -l < "$DATA_FILE") - 1))"

newman run "$SEED_COLLECTION" \
  --iteration-data "$DATA_FILE" \
  --iteration-count "$ITERATIONS" \
  --env-var "baseUrl=$BASE_URL"

newman run "$STRESS_COLLECTION" \
  --iteration-data "$DATA_FILE" \
  --iteration-count "$ITERATIONS" \
  --env-var "baseUrl=$BASE_URL"
