# products-purchases-pages

## Objective

Add two pages, Products and Purchases, that read from SQL Server through EF Core repositories, plus the shared contracts (repository interfaces, `OperationResult`, the service registration mark) that four parallel agents will build on next.

## Problem and why

The app has entities and a SQL Server context but no way to browse products or purchases, and the follow-up work needs fixed names to code against. The user first specified in-memory repositories with a provider switch, then replaced that: everything persists to SQL Server and nothing in memory is used.

## Scope

Names below are a contract: use them exactly.

- `Models/Product.cs`: add `bool IsActive`, `true` by default. EF Core migration `AddProductIsActive`, with default `true` for existing rows, created with `dotnet ef migrations add` and applied to `curso_claude`. `Models/Customer.cs` and `Models/Purchase.cs` do not change.
- `Data/Repositories/IProductRepository.cs`: `Task<IReadOnlyList<Product>> GetAllAsync();` and `Task<Product?> GetAsync(string sku);`
- `Data/Repositories/ICustomerRepository.cs`: `Task<IReadOnlyList<Customer>> GetAllAsync();` and `Task<Customer?> GetAsync(int id);`
- `Data/Repositories/IPurchaseRepository.cs`: `Task<IReadOnlyList<Purchase>> GetByProductAsync(int productId);` returning newest first.
- `Data/Repositories/EfCore/`: `EfProductRepository`, `EfCustomerRepository`, `EfPurchaseRepository`, implemented over `AppDbContext`.
- `Services/OperationResult.cs`: `public record OperationResult(bool Success, IReadOnlyList<string> Errors)` with `Ok()` and `Fail(params string[])`.
- `Program.cs`: the mark `// === Application services (register here) ===` right after the `AddDbContext` registration and, below it, the three repositories as scoped services and `builder.Services.AddSingleton(TimeProvider.System);`.
- Pages: `ProductsController` with `Views/Products/Index.cshtml` lists the repository's products; `PurchasesController` with `Views/Purchases/Index.cshtml` filters by `?sku=` with a select of products. A menu link to each in `Views/Shared/_Layout.cshtml`.
- xUnit project `tests/app-curso-claude.Tests`, added to `app-curso-claude.slnx`.

Out of scope: Home and Contact pages; in-memory repositories, `SeedData` and any `Data:Provider` switch (dropped by the user); any write operation or service beyond `OperationResult`; the earlier `DeletedAt` soft delete (discarded by the user, it stays on the unpublished branch `feat/product-soft-delete`).

## Constraints

- No new NuGet package in the web project. Test-only packages go in the test project.
- .NET 10, block-scoped `app_curso_claude.<Folder>` namespaces, nullable enabled; code, identifiers, comments, UI copy and test names in English.
- `CLAUDE.md` rules apply: no credentials in versioned files; never log customer emails, phones or addresses; every new behavior has a test; no `git push` or merge.
- The web project sits at the repository root and globs `**/*.cs`; `tests/**` must be removed from its Compile, Content, EmbeddedResource and None items. Stale `tests/**/bin` and `obj` folders from an earlier branch are on disk.
- Database operations on `curso_claude` at `192.168.130.1,1433`, with the credentials in user secrets: agents run read-only queries from the app and the tests, nothing else (no seed run, no schema change, no data change). The migration `AddProductIsActive` is applied by the user with `dotnet ef database update`: the session's permission control denied agents that step.
- Standing instruction from the user: where a pasted task text says "in memory", implement it on SQL Server.
- Tests run against that SQL Server and only read in this step. Any test that writes must do so inside a transaction that is rolled back.
- Commits: Conventional Commits, no AI attribution, files staged by explicit path (unrelated changes live under `.claude/`).

## Design decisions

- Repositories are scoped, not singletons, because they depend on the scoped `AppDbContext`.
- The EF Core class names (`EfProductRepository`, `EfCustomerRepository`, `EfPurchaseRepository`) were proposed by the orchestrator and approved by the user.
- The two controllers take the repositories directly: the contract names no read service. They hold no business rules.
- Repository reads use `AsNoTracking`. `GetByProductAsync` loads each purchase's `Customer` so the page can show the name.
- Purchases page: no `sku` shows only the select; an unknown `sku` shows a notice; a product without purchases shows an empty state. The select lists all products and labels inactive ones.
- The Purchases page shows the customer's name only.
- Worktrees need no copied settings: `appsettings.json` is versioned and holds host, port and database name; `UserDB` and `PassDB` live in user secrets, shared by every worktree on the machine through the `UserSecretsId` in the project file, and load in the Development environment.

## Tasks

