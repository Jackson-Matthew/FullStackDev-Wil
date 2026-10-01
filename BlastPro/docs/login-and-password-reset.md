# Login and password reset

## Run from Visual Studio

1. Open `BlastPro/BlastPro.slnx` from the repository root. Do not open the older
   nested solution inside the MVC project.
2. If the solution is already open, stop debugging and reopen it to load the shared
   launch profile.
3. Select **BlastPro** in the startup dropdown next to the green Run button.
4. Press **F5**. The API and MVC projects start together, and the browser opens
   `https://localhost:7001/Account/Login`.

The shared `BlastPro.slnxLaunch` profile starts the API first and then the MVC app.
If the profile is not visible, enable Multi-Project Launch Profiles in Visual Studio
Options and reopen the solution. You can also right-click the solution, choose
Configure Startup Projects, and set both BlastPro.Api and BlastPro.Mvc to Start.

The API uses the configured LocalDB database from development settings or user secrets.
Startup applies current migrations and seeds Identity roles. Sample company/admin
seeding runs only in Development. The MVC app calls the API and does not connect
directly to SQL Server.

The API runs at `https://localhost:7002`; Swagger is available at
`https://localhost:7002/swagger` but no longer opens a second browser window. Use an
existing confirmed account, the development seed account, or **Create Company**.
See [company account setup](company-accounts.md) for confirmations and Blaster invitations.
Transaction tests use isolated temporary LocalDB databases; authentication tests use in-memory data.

## Login behaviour

- Five failed password attempts lock the account for 15 minutes. A locked or
  inactive account receives the same invalid-login message as incorrect credentials.
- Email confirmation is required before sign-in. Inactive companies cannot sign in.
- Without Remember Me, the browser receives a session cookie, limited to 60 minutes
  or the API token's remaining lifetime, whichever is shorter.
- With Remember Me, the cookie persists across browser restarts until the API token
  expires (currently eight hours in the application configuration).
- Cookie expiry does not slide beyond token expiry. An API rejection clears the
  browser login and returns the user to sign in. Password resets invalidate previously
  issued API tokens; inactive users cannot keep using an existing token.
- API outages produce a retry message rather than an incorrect-password message.
- Logout is a POST protected by an antiforgery token and removes the browser cookie.

## Local password reset mailbox

Email delivery is not configured. In Development, Forgot Password saves a local HTML
message containing a working reset link. The confirmation page explicitly explains
that no email is sent. The response is identical for eligible, inactive and unknown
addresses; only active, confirmed accounts receive a message. A pending Blaster must
use their invitation to complete account setup.

1. Submit the email address on Forgot Password.
2. On the computer running the API, open
   `BlastPro/BlastPro.Api/App_Data/PasswordReset`.
3. Open the newest HTML message for that account and click Reset password.
4. Enter matching passwords with at least eight characters, uppercase and lowercase
   letters, a digit and a symbol. The link expires after one hour and works once.
5. Sign in with the new password. Delete the local message when finished.

The mailbox is outside the web root, is ignored by Git and does not log reset tokens.
Anyone with filesystem access to a message can use its link while valid, so use the
mailbox only on a developer machine and do not share its contents.

The API's `appsettings.Development.json` enables this with
`PasswordReset:DevelopmentMailboxEnabled` and sets `PasswordReset:FrontendUrl` to
`https://localhost:7001`. Update that URL if the MVC port changes. The mailbox accepts
only HTTPS loopback destinations. It refuses to operate outside Development, even if
the flag is enabled. Until a real delivery service is configured, other environments
show that password reset is unavailable instead of claiming an email was sent.

API reset callers must use the Base64 URL encoded token from the generated link,
not the raw Identity token. Missing, expired and reused links provide a route back to
Forgot Password. Password-policy errors are displayed as readable messages.

## Verification

```powershell
dotnet test BlastPro/BlastPro.slnx
```

The tests exercise the real MVC pages, antiforgery protection, cookies, API endpoints,
JWT validation and Identity password hashing against an isolated in-memory database.
They cover login, lockout, inactive accounts, Remember Me, token forwarding, logout,
protected redirects, expired cookies and tokens, API outages, generic reset responses,
password validation, reset expiry and reuse, revoked sessions and the local mailbox.

SQL Server migration behaviour and actual email delivery are not covered by these
authentication tests. The production email provider remains a separate setup task.
