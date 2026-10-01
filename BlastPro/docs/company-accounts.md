# Company and account setup

A Company is a database record. Its Main Company User uses the same Sign In page as Blasters. Public company setup creates exactly one Main Company User, assigns the role on the server, and signs that user in immediately.

## Local walkthrough

1. Open `BlastPro.slnx` in the `BlastPro` folder. In Visual Studio, select the shared **BlastPro API + Web** launch profile and run it. Both `BlastPro.Api` (https://localhost:7002) and `BlastPro.Mvc` (https://localhost:7001) must be running. If using the command line, run `dotnet run --project BlastPro.Api/BlastPro.Api.csproj --launch-profile BlastPro.Api` and `dotnet run --project BlastPro/BlastPro.Mvc.csproj --launch-profile BlastPro.Mvc` in separate terminals from this folder. Then choose **Create Company** beside **Sign In**.
2. Enter company name, registration number, contact email/phone, optional address, and the first main user's name, email, and password.
3. Submit **Create Company**. The Main Company User is signed in and taken straight to the Dashboard; no email confirmation is needed.
4. Open **Company → Add Blaster**. Enter name, email, contact phone, and certification ID. No password is collected from the main user.
5. Open the invited Blaster's message in the same development mailbox. The Blaster sets and confirms their own password, which also confirms control of their email, then uses the shared Sign In page.
6. Use Company to edit contact details, edit Blaster details, deactivate/reactivate accounts, or resend pending invitations. Use Profile to edit your own permitted personal details.

Blaster invitations use the development mailbox and the existing `PasswordReset:DevelopmentMailboxEnabled` and `PasswordReset:FrontendUrl` settings. It is enabled only in Development and only with an HTTPS loopback frontend URL. Messages are private local HTML files excluded from Git; no real email is sent. Password reset messages remain in `App_Data/PasswordReset`.

Invitation links expire after one hour. A main user can resend a pending Blaster invitation from Company. Resending invalidates the previous invitation. Inactive Blasters cannot accept invitations or sign in. Completed invitations cannot be reused or used as password reset tokens. Main users created before this change can sign in without confirming their email.

## Permissions and seats

- Main Company Users see their company's projects and Company administration. They cannot administer another company or another company's Blasters.
- Blasters see only projects they own and cannot open Company administration.
- Project, pattern, results, and report data retain company and ownership checks in the API. Reports retrieve data through those protected endpoints.
- Company edits permit contact email, contact phone, and address. Company identity and registration number are read-only.
- Blaster edits permit full name, contact phone, and certification ID. Account email, company, and role are read-only. Only the main user can change a Blaster's active status through the dedicated status endpoint.
- Personal profile edits permit full name, contact phone, nickname, gender, country, and time zone. They cannot change company, role, active status, certification, or email.
- Five active Blasters are allowed per company. Pending active invitations count towards the limit. The main user's account does not consume a Blaster seat.
- Deactivation frees a seat and retains all designs. It revokes existing JWT sessions permanently; a reactivated user must sign in again. Reactivation checks the limit again.

Company creation and Identity role assignment share one SQL transaction. Invitation creation and delivery also share a transaction. A failed setup saves neither record; failed invitation delivery saves no Blaster. Seat-changing requests acquire an exclusive company row lock before counting seats, inside a serializable SQL transaction, preventing concurrent API processes from overbooking.

Registration numbers are trimmed and converted to uppercase. The existing unique database indexes enforce company registration numbers, normalized Identity emails, and certification IDs within a company. No schema migration is required by this feature.

## API routes

| Method | Route | Access |
| --- | --- | --- |
| POST | `/api/companies` | Public company setup |
| POST | `/api/auth/accept-invitation` | Blaster invitation and password setup |
| GET / PUT | `/api/company` | Main user; current company |
| POST | `/api/company/blasters` | Main user; company taken from signed-in identity |
| GET / PUT | `/api/company/blasters/{id}` | Main user; current company's Blasters |
| PUT | `/api/company/blasters/{id}/status` | Main user; explicit `isActive` required |
| POST | `/api/company/blasters/{id}/invitation` | Main user; resend pending invitation |
| GET / PUT | `/api/profile` | Signed-in user's own profile |

All MVC forms require anti-forgery tokens. Public link pages use no-store responses and no-referrer headers; GET requests display forms without consuming links. Tokens are generated by ASP.NET Identity, encoded for URLs, never returned from setup APIs, and never written to logs.

## Tests and deployment

Run `dotnet test BlastPro/BlastPro.Tests/BlastPro.Tests.csproj` from the repository root. SQL transaction tests require Windows SQL Server LocalDB and use temporary `BlastProAccountTests_<guid>` databases, deleted on completion. Other integration tests use the existing in-memory provider. Test hosts use ephemeral data protection keys and do not require access to the developer's key store.

Tests cover setup validation and uniqueness, rollback on role failure, immediate sign in, invitation expiry/replay, concurrent creation/reactivation at capacity, deactivation and session revocation, profile field restrictions, company/owner access, and MVC form flows.

Sample company and admin account seeding runs only in Development. Identity role seeding remains available in all environments. Before production deployment, replace `IAccountEmailDelivery` and `IPasswordResetDelivery` with configured production delivery implementations. The current local implementations reject production use; account setup fails without saving records when delivery is unavailable.
