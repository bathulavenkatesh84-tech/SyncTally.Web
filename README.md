# SyncTally.Web – ASP.NET Core MVC (.NET 8) + PostgreSQL

Public marketing site for SyncTally (home, features + mega menu, pricing, partners, blog, Tally connector, about, contact,
WhatsApp chat). The site ends at the login page: every **Sign in / Try the demo / Start free trial** button goes to
`https://localhost:7299/tallywebapp/Login` (configurable, see step 4).

## Run it – step by step

1. **Install** the .NET 8 SDK and PostgreSQL 13+ (needs `psql`).
2. **Create the tables and data** (schema `cmr_admin`):
   ```bash
   psql -U postgres -d postgres -f Database/schema.sql
   psql -U postgres -d postgres -f Database/seed.sql
   ```
   `seed.sql` is re-runnable (it truncates the seeded tables first). Use another database name if you like.
3. **Set the connection string** in `appsettings.json` → `DatabaseSettings:ConnectionString`
   (keep `Search Path=cmr_admin`, or leave the explicit `cmr_admin.` prefixes to do the work).
4. **Set where "Sign in" goes** in `appsettings.json` → `ExternalApp:LoginUrl` / `RegisterUrl`
   (default `https://localhost:7299/tallywebapp/Login`).
5. **Restore and run** (restoring `Npgsql` needs internet access to NuGet):
   ```bash
   dotnet restore
   dotnet dev-certs https --trust     # first time only
   dotnet run --launch-profile https
   ```
   Open **https://localhost:7300** (a different port from your tallywebapp on 7299, so both can run together).

## Architecture

`Controller → Service → Repository → Flexidb → PostgreSQL`

* **No Entity Framework, no DTOs, no mapping tables.** Repositories return `DataTable`s from parameterised
  `NpgsqlCommand`s; services map `DataRow` → ViewModels (`Services/DataRowExtensions.cs`). The `/DTOs` folder from the
  brief is intentionally absent – ViewModels are the only shapes.
* **Flexidb** (`Services/Flexidb.cs`, `IFlexidb`) follows your class: `Getdt`, `Getdtcmd`, `GetScalarcmd`,
  `InsertDbRowcmd`, static `WriteError`, `IOptions<DatabaseSettings>`. Added: async versions, `ExecuteTransactionAsync`
  (multi-step writes), errors are logged **and re-thrown** (so the error page shows instead of silently empty data).
  The Google Drive / Sheets / OAuth members were not carried over – this site does not use them.
* Every service and repository has an interface, registered in `Program.cs`.
* Every contact / lead / partner-application submit writes its row **and** a `site_activity` row in one transaction.
* Search, filter, sort and paging run in SQL (`ILIKE`, whitelisted `ORDER BY`, `LIMIT/OFFSET`, `COUNT(*) OVER()`).
* Menu and settings are cached with `IMemoryCache` (10 min).
* `Middleware/ExceptionHandlingMiddleware.cs` → `IErrorLogService` (daily file in `ErrorLog/` **and** table
  `site_error_log`) → friendly `/Home/Error` page (JSON for AJAX). 404 → `/Home/StatusCodePage/404`.
* Anti-forgery on every POST form; the WhatsApp dialog sends the token in the `RequestVerificationToken` header.
* Cookie authentication, `[Authorize]` support and the login-redirect are wired in `Program.cs`, but the
  authenticated application (register, dashboard, roles) lives in **tallywebapp**, per your scope ("till login page").

## Routes

| Page | URL |
|---|---|
| Home | `/` |
| Features / detail | `/Features?q=&status=` · `/Features/Details/{slug}` |
| Pricing | `/Pricing` |
| Partners (+ apply POST) | `/Partners?q=&kind=&sort=&page=` · `POST /Partners/Apply` |
| Blog / post | `/Blog?q=&tag=&sort=&page=` · `/Blog/Details/{slug}` |
| Tally Connector (+ downloads) | `/Connector` · `/Connector/Config` · `/Connector/Guide` · `/Connector/SampleItems` · `/Connector/SampleLedgers` |
| About / Contact | `/Home/About` · `/Home/Contact` |
| WhatsApp lead (AJAX) | `POST /WhatsApp/Lead` |
| Sign in / Register | `/Account/Login` · `/Account/Register` → redirect to tallywebapp |

## Folder layout

```
Controllers/  Middleware/  Models/  ViewModels/  ViewComponents/  Helpers/
Repositories/{Interfaces,Implementations}/   Services/{Interfaces,Implementations}/
Views/{Shared,Home,Features,Pricing,Partners,Blog,Connector}/   wwwroot/{css,js,lib}/   Database/{schema,seed}.sql
```
