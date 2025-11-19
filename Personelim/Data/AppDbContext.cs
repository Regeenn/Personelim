using Microsoft.EntityFrameworkCore;
using Personelim.Models;

namespace Personelim.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessMember> BusinessMembers { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Business Configuration
            modelBuilder.Entity<Business>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.Owner)
                    .WithMany(u => u.OwnedBusinesses)
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // BusinessMember Configuration
            modelBuilder.Entity<BusinessMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Role)
                    .HasConversion<string>();
                
                entity.HasIndex(e => new { e.UserId, e.BusinessId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.BusinessMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Members)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Invitation Configuration
            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.InvitationCode).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);

                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Invitations)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.InvitedBy)
                    .WithMany()
                    .HasForeignKey(e => e.InvitedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<Province>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // District configuration
            modelBuilder.Entity<District>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
                entity.HasOne(d => d.Province)
                    .WithMany(p => p.Districts)
                    .HasForeignKey(d => d.ProvinceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Business configuration
            modelBuilder.Entity<Business>(entity =>
            {
                entity.HasKey(e => e.Id);
            
                entity.HasOne(b => b.Province)
                    .WithMany()
                    .HasForeignKey(b => b.ProvinceId)
                    .OnDelete(DeleteBehavior.Restrict);
            
                entity.HasOne(b => b.District)
                    .WithMany()
                    .HasForeignKey(b => b.DistrictId)
                    .OnDelete(DeleteBehavior.Restrict);
            
                entity.HasOne(b => b.Owner)
                    .WithMany()
                    .HasForeignKey(b => b.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
        
    }
}