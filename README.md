# FullStackDev-Wil
Full stack repo for wil group 
You Know Who Else Has A Full Stack...
Whos JSON

## Run BlastPro locally

Install the .NET 10 SDK and SQL Server LocalDB, then open `BlastPro/BlastPro.slnx` in Visual Studio. Start both `BlastPro.Api` and `BlastPro.Mvc` with their Development profiles. The API uses `https://localhost:7002` and MVC uses `https://localhost:7001`. MVC calls the API; it does not connect to SQL Server directly.

The API applies the committed EF Core migrations to your own LocalDB database on startup. If startup reports that a table such as `AspNetRoles` already exists, your local database schema and migration history do not match. Keep any data you need; use a different local database name instead of deleting the old database. From the `BlastPro` directory, run:

```powershell
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Server=(localdb)\MSSQLLocalDB;Database=BlastProApiDevYourName;Trusted_Connection=True;MultipleActiveResultSets=true' --project BlastPro.Api/BlastPro.Api.csproj
```

Replace `YourName` with your own identifier and restart the API. The longer project guides are kept outside this repository.
