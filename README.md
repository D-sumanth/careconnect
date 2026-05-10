# CareConnect

CareConnect is a care-home communication and acknowledgement platform. Department leads sign in on a shared/team device, present important notices to their teams, and record typed staff acknowledgements. Admin users can manage staff, departments, users, notices, acknowledgement records, CSV exports, and audit logs.

## Stack

- ASP.NET Core 9 Razor Pages
- ASP.NET Core Identity with role-based authorization
- Entity Framework Core
- PostgreSQL via Npgsql, ready for Neon
- Serilog console logging
- Built-in ASP.NET Core rate limiting
- xUnit tests

## Solution Structure

- `src/CareConnect.Web` - Razor Pages UI, auth flow, admin and lead pages
- `src/CareConnect.Application` - service contracts and DTOs
- `src/CareConnect.Domain` - entities, roles, and enums
- `src/CareConnect.Infrastructure` - EF Core, Identity, services, seed data
- `tests/CareConnect.Tests` - unit and integration tests

## Required Configuration

Do not commit secrets. Configure the database through environment variables, app settings in the hosting platform, or user secrets.

Required production value:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=your-neon-host;Database=neondb;Username=neondb_owner;Password=...;SSL Mode=Require;Trust Server Certificate=true"
$env:FirstAdminSetup__Token="change-this-to-a-long-random-one-time-token"
```

For local development, the app falls back to an in-memory database if `ConnectionStrings:DefaultConnection` is empty. Outside Development, `ConnectionStrings__DefaultConnection` is required and the app will not start without it. Development seed data is created automatically:

- Admin: `admin@careconnect.local` / `Admin123!`
- Department lead: `lead@careconnect.local` / `Lead123!`

Change or disable these before using shared environments.

## First Production Admin

Production does not create demo users. To create the first admin account:

1. Add an App Service setting named `FirstAdminSetup__Token` with a long random value.
2. Confirm `ConnectionStrings__DefaultConnection` is configured and the app can reach Neon.
3. Restart the app.
4. Open `/Account/Setup?token=YOUR_TOKEN`.
5. Create the admin account.
6. Remove `FirstAdminSetup__Token` from App Service settings after the admin is created.

The setup page is disabled automatically once an `Admin` user exists.

## Local Run

```powershell
dotnet restore
dotnet build
dotnet run --project src/CareConnect.Web --no-build
```

Open the printed localhost URL and sign in with one of the development users.

## EF Core and Neon PostgreSQL

Install the EF tool if needed:

```powershell
dotnet tool install --global dotnet-ef
```

Create a migration:

```powershell
dotnet ef migrations add InitialCreate --project src/CareConnect.Infrastructure --startup-project src/CareConnect.Web
```

Apply migrations:

```powershell
dotnet ef database update --project src/CareConnect.Infrastructure --startup-project src/CareConnect.Web
```

For Neon, use the pooled connection string for normal app traffic. If you use a separate direct connection for migrations, configure it in your deployment pipeline rather than committing it.

## Security Notes

- HTTPS redirection and HSTS are enabled outside development.
- Identity cookies are HTTP-only, Secure in non-development environments, and SameSite=Lax.
- Razor forms use antiforgery protection.
- Admin pages require the `Admin` role.
- Lead pages require the `DepartmentLead` role.
- Login and form traffic are rate-limited.
- Accounts lock for 15 minutes after 5 failed password attempts.
- Security headers include CSP, frame blocking, content-type protection, referrer policy, and permissions policy.
- Audit logs avoid raw IP storage by hashing IP addresses.
- Passwords are hashed by ASP.NET Core Identity.
- Admin password reset is available from the Users page. Email-based self-service reset should be wired to a provider before wider rollout.

## Compliance Notes

- Staff directory records allow exact missing acknowledgement tracking.
- Acknowledgements are treated as append-only compliance records.
- Admin corrections and voids are audit logged instead of silently deleting records.
- CSV exports include export metadata and are audit logged.
- Confirm the final retention period with the care provider before live use. A common policy is to retain acknowledgement and audit records for multiple years.
- Confirm Neon backup/restore settings before onboarding real care-home data.

## Testing

```powershell
dotnet test
```

Current coverage includes acknowledgement creation, duplicate team-member acknowledgement behavior, audit logging, anonymous redirects for protected pages, production configuration guardrails, notice progress calculations, and the health endpoint.

## Azure App Service Deployment Notes

1. Publish `src/CareConnect.Web`.
2. Configure `ConnectionStrings__DefaultConnection` as an App Service setting.
3. Set `ASPNETCORE_ENVIRONMENT=Production`.
4. Run EF migrations from CI/CD or a controlled deployment step before deploying code that depends on new columns.
5. Enable HTTPS only.
6. Configure log streaming or Application Insights as needed.
7. Configure Azure budget alerts and App Service cost alerts.
8. Prefer a staging slot for production changes when available: deploy to staging, smoke test `/health`, sign-in, admin notices, and lead acknowledgement, then swap.

Recommended production release order:

```powershell
dotnet test
dotnet ef database update --project src/CareConnect.Infrastructure --startup-project src/CareConnect.Web
dotnet publish src/CareConnect.Web -c Release
```

The `/health` endpoint verifies the application can reach the configured database.

## Legacy Node App

The original Express/static HTML files remain in the repository as migration reference. The ASP.NET Core solution is the production path going forward.
