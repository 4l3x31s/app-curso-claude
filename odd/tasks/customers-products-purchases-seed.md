# customers-products-purchases-seed

## Objective

Create the `Customers`, `Products`, and `Purchases` tables in the `curso_claude` SQL Server database through EF Core and fill each with 500 generated rows using the Bogus library.

## Problem and why

The database is empty and the app only has an empty `AppDbContext`. The user needs realistic sample data: customers, products, and the purchases that link them.

## Scope

- Entities `Customer`, `Product`, `Purchase` under `Models/`, mapped in `Data/AppDbContext.cs`.
- One EF Core migration creating the three tables, applied to `curso_claude`.
- A Bogus-based seeder that inserts 500 customers, 500 products, and 500 purchases, run on demand with `dotnet run -- seed`.

Out of scope: controllers, views, CRUD screens, tests project, authentication.

## Constraints

- .NET 10, block-scoped `app_curso_claude.<Folder>` namespaces, nullable enabled.
- Code, identifiers, table and column names in English.
- Seeding is explicit (never on normal app startup), deterministic (fixed Bogus seed), and idempotent (a table that already has rows is skipped).
- `appsettings.json` holds the database password and is not committed by this work; committing it is the user's decision.
- Database operations authorized by the user: create these three tables and insert the generated rows in `curso_claude` at `192.168.130.1,1433` using the `appsettings.json` credentials.

## Data model

- `Customer`: `Id`, `FirstName`, `LastName`, `Email` (unique), `Phone`, `Address`, `City`, `Country`, `CreatedAt`.
- `Product`: `Id`, `Sku` (unique), `Name`, `Description`, `Category`, `Price` decimal(18,2), `Stock`, `CreatedAt`.
- `Purchase`: `Id`, `CustomerId` (FK), `ProductId` (FK), `Quantity`, `UnitPrice` decimal(18,2), `PurchasedAt`. Deleting a customer or product with purchases is restricted.

## Tasks

- [x] T1 — EF Core SQL Server connection (`AppDbContext`, `AddDbContext`, provider package). Route: inline (done in the previous request, three mechanical edits). Evidence: EF connection opened against `192.168.130.1,1433` / `curso_claude`; `dotnet build` 0 errors, 0 warnings. Commit: `44f0dc8`.
- [x] T2 — Entities, `AppDbContext` mapping, `Microsoft.EntityFrameworkCore.Design`, local `dotnet-ef` tool manifest, migration `CreateCustomersProductsPurchases`, applied to the database. Route: delegated writer (2+ non-trivial files). Checks: `dotnet build`; `dotnet ef database update`; the three tables exist with the expected columns and foreign keys. Evidence: build 0 errors, 0 warnings; migration `20261006223259_CreateCustomersProductsPurchases` applied; `FK_Purchases_Customers_CustomerId` and `FK_Purchases_Products_ProductId` present (parent readback). Packages: `Microsoft.EntityFrameworkCore.Design` 10.0.12, local tool `dotnet-ef` 10.0.12. Commit: `7616fb1`.
- [x] T3 — Bogus package, `Data/DbSeeder.cs`, `seed` command in `Program.cs`, run against the database. Route: delegated writer (same writer as T2). Checks: `dotnet build`; `dotnet run -- seed`; `SELECT COUNT(*)` returns 500 for each table; every purchase references an existing customer and product; a second `dotnet run -- seed` inserts nothing. Evidence: first run inserted 500/500/500; second run inserted 0/0/0; parent spot check of the count query returned 500 customers, 500 products, 500 purchases, 0 orphans. Package: `Bogus` 35.6.5. Commit: `7a1112c`.

## Acceptance criteria

- `Customers`, `Products`, and `Purchases` exist in `curso_claude` with 500 rows each.
- Every `Purchases` row has a valid `CustomerId` and `ProductId`.
- The app still builds with 0 errors and starts normally without seeding.

## Test-first exception

There is no test project and no runner. Verification is functional: build, migration applied, and row-count and foreign-key queries against the database.

## Delivery

Strategy: `ask-on-risk`. Forecast: about 250 authored lines (generated migration files excluded), under the 400-line budget. Review mode is off (global), so no native review runs.

## Progress

- T1, T2, and T3 done and verified on `feat/customers-products-purchases-seed`.
- Running count from `44f0dc8` to `7a1112c`: 791 changed lines, of which 499 are generated migration files; about 292 authored.
- Native assessment of that range: `medium` (`configuration_change` on `.config/dotnet-tools.json`). Review mode is off (global), so no review was started; writer self-verification plus the parent spot check are the checks of record.
- Decisions taken by the writer: `"es"` Bogus locale for customers and products; emails `<username>.<row number>@example.com`; SKUs `SKU-00001` to `SKU-00500`; all string columns required; reference date 2026-01-01 UTC.

## Pending and not verified

- Normal startup (`dotnet run` without `seed`) was not launched after the change; that it never seeds rests on the `args[0] == "seed"` guard in `Program.cs`.
- `appsettings.json` (new keys and the password) and the user's `.gitignore` change remain uncommitted.
- Engram mirror `odd/customers-products-purchases-seed/tasks`: pending, `mem_save` fails with a session registration error.

## Next step

User decisions: whether to commit `appsettings.json`, and whether to push or open a pull request.
