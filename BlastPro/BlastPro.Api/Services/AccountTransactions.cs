using System.Data;
using BlastPro.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlastPro.Api.Services;

public static class AccountTransactions
{
    // Production uses SQL Server. The nonrelational branch supports the existing in-memory test host.
    public static async Task<IDbContextTransaction?> BeginAsync(ApplicationDbContext db, int? companyId = null)
    {
        if (!db.Database.IsRelational()) return null;
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (companyId.HasValue)
            {
                // Take an exclusive company lock BEFORE counting seats. All seat mutations use this
                // lock, including reactivation, so separate API processes cannot overbook a company.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [Companies] SET [UpdatedAtUtc] = [UpdatedAtUtc] WHERE [Id] = {companyId.Value}");
            }
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
