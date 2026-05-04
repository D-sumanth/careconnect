# CareConnect

CareConnect is a care-home communication and acknowledgement platform. Department leads sign in on a shared/team device, present important notices to their teams, and record typed staff acknowledgements. Admin users can manage departments, users, notices, acknowledgement records, CSV exports, and audit logs.

## Stack

- ASP.NET Core 9 Razor Pages
- ASP.NET Core Identity with role-based authorization
- Entity Framework Core
- PostgreSQL via Npgsql, ready for Neon
- Serilog console logging
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
- Identity cookies are HTTP-only and SameSite=Lax.
- Razor forms use antiforgery protection.
- Admin pages require the `Admin` role.
- Lead pages require the `DepartmentLead` role.
- Audit logs avoid raw IP storage by hashing IP addresses.
- Passwords are hashed by ASP.NET Core Identity.

## Testing

```powershell
dotnet test
```

Current coverage includes acknowledgement creation, duplicate team-member acknowledgement behavior, audit logging, anonymous redirects for protected pages, and the health endpoint.

## Azure App Service Deployment Notes

1. Publish `src/CareConnect.Web`.
2. Configure `ConnectionStrings__DefaultConnection` as an App Service setting.
3. Set `ASPNETCORE_ENVIRONMENT=Production`.
4. Run EF migrations from CI/CD or a controlled deployment step.
5. Enable HTTPS only.
6. Configure log streaming or Application Insights as needed.

## Legacy Node App

The original Express/static HTML files remain in the repository as migration reference. The ASP.NET Core solution is the production path going forward.
