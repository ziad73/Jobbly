using Microsoft.EntityFrameworkCore;

namespace Jobbly.Application.Common;

// This interface is how the Application layer talks to the database without
// depending on the Infrastructure project. It exposes the DbSet directly, so
// services get the full power of EF Core and LINQ - no repository in between.
public interface IJobblyDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
