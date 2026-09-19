# BlastPro Task 2 Database

## Development database

Task 2 uses Entity Framework Core with SQL Server LocalDB. The development database is named `BlastProTask2`. The connection string is stored in `BlastPro/appsettings.Development.json` and is used only in the Development environment.

Azure is not configured in this phase. The project uses the EF Core SQL Server provider so a future Azure SQL connection can use the same entities and migrations.

## Page to data mapping

| Page or feature | Main database records |
| --- | --- |
| Login and forgot password | ASP.NET Core Identity tables |
| Profile | `AspNetUsers` and `Companies` |
| Dashboard | `BlastProjects`, `AspNetUsers` and `Companies` |
| Create Project | `BlastProjects` |
| Pattern Design | `BlastProjects`, `BlastHoles` and `ExplosiveProducts` |
| Results | `CalculationResults` and `BlastWarnings` |
| Report | Projects, holes, calculations, warnings, user and company data |
| Company | `Companies` and `AspNetUsers` |
| Add, view and edit Blaster | `AspNetUsers`, `AspNetRoles` and `AspNetUserRoles` |

## Application tables

- `Companies`: company identity and contact information.
- `AspNetUsers`: application users, profiles, company membership and account state.
- `AspNetRoles`: contains the `MainCompanyUser` and `Blaster` roles.
- `BlastProjects`: project details, pattern parameters, owner, company and project state.
- `BlastHoles`: hole coordinates, depth, explosive, charge, stemming and delay.
- `ExplosiveProducts`: company explosive choices and prices used for cost calculations.
- `CalculationResults`: saved calculation outputs for a project.
- `BlastWarnings`: information, warning and critical messages linked to a calculation result.

The additional `AspNet*` tables are standard ASP.NET Core Identity tables for roles, claims, logins, tokens and password security.

## Important rules

- Every user belongs to one company.
- Every project belongs to one company and one owner.
- A project is hidden through soft deletion instead of being physically deleted.
- Hole numbers are unique inside a project.
- User email addresses are unique.
- Certification IDs are unique within a company when supplied.
- Explosive-product names are unique within a company.
- Numeric database constraints reject invalid negative or zero values where appropriate.
- Project row-version data provides optimistic concurrency protection.
- Deleting a calculation deletes its warnings.
- Deleting a project record would delete its holes and calculation results, although the application normally uses soft deletion.

## Migrations

Restore the repository-scoped EF tool:

```powershell
dotnet tool restore
```

Create a migration after changing the EF model:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project BlastPro/BlastPro.csproj --startup-project BlastPro/BlastPro.csproj --output-dir Data/Migrations
```

Apply pending migrations to LocalDB:

```powershell
dotnet tool run dotnet-ef database update --project BlastPro/BlastPro.csproj --startup-project BlastPro/BlastPro.csproj
```

Do not edit the LocalDB schema manually. Change the entity model, create a migration, review it and apply it.

## Calculation boundary

The database contains fields for the results visible in the design, including powder factor, PPV and flyrock. Their formulas and required inputs must be approved separately before the calculation service is implemented. The database does not hard-code or invent engineering results.
