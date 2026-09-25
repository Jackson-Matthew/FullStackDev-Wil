# BlastPro

**Web based blast design and calculation system**

BlastPro is a work integrated learning project for managing blast projects, recording hole patterns, and reviewing calculation results. It has an ASP.NET Core MVC web application, a separate ASP.NET Core API, and a SQL Server database managed with Entity Framework Core.

> [!IMPORTANT]
> This is a development build. Calculation outputs, including predicted vibration and idealised flyrock range, are for demonstration and review. They are not an approved blast design or a validated exclusion distance.

## Current capabilities

| Area | Available in the current build |
| --- | --- |
| Accounts | Sign in, sign out, account lockout, and password reset through a local development mailbox. |
| Projects | Create, search, filter, edit, and soft delete projects. |
| Pattern design | Set project parameters, add or remove holes, choose available explosive products, and save a draft layout. |
| Calculations | Calculate and save totals, estimated volume and tonnage, powder factor, predicted peak particle velocity (PPV), idealised flyrock range, and safety warnings when the required inputs are available. Review previous results. |
| Access control | `Blaster` users work with their own projects. `MainCompanyUser` users can access projects within their company. The API enforces company and ownership boundaries. |

Explosive cost is shown only when every hole has a priced product in one currency. Product and price administration is still under development.

### Still under development

- Report data binding and PDF report download. The report route is present, but it does not yet display a completed report.
- Self registration and connected company, profile, and Blaster administration screens.
- Explosive product and price administration in the user interface.
- Production deployment and production password reset email delivery.

## Architecture

```text
Browser → BlastPro.Mvc → BlastPro.Api → SQL Server LocalDB
          cookie session     JWT + business rules   EF Core
```

The MVC application renders the interface and calls the API over HTTPS. The API handles authentication, authorisation, projects, calculations, and database access. MVC does not connect directly to the database.

| Project | Purpose |
| --- | --- |
| [`BlastPro.Mvc`](BlastPro/BlastPro/BlastPro.Mvc.csproj) | Web interface and API client. |
| [`BlastPro.Api`](BlastPro/BlastPro.Api/BlastPro.Api.csproj) | API, Identity, business logic, and EF Core migrations. |
| [`BlastPro.Tests`](BlastPro/BlastPro.Tests/BlastPro.Tests.csproj) | Unit, integration, and end to end tests. |

## Run locally

### Prerequisites

- Windows with the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- SQL Server LocalDB.
- Visual Studio with ASP.NET and web development support, or two terminals for the .NET CLI.

The repository's development launch profiles use HTTPS. If your local ASP.NET Core certificate is not trusted, run:

```powershell
dotnet dev-certs https --trust
```

### Visual Studio

1. Open [`BlastPro/BlastPro.slnx`](BlastPro/BlastPro.slnx) from the repository root.
2. Select the **BlastPro** multi-project launch profile and press **F5**. This starts the API and MVC projects together.
3. Open <https://localhost:7001/Account/Login> if the browser does not open automatically.

If the shared profile is not shown, configure `BlastPro.Api` and `BlastPro.Mvc` as startup projects and start both. The API must be available for sign-in and project pages to work.

### .NET CLI

From the repository root, start the API in one terminal:

```powershell
dotnet run --project .\BlastPro\BlastPro.Api\BlastPro.Api.csproj --launch-profile BlastPro.Api
```

Start the web application in another terminal:

```powershell
dotnet run --project .\BlastPro\BlastPro\BlastPro.Mvc.csproj --launch-profile BlastPro.Mvc
```

| Service | Development address |
| --- | --- |
| Web application | <https://localhost:7001> |
| API | <https://localhost:7002> |
| Swagger UI | <https://localhost:7002/swagger> |

Swagger UI is available only in the Development environment. Protected API endpoints require a valid bearer token.

### Database and configuration

The API uses SQL Server LocalDB for local development. On startup it applies the committed EF Core migrations and seeds the development roles, company, and initial administrator if they do not exist. The seed account is for development only; review the seeding logic in [`BlastPro.Api/Program.cs`](BlastPro/BlastPro.Api/Program.cs) before using a shared environment.

The Development profile uses the connection string in `BlastPro/BlastPro.Api/appsettings.Development.json`. If your existing LocalDB database conflicts with migration history, use a unique database name without deleting data you need. From the repository root:

```powershell
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Server=(localdb)\MSSQLLocalDB;Database=BlastProApiDevYourName;Trusted_Connection=True;MultipleActiveResultSets=true' --project .\BlastPro\BlastPro.Api\BlastPro.Api.csproj
```

Replace `YourName` with your own identifier, then restart the API. The MVC API address is configured through `ApiBaseUrl` in `BlastPro/BlastPro/appsettings.json`. Keep real credentials and deployment secrets out of committed settings.

## Test

From the repository root:

```powershell
dotnet test .\BlastPro\BlastPro.slnx
```

The test project covers authentication, project access, calculations, result history, and MVC flows. Tests use isolated test data; local SQL Server migration behaviour should also be checked when changing the data model.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Sign-in or projects cannot load | Confirm that both applications are running and that MVC's `ApiBaseUrl` points to the API. |
| Local HTTPS certificate warning | Trust the development certificate with `dotnet dev-certs https --trust`. |
| Migration reports that a table already exists | Use a new LocalDB database name through API user secrets; preserve the old database if it contains data you need. |
| No password reset email arrives | Development reset links are written to `BlastPro/BlastPro.Api/App_Data/PasswordReset`, not sent by email. See the [account guide](BlastPro/docs/login-and-password-reset.md). |
| Results show no total cost | Each hole needs an active, priced explosive product using the same currency. |
| The report page says there are no results to report on | Report data binding and PDF generation are still under development. Review the current output on the Results page. |

## Further documentation

- [Login and local password reset](BlastPro/docs/login-and-password-reset.md)
- [Database model and migration notes](BlastPro/docs/database-plan.md)

This repository currently has no production deployment guide or published demonstration link.
