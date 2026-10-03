# BlastPro

**Web based blast design and calculation system**

BlastPro is a work integrated learning project for managing blast projects, recording hole patterns, and reviewing calculation results. It has an ASP.NET Core MVC web application, a separate ASP.NET Core API, and a SQL Server database managed with Entity Framework Core.

> [!IMPORTANT]
> This is a development build. Calculation outputs, including predicted vibration and idealised flyrock range, are for demonstration and review. They are not an approved blast design or a validated exclusion distance.

## Current capabilities

| Area | Available in the current build |
| --- | --- |
| Accounts | Create a company and its Main Company User with immediate sign-in; sign in and out; use a local development mailbox for password reset. |
| Company administration | Main Company Users manage company contact details, explosive products and prices, and create Blaster accounts directly with an email and password. They can set a Blaster's password, edit details, and deactivate or reactivate accounts. The API enforces five active Blasters per company. |
| Profile | Users view and edit their permitted personal details. Company, role, and account status are controlled by the API. |
| Projects | Create, search, filter, edit, and soft delete projects. New mobile projects appear on the web. |
| Project notes | Web and Android users with project access can add notes and comments and store up to three JPEG, PNG, or WebP photos per note in the shared database. |
| Pattern design | Set project parameters, add or remove holes, choose available explosive products, and save a draft layout. |
| Calculations | Calculate and save totals, estimated volume and tonnage, powder factor, predicted peak particle velocity (PPV), idealised flyrock range, and safety warnings when the required inputs are available. Review previous results. |
| Reports | Preview saved calculation results and use the browser print view. A direct PDF file download is not implemented. |
| Access control | `Blaster` users work with their own projects. `MainCompanyUser` users can access projects within their company. The API enforces company and ownership boundaries. |

Explosive cost is shown only when every hole has an active, priced product in one currency. Main Company Users can add and edit company products and link prices to the AECI catalogue choices used in pattern design. The sample button adds editable demonstration prices for S100 (R35/kg), S300 (R28/kg), and Powergel Eco (R55/kg). These are placeholders, not supplier quotes. Saved calculation totals keep their original cost when a product price changes.

### Still under development

- Direct PDF file download. The `DownloadPdf` action currently opens the browser print view.
- Production deployment. Self-service password reset needs a delivery method outside local development; Blaster account creation does not use email delivery.

## Architecture

```text
Browser → BlastPro.Mvc → BlastPro.Api → SQL Server LocalDB
          cookie session     JWT + business rules   EF Core
```

The MVC application renders the interface and calls the API over HTTPS. The API handles authentication, authorisation, projects, calculations, and database access. MVC does not connect directly to the database.

The Android app signs in with an existing web account, then uses the same API for projects, notes, comments, and photos. Android has no calculation or account-creation screen. It requires a connection to the API and asks the user to sign in again after the app process ends. Earlier device-only Room notes are not automatically imported into shared projects.

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

The web development launch profile uses HTTPS. The API also exposes local HTTP on port 5002 for the Android emulator debug build. If your local ASP.NET Core certificate is not trusted, run:

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

The API uses SQL Server LocalDB for local development. On startup it applies the committed EF Core migrations and ensures Identity roles exist. The sample company is seeded only in Development; its administrator is created only when `DevelopmentSeed:AdminPassword` is configured. New companies use the public Create Company flow.

The Development profile uses the connection string in `BlastPro/BlastPro.Api/appsettings.Development.json`. The API checks LocalDB before applying migrations. If LocalDB claims to be stopped while its old SQL process still holds the database files, the API gracefully shuts down that process when it has no other user sessions and reconnects. Existing database data is kept. If your existing LocalDB database conflicts with migration history, use a unique database name without deleting data you need. From the repository root:

```powershell
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Server=(localdb)\MSSQLLocalDB;Database=BlastProApiDevYourName;Trusted_Connection=True;MultipleActiveResultSets=true' --project .\BlastPro\BlastPro.Api\BlastPro.Api.csproj
```

