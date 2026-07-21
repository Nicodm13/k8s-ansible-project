#!/usr/bin/env bash
set -euo pipefail

# Requestly / Postman-style collection runners execute requests and iterations
# sequentially on a single thread - there is no "parallel" toggle in the GUI runner.
# To generate real concurrent load against the system, this script shards the CSV
# data into $SHARDS files and launches that many Newman processes at the same time,
# each driving a slice of the rows through the stress collection concurrently.
#
# You can achieve the same effect in Requestly itself: run this script with
# --skip-seed once seeding is done, note the shard files it writes under
# data/shards/, then import each shard CSV into its own Requestly runner tab and
# hit "Run" on all tabs at (roughly) the same time.

BASE_URL="https://taxsystem.kvikit.dk"
SHARDS=4
SKIP_SEED=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base-url)
      BASE_URL="$2"
      shift 2
      ;;
    --shards)
      SHARDS="$2"
      shift 2
      ;;
    --skip-seed)
      SKIP_SEED=1
      shift
      ;;
    *)
      # Backward-compatible positional base URL argument.
      BASE_URL="$1"
      shift
      ;;
  esac
done

PACKAGE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATA_FILE="$PACKAGE_ROOT/data/taxsystem-test-data.csv"
SEED_COLLECTION="$PACKAGE_ROOT/collections/TaxSystem CSV Seed Test Data.postman_collection.json"
STRESS_COLLECTION="$PACKAGE_ROOT/collections/TaxSystem CSV E2E Stress Test.postman_collection.json"
SHARD_DIR="$PACKAGE_ROOT/data/shards"

if ! command -v newman >/dev/null 2>&1; then
  echo "Newman was not found. Install Node.js, then run: npm install -g newman" >&2
  exit 1
fi

if [[ "$SHARDS" -lt 1 ]]; then
  echo "--shards must be at least 1" >&2
  exit 1
fi

ITERATIONS="$(($(wc -l < "$DATA_FILE") - 1))"

if [[ "$SKIP_SEED" -eq 0 ]]; then
  # Seeding must run once, sequentially, against the full data set so that
  # company/citizen creation is not raced across parallel workers.
  newman run "$SEED_COLLECTION" \
    --iteration-data "$DATA_FILE" \
    --iteration-count "$ITERATIONS" \
    --env-var "baseUrl=$BASE_URL"
fi

rm -rf "$SHARD_DIR"
mkdir -p "$SHARD_DIR"

# Split rows round-robin across N shard CSVs, preserving the header in each.
awk -v n="$SHARDS" -v dir="$SHARD_DIR" '
  NR==1 { header=$0; next }
  {
    i=(NR-2)%n
    file=dir"/shard-"i".csv"
    if (!(i in seen)) { print header > file; seen[i]=1 }
    print $0 >> file
  }
' "$DATA_FILE"

echo "Launching parallel Newman workers against $BASE_URL ..."

PIDS=()
for SHARD_FILE in "$SHARD_DIR"/shard-*.csv; do
  [[ -e "$SHARD_FILE" ]] || continue
  SHARD_COUNT="$(($(wc -l < "$SHARD_FILE") - 1))"
  (
    newman run "$STRESS_COLLECTION" \
      --iteration-data "$SHARD_FILE" \
      --iteration-count "$SHARD_COUNT" \
      --env-var "baseUrl=$BASE_URL"
  ) &
  PIDS+=("$!")
done

FAILED=0
for PID in "${PIDS[@]}"; do
  if ! wait "$PID"; then
    FAILED=$((FAILED + 1))
  fi
done

if [[ "$FAILED" -gt 0 ]]; then
  echo "$FAILED of ${#PIDS[@]} parallel Newman workers did not complete successfully." >&2
  exit 1
fi

echo "All parallel Newman workers finished."

