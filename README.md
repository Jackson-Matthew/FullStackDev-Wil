# BlastPro

BlastPro is Group 21's work integrated learning application for planning, recording, and reviewing surface blast designs. It combines a browser based design workspace with an Android field notebook. Both clients use the same ASP.NET Core API and SQL Server database, so authorised project notes and photos appear across the web and mobile apps.

**This is the first working phase of the full BlastPro project.** The earlier prototype and broader requirements documents guided the design; this README describes what the submitted code actually does. BlastPro is decision support for qualified blasters. Its calculations and warnings are planning estimates and do not approve a blast, determine a safe exclusion distance, or replace site procedures and professional review.

## Start here for assessment

| Item | Where to find it |
| --- | --- |
| Hosted web application | [Open BlastPro](https://blastpro-web-2c61c74b-axf3gvbtg9a8f3hg.southafricanorth-01.azurewebsites.net/) |
| Hosted API | [Azure API base URL](https://blastpro-api-2c61c74b-hnaxb6c4anfbh0cd.southafricanorth-01.azurewebsites.net/) — protected endpoints require a signed-in user's token; Swagger UI is enabled only in local Development |
| Web and API solution | [`BlastPro/BlastPro.slnx`](BlastPro/BlastPro.slnx) |
| Android Studio project | [`BlastPro.Android/`](BlastPro.Android/) |
| Automated tests | Run `dotnet test .\BlastPro\BlastPro.slnx` from the repository root |
| Pipeline and build artifacts | [GitHub Actions](https://github.com/Jackson-Matthew/FullStackDev-Wil/actions/workflows/ci-cd.yml) |
| Access for a marker | Use credentials supplied privately by the team, or use **Create Company** on the sign-in page to create a separate test company and its Main Company User |

The hosted sign-in page returned HTTP 200 and the hosted API correctly rejected an unauthenticated `/api/auth/me` request with HTTP 401 on **5 October 2026**. Availability of the free Azure hosting tier can vary; the local setup below is the reproducible fallback. No passwords or deployment secrets are stored in this README.

## What the submitted system does

| Area | Implemented behaviour |
| --- | --- |
| Accounts and roles | A new company can create its Main Company User and sign in immediately. Main Company Users create Blaster accounts with passwords, manage their details and active status, and are limited to five active Blasters per company. Both roles use the same sign-in page. |
| Access control | A Blaster sees their own projects; a Main Company User can see projects in their company. The API checks company and ownership boundaries for projects, results, notes, photos, and administrative actions. |
| Company and profile | Main Company Users edit company contact details, Blasters, explosive products, and prices. Users edit permitted profile details. |
| Projects | Create, search, filter, view, edit, and soft delete projects. The dashboard shows project states and opens the project's design and results pages. A project created on Android appears in the web dashboard. |
| Pattern design | Enter bench and rock information, generate square/rectangular or staggered hole layouts, choose AECI surface product references, and edit individual hole positions, depths, diameter, subdrill, stemming, charge, product, and delay. The plan can be enlarged, zoomed, and panned; selecting a hole opens its editor. Draft changes update the live summary. |
| Calculations and results | Save a draft, then calculate from the saved design. The API stores totals for holes, drilling, charge, volume, tonnage, powder factor, maximum charge per delay, conditional cost, predicted PPV, idealised flyrock trajectory, and warnings when the required inputs exist. Previous results remain available and old results are marked outdated when the design changes. |
| Reports | Preview a saved result with warnings and a hole schedule/plan when its snapshot exists. Print the report or download a generated PDF. Recent results preserve the pattern snapshot used for that calculation; older results created before snapshots cannot reconstruct a historical hole plan. |
| Field notes | Web and Android users with project access can create project notes, add comments, and attach up to three JPEG, PNG, or WebP photos per note (5 MB maximum each). Android provides note search, project filtering, a note editor, photo viewing, and project creation. |

Explosive cost is shown only if **every hole** has an active priced product in one currency. The optional sample-price action inserts editable demonstration prices; these are not supplier quotations. Saved results keep their calculated cost if prices later change.

### Requirements and scope

The team's Task 1 requirements and prototype describe a much broader eventual product. The assessment build focuses on the working web flow and the later Android notes companion agreed by the team. The following map helps distinguish delivered evidence from future requirements:

| Requirement theme | Evidence in this repository | Current boundary |
| --- | --- | --- |
| Company identity, roles, and data separation | API authentication and company-scoped controllers; account and access tests | Local password reset writes links to a development mailbox; hosted email delivery is not configured |
| Project lifecycle and hole design | Dashboard, project management, rectangular bench plan, editable generated holes, saved drafts | Freeform irregular geometry, side profiles, 3D editing, and full geometry undo/redo are future work |
| Engineering calculations and traceability | Calculation service, saved result history, warnings, pattern snapshots, report PDF | Site coefficients and limits must be supplied and checked by a qualified specialist; there is no formal approval/revision workflow or complete audit log |
| Shared web/mobile field information | Project notes, comments, photos, Android API client | Android requires internet; changes are not queued offline. The proposed AI chat assistant is disabled and is not part of the working release |
| Delivery and quality | Two deployed Azure web apps, Azure SQL, automated tests, and a GitHub Actions workflow | The workflow builds and tests on pushes and pull requests; Azure deployment jobs require repository variables and publish-profile secrets |

This distinction matters when comparing the submission with the full requirements chapter: items described there as proposed or future work should not be treated as implemented in this phase.

## Architecture

```text
Browser ──HTTPS──> BlastPro.Mvc ──HTTPS + JWT──> BlastPro.Api ──EF Core──> SQL Server
                  cookie session                    Identity, rules,       LocalDB locally
                                                    calculations          Azure SQL hosted

Android app ───────────────HTTPS + JWT────────────> BlastPro.Api
```

The MVC project renders web pages and calls the API; it does not access the database directly. The API applies authentication, company/owner checks, validation, calculation logic, and Entity Framework Core migrations. Web sessions use a secure HTTP-only cookie that carries the API token; Android signs in to the API directly. The hosted web app and mobile build use the hosted API and its shared Azure SQL data.

| Path | Purpose |
| --- | --- |
| [`BlastPro/BlastPro/`](BlastPro/BlastPro/) | ASP.NET Core MVC views, controllers, web API client, CSS and JavaScript |
| [`BlastPro/BlastPro.Api/`](BlastPro/BlastPro.Api/) | REST API, Identity, EF Core entities/migrations, authorisation and calculations |
| [`BlastPro/BlastPro.Tests/`](BlastPro/BlastPro.Tests/) | Unit, integration, and end-to-end web/API tests |
| [`BlastPro.Android/app/`](BlastPro.Android/app/) | Kotlin Android application |
| [`BlastPro/docs/`](BlastPro/docs/) | Account, company, and database notes |

## Suggested marker walkthrough

Use a test company and non-sensitive demonstration data. A complete walk-through is:

1. Open the [hosted website](https://blastpro-web-2c61c74b-axf3gvbtg9a8f3hg.southafricanorth-01.azurewebsites.net/) and sign in. If no account was supplied, select **Create Company**. The new Main Company User is signed in immediately.
2. On **Dashboard**, create a project, then use search and status filtering. Open the project to see its details and move into **Pattern Design**.
3. Enter bench dimensions, rock data and site inputs; choose a pattern and product. Generate holes, edit one by clicking it on the plan, and remove or add a hole. Observe the live hole table and totals update.
4. Save the draft and open **Results**. Run the calculation from the saved design. Review totals, missing-input messages, and any warnings. Change the design and compare the older result with the new one to see the outdated state and result history.
5. Open **Reports** for the project. Preview the saved result and its hole plan; use **Print** or **Download PDF**.
6. As a Main Company User, open **Company** to inspect Blaster management and products/prices. A Blaster account should only see its own authorised projects.
7. In the Android app, sign in with an existing web account. Create or open a project note, add a comment/photo, then reopen that project's **Notes** on the web to see the shared record. **New Project** can create a project that appears on the web. **Settings** shows the API connection status and provides Refresh and Logout.

The Android companion is an online field notebook. It does not perform blast calculations. The current chat tab is hidden; the application does not provide an AI answer feature.

## Run locally

### Prerequisites

- Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB (normally installed with Visual Studio's data tools)
- Visual Studio with ASP.NET/web development support, or two PowerShell terminals
- For Android: Android Studio, Android SDK 35, and an installed JDK 21 selected as the Gradle JDK (minimum Android API 26)

From the repository root, open [`BlastPro/BlastPro.slnx`](BlastPro/BlastPro.slnx) and choose the shared **BlastPro API + Web** startup profile in Visual Studio. Start both projects. The browser should open the sign-in page. The API must be running for web sign-in and project actions to work.

For a terminal run, use two terminals in the repository root:

```powershell
# Terminal 1
dotnet run --project .\BlastPro\BlastPro.Api\BlastPro.Api.csproj --launch-profile BlastPro.Api
```

```powershell
# Terminal 2
dotnet run --project .\BlastPro\BlastPro\BlastPro.Mvc.csproj --launch-profile BlastPro.Mvc
```

| Service | Local URL |
| --- | --- |
| Website | <https://localhost:7001/Account/Login> |
| API | <https://localhost:7002/> |
| Swagger UI, Development only | <https://localhost:7002/swagger> |

If the browser warns about the local development certificate, run `dotnet dev-certs https --trust` and restart the applications.

### Local database and configuration

The Development API uses SQL Server LocalDB and applies committed migrations at startup. Roles are created automatically. The sample **Xploma** company is seeded in Development, but its administrator account is created only when `DevelopmentSeed:AdminPassword` is configured. The simplest first-use path is **Create Company** in the web app; there is no public Blaster registration.

The default LocalDB connection is in [`BlastPro.Api/appsettings.Development.json`](BlastPro/BlastPro.Api/appsettings.Development.json). If a teammate already has a conflicting database schema, give their development database a unique name without deleting existing data:

```powershell
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Server=(localdb)\MSSQLLocalDB;Database=BlastProApiDevYourName;Trusted_Connection=True;MultipleActiveResultSets=true' --project .\BlastPro\BlastPro.Api\BlastPro.Api.csproj
```

In Development the API creates and reuses a private local JWT signing key when none is configured. To choose your own, set `Jwt:Key` with API user secrets. The web client's API address is `ApiBaseUrl` in [`BlastPro.Mvc/appsettings.json`](BlastPro/BlastPro/appsettings.json) and defaults to `https://localhost:7002`. Keep database credentials, JWT keys, and real account passwords outside Git.

### Android

Open [`BlastPro.Android/`](BlastPro.Android/) in Android Studio and select JDK 21 for Gradle. Debug and release builds currently point to the hosted HTTPS API by default. A phone or emulator therefore needs internet, but does **not** need Visual Studio or the local API to stay running. Accounts must be created through the web app first.

To build an installable debug APK from PowerShell:

```powershell
cd .\BlastPro.Android
.\gradlew.bat :app:assembleDebug
```

The APK is written to `BlastPro.Android/app/build/outputs/apk/debug/app-debug.apk`. Install it using Android Studio or `adb install -r .\app\build\outputs\apk\debug\app-debug.apk` while in the Android project directory.

For an emulator talking to a **locally running** API instead, build with:

```powershell
.\gradlew.bat :app:assembleDebug -PblastProApiUrl=http://10.0.2.2:5002/
```

`10.0.2.2` is the emulator's route to the host computer and will not work on a physical phone. A release build requires an HTTPS API URL. The Settings screen shows whether the compiled API is online; it does not change the API address.

## Verification

Run the .NET suite from the repository root:

```powershell
dotnet test .\BlastPro\BlastPro.slnx
```

On 5 October 2026 the solution built and **88/88 .NET tests passed** (0 failed, 0 skipped). The separate JavaScript timing suite also passed **4/4 tests** with `node --test BlastPro/BlastPro.Tests/JavaScript/timing-sequences.test.cjs`. Coverage includes authentication, role/company/owner access, company account creation and Blaster limits, product prices and saved costs, notes and photo access, calculation results/history, reports, and MVC flows. Some transaction tests require Windows LocalDB; other tests use isolated in-memory data.

The Android debug build also completed successfully with `.\gradlew.bat :app:assembleDebug` on 5 October 2026. This confirms compilation; it does not claim that every phone model or hosted user flow was device-tested. The [GitHub Actions workflow](.github/workflows/ci-cd.yml) builds/tests the web/API and Android app on pushes and pull requests; it excludes tests tagged `LocalDb` on hosted runners. Run the full suite locally to include those tests.

## Deployment and operational notes

The current demonstration environment uses **two Azure App Service web apps** (MVC and API) and **Azure SQL**. The Android app calls the hosted API directly. The repository has a deployment workflow for pushes to `main`, after the test and Android jobs pass. Its deployment jobs run only when the matching Azure app-name variables are set and the publish-profile secrets are available. Until that one-time GitHub configuration is complete, publishing the repository does not update the live apps.

For another environment, configure the API's `ConnectionStrings__DefaultConnection` and `Jwt__Key` securely, and set the MVC app's `ApiBaseUrl` to that API's HTTPS URL. Use a stable, strong JWT key for the deployed API. The API applies EF Core migrations when it starts. Do not put these values in source control.

Free hosting may pause or start slowly. Local and hosted databases are separate; creating a local account does not create it on Azure. The hosted password-reset flow has no outbound email provider. In local Development, reset links are written to `BlastPro/BlastPro.Api/App_Data/PasswordReset`; see [login and password reset](BlastPro/docs/login-and-password-reset.md).

### GitHub Actions setup

The [pipeline](.github/workflows/ci-cd.yml) runs .NET and JavaScript tests on Windows, builds a debug Android APK on Linux, and uploads the test results and APK as artifacts. For Azure deployment, configure these in the GitHub repository under **Settings → Secrets and variables → Actions**:

| Type | Name | Purpose |
| --- | --- | --- |
| Variable | `AZURE_API_APP_NAME` | API App Service name |
| Variable | `AZURE_WEB_APP_NAME` | MVC App Service name |
| Secret | `AZURE_API_PUBLISH_PROFILE` | API App Service publish profile |
| Secret | `AZURE_WEB_PUBLISH_PROFILE` | MVC App Service publish profile |

The API's database connection string and JWT key stay in Azure App Service configuration. The team workflow is to create a `feature/*` branch, open a pull request into `main`, and merge after the checks pass. A push to `main` can then deploy to Azure when the configuration above is present.

## Known limits and future work

- Blast design starts from a rectangular bench planning area. The broader specification's irregular polygon editor, linked side profiles, full 3D design, and automatic prohibited-zone handling are not implemented.
- PPV uses site-provided calibration, and flyrock is an idealised trajectory screen. USBM residential guidance is a reference, not a South African legal or site-approved limit. The blaster must verify input values, warnings, timing and the approved exclusion radius.
- Historical reports from calculations saved before pattern snapshots cannot show the exact earlier hole plan. Current saved calculations include a snapshot.
- Android notes use live API requests. Offline save/queue/replay and an AI assistant are not implemented.
- Production email delivery and a formal design approval/audit workflow are not in this release. Azure deployment automation depends on the GitHub variables and secrets listed above.

## Further reading

- [Company, Blaster and profile flow](BlastPro/docs/company-accounts.md)
- [Login and local password reset](BlastPro/docs/login-and-password-reset.md)
- [Database model and migrations](BlastPro/docs/database-plan.md)

The full Task 1 requirements and presentation materials are maintained separately by the team. This README, the current source, and the commands above are the evidence for what this repository delivers now.
