using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BBEIDataAccess.Models;
using BBEIDataAccess;

namespace BBEIDataAccess.Models
{
    public partial class BBEIContext : IdentityDbContext<ApplicationUser>
    {
        public BBEIContext()
        {
        }

        public BBEIContext(DbContextOptions<BBEIContext> options)
            : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(null);
            }
        }

        public virtual DbSet<dbc_BBEIAuthors> dbc_BBEIAuthors { get; set; }
        public virtual DbSet<dbc_BBEIFacts> dbc_BBEIFacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<dbc_BBEIAuthors>(entity =>
            {
                entity.Property(e => e.Id).HasAnnotation("Sqlite:Autoincrement", true);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<dbc_BBEIFacts>(entity =>
            {
                entity.Property(e => e.Id).HasAnnotation("Sqlite:Autoincrement", true);

                entity.Property(e => e.Question)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.Reply)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.IdAuthor)
                    .IsRequired();

                entity.Property(e => e.DateFact)
                    .IsRequired();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}