- [x] T1 — Test project (web project excludes `tests/**` through `DefaultItemExcludes`, already added uncommitted), `Product.IsActive`, migration `AddProductIsActive` generated but not applied, `Services/OperationResult.cs` with unit tests. No test in this task touches the database. Route: delegated writer. Checks: `dotnet build`; `dotnet test`; the migration only adds `IsActive` (non-null, default `true`) to `Products`. Evidence: commit `7c9d652`; RED was compile error CS0234 (namespace `app_curso_claude.Services` missing); GREEN `dotnet test app-curso-claude.slnx` 6 passed, 0 failed; `dotnet build app-curso-claude.slnx` 0 warnings, 0 errors, no CS0579; migration `20261007003136_AddProductIsActive`: `Up` only adds `IsActive` (`bit`, non-null, default `true`) to `Products`, `Down` drops it. Nothing connected to the database.
- [x] User step — apply the migration with `dotnet ef database update`. T2 and T3 cannot go green before it. Evidence: applied by the user, not by an agent; observed indirectly because the repository tests that read whole `Product` rows, `IsActive` included, pass against `curso_claude`.
- [ ] T2 — Repository interfaces, EF Core repositories, `Program.cs` mark and registrations, repository tests against SQL Server: products come ordered by SKU, `GetAsync` of an unknown SKU returns `null`, purchases come newest first. Route: delegated writer. Checks: `dotnet build`; `dotnet test`. Evidence: commit `73b6970`; RED was compile error CS0234 (namespace `app_curso_claude.Data.Repositories` missing, 5 occurrences); GREEN `dotnet test app-curso-claude.slnx` 18 passed, 0 failed (12 new: 3 product, 3 customer, 3 purchase repository tests and 3 DI registration tests). The product with two or more purchases is found by a query. Only reads ran against SQL Server.
- [ ] T3 — Products and Purchases pages, menu links, `CLAUDE.md` structure section, smoke tests with `WebApplicationFactory<Program>`: Home, Products, Purchases and Contact answer 200 and `/Products` shows `SKU-00001`. If a test fails, the production code is fixed, not the test. Route: same writer as T2. Checks: `dotnet build`; `dotnet test`. Evidence: commits `58bb263` (pages and tests) and `9ef9bd2` (`CLAUDE.md`); RED was 10 failed, 20 passed (the new page tests got 404 and the menu links were missing); GREEN `dotnet test app-curso-claude.slnx` 30 passed, 0 failed; `dotnet build app-curso-claude.slnx` 0 warnings, 0 errors. Page tests cover the four 200 answers, `SKU-00001` on `/Products`, the menu links, and `/Purchases` with no SKU, an existing SKU (order on the page and customer name without email, phone or address), an unknown SKU and a product without purchases.

## Acceptance criteria

- `/Products` lists the products stored in `curso_claude` with their active state.
- `/Purchases?sku=<existing sku>` lists that product's purchases newest first; the three other cases behave as in the design decisions.
- `GetByProductAsync` returns newest first; `GetAsync` returns `null` for an unknown SKU or id.
- The three repositories and `TimeProvider` resolve from DI.
- `dotnet build` finishes with 0 errors and `dotnet test` passes against SQL Server.

## Test-first

Runner: `dotnet test app-curso-claude.slnx` (xUnit). RED is observed per task before implementing; a compile failure on the missing API counts. The generated migration and the Razor markup have no meaningful RED and are covered by the build and by the page tests.

## Delivery

Strategy: `single-pr`. Forecast: about 500 authored lines (generated migration excluded), above the 400-line guide. It stays one pull request because the user defined this step as one foundation that four parallel agents start from.

## Progress

- Branch `feat/products-purchases-pages` created from `master` at `ff5aae9`.
- A first writer started on the in-memory contract and was stopped before writing anything when the user switched to SQL Server.
- T1 done in `7c9d652`: test project `tests/app-curso-claude.Tests` (xUnit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing` 10.0.12) in the solution, `Product.IsActive`, `Services/OperationResult.cs`, migration `AddProductIsActive` generated and not applied. 6 unit tests, none touches the database. `OperationResult.Fail` copies the array it receives. A model test checks the `IsActive` database default without opening a connection.

- T2 done in `73b6970`: three repository interfaces, three EF Core repositories, the `Program.cs` mark with the scoped registrations and `TimeProvider.System`. `GetByProductAsync` breaks `PurchasedAt` ties by descending id. The tests share one `WebApplicationFactory<Program>` host (`AppFactory`, Development environment) through an xUnit collection.
- T3 done in `58bb263` and `9ef9bd2`: `ProductsController`, `PurchasesController`, their `Index` views, two menu links, `CLAUDE.md` structure section. `PurchasesIndexViewModel` (namespace `app_curso_claude.Models`) is declared at the end of `Controllers/PurchasesController.cs` because `Models/` was outside the writer's edit surface; moving it to `Models/PurchasesIndexViewModel.cs` needs no other change.
- Authored changed lines so far (generated migration excluded): T2 364, T3 357, `CLAUDE.md` 54 (28 added, 26 removed), on top of T1. Above the 400-line guide, as forecast; delivery stays `single-pr`.

## Pending and not verified

- The pages were verified through HTTP tests only; nobody looked at them in a browser, so layout, the toast and the select were not checked visually.
- An inactive product's label in the select and its badge on `/Products` are not covered by a test: the seeded data was not checked for inactive products and the tests may not write.
- Prices use the server's current culture for the decimal separator.
- The branch is not pushed and has no pull request.

## Next step

All tasks are checked. The user decides on push and pull request; the four parallel agents can start from this branch.
