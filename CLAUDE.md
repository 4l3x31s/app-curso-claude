# CLAUDE.md

ASP.NET Core MVC app on .NET 10 with EF Core and SQL Server. Solution: `app-curso-claude.slnx`.

## Commands

```bash
dotnet build
dotnet test
dotnet run                                    # http://localhost:5007
dotnet run --urls http://localhost:5008       # inside a worktree
```

In a worktree always pass `--urls` with another port (5008, 5009, 5010, ...) so it does not collide with another session.

## Structure

Exists today:
- `Models/` — entities and view models.
- `Data/` — `AppDbContext` and `Migrations/`.
- `Data/Repositories/` — repository interfaces.
- `Data/Repositories/EfCore/` — their implementations.
- `Services/` — business rules.
- `Controllers/`, `Views/` — MVC layer.
- `tests/app-curso-claude.Tests/` — xUnit tests.

## Rules

- Register services in `Program.cs` below the mark `// === Application services (register here) ===`.
- Controllers and views hold no business rules: they call a service.
- Data standard: the schema changes only through EF Core migrations (`Data/Migrations`); tables and columns in English and PascalCase; every text column has `HasMaxLength`; decimals use `HasPrecision(18, 2)`; no SQL built by concatenation or interpolation (`FromSqlRaw` or `ExecuteSqlRaw` with user data).
- Product deletion is logical (`IsActive = false`), never `Remove` or `DELETE`.
- No credentials in versioned files.
- Never write customer emails, phone numbers, or addresses to the logs.
- Every new behavior comes with its test; a task is not finished until `dotnet test` is green.
- Do not `git push` or merge unless asked.
