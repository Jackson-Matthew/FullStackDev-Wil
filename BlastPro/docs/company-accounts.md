# Company and account setup

A Company is a database record. Its Main Company User and Blasters use the same Sign In page. Public company setup creates the company and its first Main Company User together, assigns the role on the server, and signs that user in immediately.

## Local walkthrough

1. Open the outer `BlastPro.slnx` solution and start both `BlastPro.Api` and `BlastPro.Mvc` with the shared **BlastPro** launch profile.
2. On **Create Company**, enter the company details and the first main user's name, email, and password. The main user goes straight to the Dashboard.
3. Open **Company → Add New Blaster**. Enter the Blaster's email and password. The account is active immediately; give the sign-in details to the Blaster securely. No invitation email or confirmation link is sent.
4. Use **Edit** to add or update the Blaster's name, contact phone, and certification ID. Until a name is supplied, the account displays its email as its name.
5. Use **Set password** if a Blaster forgets their password or if an older Blaster account was created under the retired invitation flow. Setting a new password ends previous sessions.
6. Use Company to edit contact details or deactivate/reactivate Blasters. Use Profile to edit your own permitted personal details.

## Permissions and seats

- Main Company Users see their company's projects and Company administration. They cannot administer another company or another company's Blasters.
- Blasters see only projects they own and cannot open Company administration.
- Company edits permit contact email, contact phone, and address. Company identity and registration number are read-only.
- Blaster edits permit full name, contact phone, and certification ID. Account email, company, and role are read-only. Only the main user can change a Blaster's status or set their password.
- Five active Blasters are allowed per company. The main user's account does not consume a Blaster seat.
- Deactivation frees a seat and retains designs. It revokes existing sessions; a reactivated user must sign in again. Reactivation checks the seat limit.

Passwords are checked against the ASP.NET Identity policy: at least eight characters with uppercase and lowercase letters, a digit, and a symbol. Passwords are hashed by Identity and are never returned by the API or shown on the Company page. The main user must communicate the initial password to the Blaster securely.

Company creation, Blaster creation, and role assignment use SQL transactions. A failed account setup does not consume a seat. Seat-changing requests acquire an exclusive company row lock before counting seats, preventing concurrent requests from overbooking. The existing unique indexes enforce company registration numbers, normalized account emails, and certification IDs within a company. No schema migration is needed for this flow.

## API routes

| Method | Route | Access |
| --- | --- | --- |
| POST | `/api/companies` | Public company setup |
| GET / PUT | `/api/company` | Main user; current company |
| POST | `/api/company/blasters` | Main user; email and password required; company and role come from the signed-in identity |
| GET / PUT | `/api/company/blasters/{id}` | Main user; current company's Blasters |
| PUT | `/api/company/blasters/{id}/password` | Main user; set or reset a Blaster password |
| PUT | `/api/company/blasters/{id}/status` | Main user; explicit `isActive` required |
| GET / PUT | `/api/profile` | Signed-in user's own profile |

All MVC forms require antiforgery tokens. The retired invitation API and pages are removed.

## Tests and deployment

Run `dotnet test BlastPro/BlastPro.slnx` from the repository root. SQL transaction tests require Windows SQL Server LocalDB and use temporary databases deleted after each test. Other integration tests use isolated in-memory data.

The tests cover account setup, direct Blaster sign-in, password policy, password setting and session revocation, legacy account recovery, seat limits, company isolation, and MVC forms.

Creating and setting Blaster passwords does not need an email service. **Forgot Password** still saves links only to the development mailbox; it is unavailable in a hosted production environment until a separate recovery method is provided. This also affects a Main Company User who forgets their password after signing out.