Replace `YourName` with your own identifier. In Development, the API creates a private signing key in the user's local application data folder if no `Jwt:Key` is configured. It reuses that key on later runs. To set your own key instead, run:

```powershell
$jwtKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set 'Jwt:Key' $jwtKey --project .\BlastPro\BlastPro.Api\BlastPro.Api.csproj
Remove-Variable jwtKey
```

The old committed signing key has been removed. Previously issued tokens will no longer work once the API uses a new key. If you need the optional local sample administrator when creating a fresh development database, set `DevelopmentSeed:AdminPassword` in user secrets to a private password that meets the Identity policy. Otherwise create a company through the application. The MVC API address is configured through `ApiBaseUrl` in `BlastPro/BlastPro/appsettings.json`.

For hosting, configure `Jwt__Key` and `ConnectionStrings__DefaultConnection` as secrets in the deployment environment. Use a new random signing key and production database credentials. Do not commit either value to GitHub. Each environment that needs to accept the same active tokens must use its own stable key.

### Android app

Debug and release builds connect to the hosted Azure API by default, so an emulator or phone can use the app without a local API or Visual Studio running. The device needs internet access. Install Android SDK 35 and select an installed JDK 21 in Android Studio under **Settings > Build, Execution, Deployment > Build Tools > Gradle > Gradle JDK**. For terminal builds, point `JAVA_HOME` at that JDK. The repository does not contain a computer-specific Java path. Build from `BlastPro.Android`:

```powershell
.\gradlew.bat assembleDebug
```

Install the debug APK on a running Android emulator with Android Studio, or run:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" install -r .\app\build\outputs\apk\debug\app-debug.apk
```

To use another API address for a local development build, pass `-PblastProApiUrl=http://10.0.2.2:5002/` to Gradle. A release build must use HTTPS. The app has no registration flow. Create accounts on the web first, then sign in on mobile with the same credentials. Photos are limited to three per note and 5 MB each. Mobile notes use the shared API while online; the old device-only notes are not migrated automatically. Main Company Users see their company's projects; Blasters see the projects they own.

## Test

From the repository root:

```powershell
dotnet test .\BlastPro\BlastPro.slnx
```

The test project covers authentication, company and owner access, account setup, Blaster seats, product prices and saved costs, project notes and photo access, calculations, result history, and MVC forms. Transaction tests require Windows SQL Server LocalDB and use temporary databases deleted after each test. Other tests use isolated in-memory data.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Sign-in or projects cannot load | Confirm that both applications are running and that MVC's `ApiBaseUrl` points to the API. Check the API output for database or startup errors. |
| LocalDB says it is stopped but SQL Server is still running | The Development API repairs this on startup if that LocalDB process has no other user sessions. Close other SQL clients if the API reports active sessions, then restart the API. |
| Local HTTPS certificate warning | Trust the development certificate with `dotnet dev-certs https --trust`. |
| Migration reports that a table already exists | Use a new LocalDB database name through API user secrets; preserve the old database if it contains data you need. |
| No password reset email arrives | Development reset links are written to `BlastPro/BlastPro.Api/App_Data/PasswordReset`, not sent by email. See the [account guide](BlastPro/docs/login-and-password-reset.md). |
| A Blaster cannot sign in after the main user creates the account | Check the email and password entered on Add Blaster. For an older account created under the retired invitation flow, use Company → Set password. See [company setup](BlastPro/docs/company-accounts.md). |
| Results show no total cost | Each hole needs an active, priced explosive product using the same currency. |
| The report page says there are no results to report on | Save a calculation result for the project first, then reopen the report preview. |

## Further documentation

- [Login and local password reset](BlastPro/docs/login-and-password-reset.md)
- [Company setup, Blaster accounts, and profiles](BlastPro/docs/company-accounts.md)
- [Database model and migration notes](BlastPro/docs/database-plan.md)

This repository currently has no production deployment guide or published demonstration link.
