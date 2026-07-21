#!/usr/bin/env python3
"""Generate CSV-driven TaxSystem Postman collections and deterministic test data.

Uses only the Python standard library.
"""

from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path
from typing import Any


FIRST_NAMES = [
    "Anders", "Mette", "Lars", "Sofie", "Nikolaj", "Camilla", "Frederik", "Julie",
    "Mikkel", "Emma", "Christian", "Ida", "Rasmus", "Maria", "Peter", "Line",
    "Jonas", "Louise", "Thomas", "Anna", "Martin", "Katrine", "Mathias", "Cecilie",
    "Simon", "Laura", "Henrik", "Pernille", "Jesper", "Trine", "Casper", "Nanna",
    "Mads", "Sara", "Emil", "Signe", "Jakob", "Malene", "Daniel", "Josefine",
]

LAST_NAMES = [
    "Jensen", "Nielsen", "Hansen", "Pedersen", "Andersen", "Christensen", "Larsen",
    "Sørensen", "Rasmussen", "Jørgensen", "Petersen", "Madsen", "Kristensen", "Olsen",
    "Thomsen", "Poulsen", "Johansen", "Møller", "Mortensen", "Knudsen", "Jakobsen",
    "Jacobsen", "Mikkelsen", "Frederiksen", "Laursen", "Henriksen", "Lund", "Holm",
    "Schmidt", "Vestergaard",
]

STREETS = [
    "Nørrebrogade", "Østerbrogade", "Vesterbrogade", "Amagerbrogade", "Frederiks Allé",
    "Jernbanegade", "Slotsgade", "Kirkegade", "Skovvej", "Strandvej", "Parkvej",
    "Stationsvej", "Møllevej", "Engvej", "Søndergade", "Vestergade", "Østergade",
    "Nygade", "Hovedgaden", "Birkevej",
]

CITIES = [
    ("København", "1000"), ("Aarhus", "8000"), ("Odense", "5000"),
    ("Aalborg", "9000"), ("Esbjerg", "6700"), ("Roskilde", "4000"),
    ("Vejle", "7100"), ("Horsens", "8700"),
]

COMPANY_CITIES = [
    "Københavns", "Aarhus", "Odense", "Aalborg", "Esbjerg", "Roskilde", "Vejle",
    "Horsens", "Randers", "Kolding", "Silkeborg", "Herning", "Hillerød", "Sønderborg",
    "Viborg",
]

INDUSTRIES = [
    "Data", "Revision", "Byggeri", "Logistik", "Energi", "Handel", "Maskinservice",
    "Transport", "Konsulenter", "Ejendomme", "Marine Service", "Teknik", "Regnskab",
    "Installation", "Software",
]

SUFFIXES = ["ApS", "A/S", "I/S", "Holding ApS", "Service ApS"]

CSV_HEADERS = [
    "seedCompany", "cvr", "companyName", "firstName", "lastName", "cpr",
    "citizenId", "streetAddress", "city", "zipCode", "bankAccountNumber",
    "income", "paidTax", "deductibleAmount", "year",
]


def valid_cvr_values(count: int) -> list[str]:
    """Return unique CVR-shaped values satisfying the modulus-11 check."""
    values: list[str] = []
    candidate = 1_000_001
    weights = [2, 7, 6, 5, 4, 3, 2]

    while len(values) < count:
        first_seven = f"{candidate:07d}"[-7:]
        weighted_sum = sum(int(digit) * weight for digit, weight in zip(first_seven, weights))
        remainder = weighted_sum % 11
        check_digit = 0 if remainder == 0 else 11 - remainder
        if check_digit != 10:
            values.append(f"{first_seven}{check_digit}")
        candidate += 1

    return values


