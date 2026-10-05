# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ASP.NET Core MVC web app targeting **.NET 10** (`net10.0`), currently the unmodified `dotnet new mvc` template: a single project (`app-curso-claude.csproj`) referenced by an XML-format solution file (`app-curso-claude.slnx`). No NuGet package references beyond the `Microsoft.NET.Sdk.Web` SDK, no database, no authentication scheme, and no test project.

## Commands

```bash
dotnet build                          # build
dotnet run                            # run with the "http" profile -> http://localhost:5007
dotnet run --launch-profile https     # https://localhost:7291 and http://localhost:5007
dotnet watch                          # run with hot reload
```

There is no test project and no linter/formatter configuration yet. When a test project is added, include it in `app-curso-claude.slnx` and run a single test with `dotnet test --filter "FullyQualifiedName~<TestName>"`.

## Architecture

- `Program.cs` uses top-level statements and the minimal hosting model. It registers only `AddControllersWithViews()`; new services (DbContext, auth, options, etc.) are registered here before `builder.Build()`.
- Routing is conventional only: `{controller=Home}/{action=Index}/{id?}`. No attribute routing or API controllers exist.
- Static files are served with `MapStaticAssets()` and the route is chained with `.WithStaticAssets()` (the .NET 9+ build-time static asset pipeline, not `UseStaticFiles()`). Views should keep referencing assets with `asp-append-version`/`~/` paths so fingerprinting works.
- `UseAuthorization()` is in the pipeline but no authentication is configured, so `[Authorize]` will not work until a scheme is added.
- The exception handler (`/Home/Error`) and HSTS are enabled only outside the Development environment.

## Conventions

- The root namespace is `app_curso_claude` (underscores), while the project, assembly, and folder names use hyphens (`app-curso-claude`). New code uses `app_curso_claude.<Folder>` namespaces, written block-scoped as in the existing files.
- Nullable reference types and implicit usings are enabled.
- `Views/_ViewImports.cshtml` already imports `app_curso_claude` and `app_curso_claude.Models` and registers the MVC tag helpers; views do not need their own `@using` for models.
- Client libraries (Bootstrap, jQuery, jquery-validation) are vendored under `wwwroot/lib/` as shipped by the template; there is no npm/LibMan manifest. Do not edit them by hand.
- `Views/Shared/_Layout.cshtml.css` is a scoped (CSS isolation) stylesheet for the layout; global styles go in `wwwroot/css/site.css`.
