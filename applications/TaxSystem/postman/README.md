# TaxSystem CSV-driven Postman package

## Files

- `collections/TaxSystem CSV Seed Test Data.postman_collection.json`
- `collections/TaxSystem CSV E2E Stress Test.postman_collection.json`
- `data/taxsystem-test-data.csv`
- `scripts/build_taxsystem_postman_package.py`
- `scripts/run-taxsystem-newman.ps1`
- `scripts/run-taxsystem-newman.sh`

## Postman

1. Import both collection JSON files.
2. Open the runner for `TaxSystem CSV Seed Test Data`.
3. Select `data/taxsystem-test-data.csv` as the data file.
4. Run 1000 iterations.
5. After seeding completes, run `TaxSystem CSV E2E Stress Test` with the same CSV and iteration count.

The seed collection creates a company only when `seedCompany` is `true`. It creates one citizen for every CSV row. The health check is skipped after the first row.

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

## Newman on Linux/macOS

```bash
./scripts/run-taxsystem-newman.sh
```

Use another API URL:

```bash
./scripts/run-taxsystem-newman.sh https://localhost:5001
```
