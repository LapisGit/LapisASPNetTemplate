using LapisASPNetTemplate.Models;
using Microsoft.EntityFrameworkCore;

namespace LapisASPNetTemplate;

public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options) : base(options) { }
    
    public DbSet<Account> Accounts { get; set; }
    public DbSet<TokenExpiry> TokenExpiries { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.Email).ValueGeneratedNever();
            entity.Property(e => e.AccountId).ValueGeneratedOnAdd();
            entity.Property(e => e.AccountName).IsRequired();
            entity.Property(e => e.Password).IsRequired();
            entity.ToTable("accounts");
        });
        
        modelBuilder.Entity<TokenExpiry>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).ValueGeneratedNever();
            entity.Property(e => e.ExpiredAtUnix).IsRequired();
            entity.ToTable("token_expiries");
        });
    }
}