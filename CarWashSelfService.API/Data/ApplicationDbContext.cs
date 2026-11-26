using Microsoft.EntityFrameworkCore;
using CarWashSelfService.API.Entities;

namespace CarWashSelfService.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<WashPost> WashPosts { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<WashSession> WashSessions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    }
}