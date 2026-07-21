# TaxSystem CSV-driven Postman package

## Files

- `collections/TaxSystem CSV Seed Test Data.postman_collection.json`
- `collections/TaxSystem CSV E2E Stress Test.postman_collection.json`
- `data/taxsystem-test-data.csv`
- `scripts/build_taxsystem_postman_package.py`
- `scripts/run-taxsystem-newman.ps1`
- `scripts/run-taxsystem-newman.sh`
- `scripts/run-taxsystem-newman-parallel.ps1`
- `scripts/run-taxsystem-newman-parallel.sh`

## Postman / Requestly

Both collection files use the standard Postman v2.1 schema, so they can be imported into Postman or into Requestly's API Client the same way.

1. Import both collection JSON files.
2. Open the runner for `TaxSystem CSV Seed Test Data`.
3. Select `data/taxsystem-test-data.csv` as the data file.
4. Run 1000 iterations.
5. After seeding completes, run `TaxSystem CSV E2E Stress Test` with the same CSV and iteration count.

The seed collection creates a company only when `seedCompany` is `true`. It creates one citizen for every CSV row. The health check is skipped after the first row.

The E2E stress collection runs this flow for each CSV row:

1. Get Citizen
2. Report Deductible
3. Get Company
4. Report Salary
5. Get Statement
6. Get Bank Transfers

The CSV includes `cpr`, `citizenId`, `cvr`, `income`, `paidTax`, `deductibleAmount`, and `year` values used by the E2E requests.

### Running requests in parallel

Requestly's runner (like Postman's) executes iterations and requests sequentially on a single thread - there is no built-in "run in parallel" toggle. To actually stress the system with concurrent traffic, run several runner instances at once, each against a different slice of the CSV:

- **GUI (Requestly or Postman):** run `scripts/run-taxsystem-newman-parallel.ps1 -SkipSeed` (or the `.sh` equivalent with `--skip-seed`) once to generate sharded CSVs under `data/shards/`. Then import `TaxSystem CSV E2E Stress Test` into several runner tabs, pick a different shard file as the data source in each tab, and start every tab at roughly the same time.
- **CLI (Newman):** use `scripts/run-taxsystem-newman-parallel.ps1` / `.sh` directly - see below. It seeds sequentially once, then launches multiple Newman processes concurrently, each driving its own shard of rows through the stress collection.


## Regenerate the package

```bash
python scripts/build_taxsystem_postman_package.py \
  --citizens 1000 \
  --companies 100 \
  --year 2026 \
  --base-url https://taxsystem.kvikit.dk
```

The CSV is written as quoted UTF-8 with BOM so identifiers such as CPR, CVR, ZIP code, and account number remain text.

## Newman on Windows PowerShell

```powershell
.\scripts\run-taxsystem-newman.ps1
```

Seed only:

```powershell
.\scripts\run-taxsystem-newman.ps1 -SkipStress
```

Use another API URL:

```powershell
.\scripts\run-taxsystem-newman.ps1 -BaseUrl "https://localhost:5001"
```

### Parallel stress load (Windows PowerShell)

```powershell
.\scripts\run-taxsystem-newman-parallel.ps1 -Shards 8
```

Skip seeding (data already seeded) and only shard + run the stress collection:

```powershell
.\scripts\run-taxsystem-newman-parallel.ps1 -Shards 8 -SkipSeed
```

Use another API URL:

```powershell
.\scripts\run-taxsystem-newman-parallel.ps1 -BaseUrl "https://localhost:5001" -Shards 8
```

## Newman on Linux/macOS

```bash
./scripts/run-taxsystem-newman.sh
```

Use another API URL:

```bash
./scripts/run-taxsystem-newman.sh https://localhost:5001
```

### Parallel stress load (Linux/macOS)

```bash
./scripts/run-taxsystem-newman-parallel.sh --shards 8
```

Skip seeding (data already seeded) and only shard + run the stress collection:

```bash
./scripts/run-taxsystem-newman-parallel.sh --shards 8 --skip-seed
```

Use another API URL:

```bash
./scripts/run-taxsystem-newman-parallel.sh --base-url https://localhost:5001 --shards 8
```

