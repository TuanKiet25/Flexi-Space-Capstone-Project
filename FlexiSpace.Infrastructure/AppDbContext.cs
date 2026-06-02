using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserOTP> UserOTPs { get; set; }
        public virtual DbSet<Profile> Profiles { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
