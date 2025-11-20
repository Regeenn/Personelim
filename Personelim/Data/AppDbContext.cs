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
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(1000);

                // Business - Owner ilişkisi
                entity.HasOne(b => b.Owner)
                    .WithMany(u => u.OwnedBusinesses)
                    .HasForeignKey(b => b.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Business - Province ilişkisi
                entity.HasOne(b => b.Province)
                    .WithMany()
                    .HasForeignKey(b => b.ProvinceId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Business - District ilişkisi
                entity.HasOne(b => b.District)
                    .WithMany()
                    .HasForeignKey(b => b.DistrictId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Business - ParentBusiness (Self-referencing) ilişkisi - Alt İşletme Desteği
                entity.HasOne(b => b.ParentBusiness)
                    .WithMany(b => b.SubBusinesses)
                    .HasForeignKey(b => b.ParentBusinessId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // BusinessMember Configuration
            modelBuilder.Entity<BusinessMember>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Role)
                    .HasConversion<string>();

                // Composite unique index (bir kullanıcı aynı işletmede sadece bir kez üye olabilir)
                entity.HasIndex(e => new { e.UserId, e.BusinessId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.BusinessMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Members)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            // Province Configuration
            modelBuilder.Entity<Province>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // District Configuration
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

            // PasswordResetToken Configuration
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
                entity.Property(e => e.ExpiresAt).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}