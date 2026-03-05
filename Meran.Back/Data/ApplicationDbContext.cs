using Microsoft.EntityFrameworkCore;
using Meran.Back.Models;

namespace Meran.Back.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Administrator> Administrators => Set<Administrator>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<ApplicationPlan> ApplicationPlans => Set<ApplicationPlan>();
        public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
        public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Administrator>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.Email)
                    .HasColumnName("email")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.PasswordHash)
                    .HasColumnName("password_hash")
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(x => x.DisplayName)
                    .HasColumnName("display_name")
                    .HasMaxLength(255);

                entity.Property(x => x.IsActive)
                    .HasColumnName("is_active");

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(x => x.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(x => x.Email)
                    .IsUnique();
            });

            modelBuilder.Entity<Application>(entity =>
            {
                entity.ToTable("applications");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.Description)
                    .HasColumnName("description")
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(x => x.Format)
                    .HasColumnName("format")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.OneShotPrice)
                    .HasColumnName("one_shot_price")
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at");
            });

            modelBuilder.Entity<ApplicationPlan>(entity =>
            {
                entity.ToTable("application_plans");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.ApplicationId)
                    .HasColumnName("application_id");

                entity.Property(x => x.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.Description)
                    .HasColumnName("description")
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(x => x.BillingPeriod)
                    .HasColumnName("billing_period")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.Price)
                    .HasColumnName("price")
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.Application)
                    .WithMany(a => a.Plans)
                    .HasForeignKey(x => x.ApplicationId);
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("application_users");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.ApplicationId)
                    .HasColumnName("application_id");

                entity.Property(x => x.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.Email)
                    .HasColumnName("email")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.Origin)
                    .HasColumnName("origin")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(x => x.Plan)
                    .HasColumnName("plan")
                    .HasMaxLength(255);

                entity.Property(x => x.IsActive)
                    .HasColumnName("is_active");

                entity.Property(x => x.LastPaymentAt)
                    .HasColumnName("last_payment_at");

                entity.Property(x => x.NextPaymentDueAt)
                    .HasColumnName("next_payment_due_at");

                entity.Property(x => x.PaymentProvider)
                    .HasColumnName("payment_provider")
                    .HasMaxLength(255);

                entity.Property(x => x.PaymentReference)
                    .HasColumnName("payment_reference")
                    .HasMaxLength(255);

                entity.Property(x => x.LastPaymentAmount)
                    .HasColumnName("last_payment_amount")
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.LastPaymentCurrency)
                    .HasColumnName("last_payment_currency")
                    .HasMaxLength(10);

                entity.Property(x => x.HasPaymentIssue)
                    .HasColumnName("has_payment_issue");

                entity.HasOne(x => x.Application)
                    .WithMany(a => a.Users)
                    .HasForeignKey(x => x.ApplicationId);

                entity.HasIndex(x => new { x.ApplicationId, x.Email })
                    .IsUnique();
            });

            modelBuilder.Entity<PaymentEvent>(entity =>
            {
                entity.ToTable("payment_events");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.ApplicationId)
                    .HasColumnName("application_id");

                entity.Property(x => x.ApplicationUserId)
                    .HasColumnName("application_user_id");

                entity.Property(x => x.Type)
                    .HasColumnName("type")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Currency)
                    .HasColumnName("currency")
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(x => x.OccurredAt)
                    .HasColumnName("occurred_at");

                entity.Property(x => x.Provider)
                    .HasColumnName("provider")
                    .HasMaxLength(255);

                entity.Property(x => x.ProviderReference)
                    .HasColumnName("provider_reference")
                    .HasMaxLength(255);

                entity.Property(x => x.RawPayload)
                    .HasColumnName("raw_payload");

                entity.HasOne(x => x.Application)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationId);

                entity.HasOne(x => x.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationUserId);

                entity.HasIndex(x => new { x.ApplicationId, x.ApplicationUserId, x.OccurredAt });
            });
        }
    }
}

