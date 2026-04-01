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
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<ApplicationFeature> ApplicationFeatures => Set<ApplicationFeature>();
        public DbSet<ApplicationPlanFeatureValue> ApplicationPlanFeatureValues => Set<ApplicationPlanFeatureValue>();
        public DbSet<ApplicationUserRole> ApplicationUserRoles => Set<ApplicationUserRole>();
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

                entity.HasMany(x => x.Features)
                    .WithOne(x => x.Application)
                    .HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                    .HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
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

                entity.Property(x => x.IsActive)
                    .HasColumnName("is_active");

                entity.HasOne(x => x.Application)
                    .WithMany(a => a.Users)
                    .HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.ApplicationId, x.Email })
                    .IsUnique();
            });

            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.ToTable("subscriptions");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.ApplicationUserId)
                    .HasColumnName("application_user_id");

                entity.Property(x => x.ApplicationPlanId)
                    .HasColumnName("application_plan_id");

                entity.Property(x => x.Status)
                    .HasColumnName("status")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(x => x.EndedAt)
                    .HasColumnName("ended_at");

                entity.Property(x => x.CurrentPeriodEnd)
                    .HasColumnName("current_period_end");

                entity.Property(x => x.TrialEndAt)
                    .HasColumnName("trial_end_at");

                entity.HasOne(x => x.ApplicationUser)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ApplicationPlan)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.ApplicationPlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.ApplicationUserId, x.Status });
            });

            modelBuilder.Entity<ApplicationFeature>(entity =>
            {
                entity.ToTable("application_features");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.ApplicationId)
                    .HasColumnName("application_id");

                entity.Property(x => x.Key)
                    .HasColumnName("key")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.Type)
                    .HasColumnName("type")
                    .HasConversion<string>()
                    .IsRequired();

                entity.HasOne(x => x.Application)
                    .WithMany(x => x.Features)
                    .HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.ApplicationId, x.Key }).IsUnique();
            });

            modelBuilder.Entity<ApplicationPlanFeatureValue>(entity =>
            {
                entity.ToTable("application_plan_feature_values");
                entity.HasKey(x => new { x.ApplicationPlanId, x.ApplicationFeatureId });

                entity.Property(x => x.ApplicationPlanId)
                    .HasColumnName("application_plan_id");

                entity.Property(x => x.ApplicationFeatureId)
                    .HasColumnName("application_feature_id");

                entity.Property(x => x.Value)
                    .HasColumnName("value")
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(x => x.ApplicationPlan)
                    .WithMany(x => x.FeatureValues)
                    .HasForeignKey(x => x.ApplicationPlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ApplicationFeature)
                    .WithMany(x => x.PlanValues)
                    .HasForeignKey(x => x.ApplicationFeatureId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ApplicationUserRole>(entity =>
            {
                entity.ToTable("application_user_roles");
                entity.HasKey(x => new { x.ApplicationUserId, x.Role });

                entity.Property(x => x.ApplicationUserId)
                    .HasColumnName("application_user_id");

                entity.Property(x => x.Role)
                    .HasColumnName("role")
                    .HasConversion<string>()
                    .IsRequired();

                entity.HasOne(x => x.ApplicationUser)
                    .WithMany(x => x.Roles)
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PaymentEvent>(entity =>
            {
                entity.ToTable("payment_events");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.ApplicationId)
                    .HasColumnName("application_id");

                entity.Property(x => x.SubscriptionId)
                    .HasColumnName("subscription_id");

                entity.Ignore(x => x.ApplicationUserId);
                entity.Ignore(x => x.Type);

                entity.Property(x => x.EventType)
                    .HasColumnName("event_type")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasMaxLength(100);

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
                    .HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Subscription)
                    .WithMany(x => x.PaymentEvents)
                    .HasForeignKey(x => x.SubscriptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.ApplicationId, x.SubscriptionId, x.OccurredAt });
            });
        }
    }
}

