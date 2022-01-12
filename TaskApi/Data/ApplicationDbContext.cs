using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        public DbSet<TaskList> TaskLists { get; set; }
        public DbSet<Task> Tasks { get; set; }
        
        public virtual DbSet<RefreshToken> RefreshTokens {get;set;}
    }
}