def company_name(index: int) -> str:
    city = COMPANY_CITIES[index % len(COMPANY_CITIES)]
    industry = INDUSTRIES[(index // len(COMPANY_CITIES)) % len(INDUSTRIES)]
    suffix = SUFFIXES[(index // (len(COMPANY_CITIES) * len(INDUSTRIES))) % len(SUFFIXES)]
    return f"{city} {industry} {suffix}"


def citizen_cpr(index: int) -> str:
    """Create a deterministic, date-shaped 10-digit synthetic CPR value."""
    birth_year = (65 + (index % 40)) % 100
    month = ((index * 7) % 12) + 1
    day = ((index * 13) % 28) + 1
    serial = 1000 + index
    return f"{day:02d}{month:02d}{birth_year:02d}{str(serial)[-4:]}"


def build_rows(citizens: int, companies: int, year: int) -> list[dict[str, str]]:
    if citizens < 1:
        raise ValueError("citizens must be at least 1")
    if companies < 1:
        raise ValueError("companies must be at least 1")
    if companies > citizens:
        raise ValueError("companies cannot exceed citizens when using one master CSV")

    cvrs = valid_cvr_values(companies)
    names = [company_name(i) for i in range(companies)]
    rows: list[dict[str, str]] = []

    for index in range(1, citizens + 1):
        company_index = (index - 1) % companies
        city, zip_code = CITIES[(index - 1) % len(CITIES)]
        house_number = ((index * 17) % 199) + 1
        income = 300_000 + ((index * 137) % 700_000)
        paid_tax = int(income * 0.37)
        deductible_amount = 5_000 + ((index * 47) % 25_000)

        rows.append({
            "seedCompany": "true" if index <= companies else "false",
            "cvr": cvrs[company_index],
            "companyName": names[company_index],
            "firstName": FIRST_NAMES[(index - 1) % len(FIRST_NAMES)],
            "lastName": LAST_NAMES[((index - 1) // len(FIRST_NAMES)) % len(LAST_NAMES)],
            "cpr": citizen_cpr(index),
            "citizenId": str(index),
            "streetAddress": f"{STREETS[(index - 1) % len(STREETS)]} {house_number}",
            "city": city,
            "zipCode": zip_code,
            "bankAccountNumber": f"DK{10_000_000_000_000 + index}",
            "income": str(income),
            "paidTax": str(paid_tax),
            "deductibleAmount": str(deductible_amount),
            "year": str(year),
        })

    return rows


def script_event(listen: str, lines: list[str]) -> dict[str, Any]:
    return {
        "listen": listen,
        "script": {"type": "text/javascript", "exec": lines},
    }


def raw_url(value: str) -> dict[str, Any]:
    without_protocol = value.replace("https://", "").replace("http://", "")
    parts = without_protocol.split("/", 1)
    host = parts[0]
    path = parts[1].split("/") if len(parts) > 1 else []
    return {"raw": value, "host": [host], "path": path}


def request_item(
    name: str,
    method: str,
    url: str,
    *,
    body: str | None = None,
    prerequest: list[str] | None = None,
    tests: list[str] | None = None,
) -> dict[str, Any]:
    events: list[dict[str, Any]] = []
    if prerequest:
        events.append(script_event("prerequest", prerequest))
    if tests:
        events.append(script_event("test", tests))

    headers: list[dict[str, str]] = []
    request: dict[str, Any] = {
        "method": method,
        "header": headers,
        "url": raw_url(url),
    }
    if body is not None:
        headers.append({"key": "Content-Type", "value": "application/json"})
        request["body"] = {
            "mode": "raw",
            "raw": body,
            "options": {"raw": {"language": "json"}},
        }

    return {"name": name, "event": events, "request": request, "response": []}


def health_check_item() -> dict[str, Any]:
    return request_item(
        "Health Check (first CSV row only)",
        "GET",
        "{{baseUrl}}/healthz",
        prerequest=[
            "// Avoid making the same health request for every CSV row.",
            "if (pm.info.iteration > 0) {",
            "  pm.execution.skipRequest();",
            "}",
        ],
        tests=[
            'pm.test("API is healthy", () => {',
            "  pm.response.to.have.status(200);",
            "});",
        ],
    )


def seed_collection(base_url: str) -> dict[str, Any]:
    company_prerequest = [
        'const shouldSeed = String(pm.iterationData.get("seedCompany") || "")',
        "  .trim()",
        "  .toLowerCase() === \"true\";",
        "",
        "if (!shouldSeed) {",
        "  pm.execution.skipRequest();",
        "} else {",
        '  const required = ["cvr", "companyName"];',
        "  const missing = required.filter((key) => {",
        "    const value = pm.iterationData.get(key);",
        '    return value === undefined || value === null || String(value).trim() === "";',
        "  });",
        "",
        "  if (missing.length > 0) {",
        '    throw new Error(`CSV row ${pm.info.iteration + 1} is missing: ${missing.join(", ")}`);',
        "  }",
        "}",
    ]

    citizen_prerequest = [
        "const required = [",
        '  "firstName", "lastName", "cpr", "streetAddress",',
        '  "city", "zipCode", "bankAccountNumber"',
        "];",
        "",
        "const missing = required.filter((key) => {",
        "  const value = pm.iterationData.get(key);",
        '  return value === undefined || value === null || String(value).trim() === "";',
        "});",
        "",
        "if (missing.length > 0) {",
        '  throw new Error(`CSV row ${pm.info.iteration + 1} is missing: ${missing.join(", ")}`);',
        "}",
    ]

    return {
        "info": {
            "name": "TaxSystem CSV Seed Test Data",
            "description": (
                "Seeds companies and citizens from taxsystem-test-data.csv. Run the collection with the CSV as "
                "iteration data. A company is created only when seedCompany=true; one citizen is created per row."
            ),
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "item": [
            health_check_item(),
            request_item(
                "Seed Company when seedCompany=true",
                "POST",
                "{{baseUrl}}/Company",
                body='{\n  "CVR": "{{cvr}}",\n  "Name": "{{companyName}}"\n}',
                prerequest=company_prerequest,
                tests=[
                    'pm.test("company seed response is acceptable", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 201, 202, 409]);",
                    "});",
                ],
            ),
            request_item(
                "Seed Citizen",
                "POST",
                "{{baseUrl}}/Citizen",
                body=(
                    '{\n'
                    '  "firstName": "{{firstName}}",\n'
                    '  "lastName": "{{lastName}}",\n'
                    '  "cpr": "{{cpr}}",\n'
                    '  "streetAddress": "{{streetAddress}}",\n'
                    '  "city": "{{city}}",\n'
                    '  "zipCode": "{{zipCode}}",\n'
                    '  "bankAccountNumber": "{{bankAccountNumber}}"\n'
                    '}'
                ),
                prerequest=citizen_prerequest,
                tests=[
                    'pm.test("citizen seed response is acceptable", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 201, 202, 409]);",
                    "});",
                ],
            ),
        ],
        "event": [],
        "variable": [{"key": "baseUrl", "value": base_url, "type": "string"}],
    }


def stress_collection(base_url: str, year: int) -> dict[str, Any]:
    row_validation = [
        'const required = ["cpr", "cvr", "citizenId", "income", "paidTax", "deductibleAmount"];',
        "const missing = required.filter((key) => {",
        "  const value = pm.iterationData.get(key);",
        '  return value === undefined || value === null || String(value).trim() === "";',
        "});",
        "",
        'const csvYear = pm.iterationData.get("year");',
        'const fallbackYear = pm.collectionVariables.get("year");',
        "const resolvedYear = csvYear === undefined || csvYear === null || String(csvYear).trim() === \"\"",
        "  ? fallbackYear",
        "  : csvYear;",
        "",
        "if (missing.length > 0) {",
        '  throw new Error(`CSV row ${pm.info.iteration + 1} is missing: ${missing.join(", ")}`);',
        "}",
        "",
        'pm.variables.set("year", String(resolvedYear));',
    ]

    response_time_test = [
        'pm.test("response time is acceptable", () => {',
        '  const maximum = Number(pm.collectionVariables.get("maxResponseTimeMs") || 2000);',
        "  pm.expect(pm.response.responseTime).to.be.below(maximum);",
        "});",
    ]

    return {
        "info": {
            "name": "TaxSystem CSV E2E Stress Test",
            "description": (
                "Runs the complete TaxSystem scenario once per row in taxsystem-test-data.csv. "
                "The CSV supplies cpr, cvr, citizenId, income, paidTax, deductibleAmount, and year."
            ),
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "item": [
            health_check_item(),
            request_item(
                "Get Citizen",
                "GET",
                "{{baseUrl}}/Citizen/{{cpr}}",
                prerequest=row_validation,
                tests=[
                    'pm.test("citizen lookup responded", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 404]);",
                    "});",
                    "",
                    'pm.test("citizen should exist after seeding", () => {',
                    "  if (pm.response.code === 404) {",
                    '    console.warn(`Citizen ${pm.iterationData.get("cpr")} was not found.`);',
                    "  }",
                    "});",
                    *response_time_test,
                ],
            ),
            request_item(
                "Report Deductible",
                "POST",
                "{{baseUrl}}/Citizen/{{citizenId}}/deductibles/{{year}}",
                body=(
                    '[\n'
                    '  {\n'
                    '    "amount": {{deductibleAmount}},\n'
                    '    "deductionPercentage": 0.3\n'
                    '  }\n'
                    ']'
                ),
                tests=[
                    'pm.test("deductible report handled", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 501]);",
                    "});",
                    *response_time_test,
                ],
            ),
            request_item(
                "Get Company",
                "GET",
                "{{baseUrl}}/Company/{{cvr}}",
                tests=[
                    'pm.test("company lookup responded", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 404]);",
                    "});",
                    "",
                    'pm.test("company should exist after seeding", () => {',
                    "  if (pm.response.code === 404) {",
                    '    console.warn(`Company ${pm.iterationData.get("cvr")} was not found.`);',
                    "  }",
                    "});",
                    *response_time_test,
                ],
            ),
            request_item(
                "Report Salary",
                "POST",
                "{{baseUrl}}/Company/{{cvr}}/employees/income/{{year}}/{{cpr}}",
                body=(
                    '{\n'
                    '  "income": {{income}},\n'
                    '  "paidTax": {{paidTax}}\n'
                    '}'
                ),
                tests=[
                    'pm.test("salary report accepted", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 202]);",
                    "});",
                    *response_time_test,
                ],
            ),
            request_item(
                "Get Statement",
                "GET",
                "{{baseUrl}}/StatementGenerator/{{cpr}}/Statements/{{year}}",
                tests=[
                    'pm.test("statement endpoint responded", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 404]);",
                    "});",
                    *response_time_test,
                ],
            ),
            request_item(
                "Get Bank Transfers",
                "GET",
                "{{baseUrl}}/Bank/transfers/{{cpr}}",
                tests=[
                    'pm.test("bank transfers endpoint responded", () => {',
                    "  pm.expect(pm.response.code).to.be.oneOf([200, 404]);",
                    "});",
                    *response_time_test,
                ],
            ),
        ],
        "event": [],
        "variable": [
            {"key": "baseUrl", "value": base_url, "type": "string"},
            {"key": "year", "value": str(year), "type": "string"},
            {"key": "maxResponseTimeMs", "value": "2000", "type": "string"},
        ],
    }


def write_json(path: Path, value: dict[str, Any]) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--citizens", type=int, default=1000)
    parser.add_argument("--companies", type=int, default=100)
    parser.add_argument("--year", type=int, default=2026)
    parser.add_argument("--base-url", default="https://taxsystem.kvikit.dk")
    args = parser.parse_args()

    output_dir = args.output_dir.resolve()
    collections_dir = output_dir / "collections"
    data_dir = output_dir / "data"
    collections_dir.mkdir(parents=True, exist_ok=True)
    data_dir.mkdir(parents=True, exist_ok=True)

    rows = build_rows(args.citizens, args.companies, args.year)
    csv_path = data_dir / "taxsystem-test-data.csv"
    with csv_path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=CSV_HEADERS, quoting=csv.QUOTE_ALL)
        writer.writeheader()
        writer.writerows(rows)

    write_json(
        collections_dir / "TaxSystem CSV Seed Test Data.postman_collection.json",
        seed_collection(args.base_url),
    )
    write_json(
        collections_dir / "TaxSystem CSV E2E Stress Test.postman_collection.json",
        stress_collection(args.base_url, args.year),
    )

    print(f"Generated {len(rows)} CSV rows with {args.companies} companies")
    print(csv_path)


if __name__ == "__main__":
    main()
