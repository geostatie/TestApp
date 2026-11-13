using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TestApp.Data;

/// <summary>
/// Used only by the dotnet-ef tool, e.g. <c>dotnet ef migrations add</c>.
/// </summary>
/// <remarks>
/// Without this, the tool runs Program.cs to find the DbContext, and Program.cs
/// refuses to start without a connection string. Generating a migration only
/// compares the model with the last snapshot, so it never opens a connection
/// and the placeholder below is never used.
/// </remarks>
internal sealed class ClinicDbDesignTimeFactory : IDesignTimeDbContextFactory<ClinicDb>
{
    public ClinicDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ClinicDb>()
            .UseSqlServer("Server=design-time-only;Database=clinicdb")
            .Options);
}
