using Microsoft.EntityFrameworkCore;

namespace AuthorizationServer.Data
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
            : base(options)
        {

        }
    }
}
