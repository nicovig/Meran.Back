using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;

namespace Meran.Back.Services
{
    public interface IApplicationService
    {
        Task<List<ApplicationDto>> GetAllAsync(CancellationToken cancellationToken);
        Task<ApplicationDto> CreateAsync(CreateApplicationRequestDto request, CancellationToken cancellationToken);
        Task<ApplicationDto?> UpdateAsync(Guid id, UpdateApplicationRequestDto request, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
        Task<ApplicationUserDto?> AddUserAsync(Guid applicationId, AddApplicationUserRequestDto request, CancellationToken cancellationToken);
        Task<List<SubscriptionDto>?> GetSubscriptionsAsync(Guid applicationId, Guid applicationUserId, CancellationToken cancellationToken);
        Task<SubscriptionDto?> CreateSubscriptionAsync(Guid applicationId, Guid applicationUserId, CreateSubscriptionRequestDto request, CancellationToken cancellationToken);
        Task<List<ApplicationFeatureDto>?> GetFeaturesAsync(Guid applicationId, CancellationToken cancellationToken);
        Task<List<ApplicationFeatureDto>?> UpsertFeaturesAsync(Guid applicationId, UpsertApplicationFeaturesRequestDto request, CancellationToken cancellationToken);
    }

    public class ApplicationService : IApplicationService
    {
        private readonly ApplicationDbContext _dbContext;

        public ApplicationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ApplicationDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            var apps = await _dbContext.Applications
                .AsNoTracking()
                .Include(a => a.Plans)
                .Include(a => a.Users)
                .OrderBy(a => a.Name)
                .ToListAsync(cancellationToken);

            return apps.Select(ToDto).ToList();
        }

        public async Task<ApplicationDto> CreateAsync(CreateApplicationRequestDto request, CancellationToken cancellationToken)
        {
            var format = ParseFormat(request.Format);

            var app = new Application
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Format = format,
                OneShotPrice = request.OneShotPrice,
                CreatedAt = DateTime.UtcNow
            };

            if (request.Plans != null && request.Plans.Count > 0)
            {
                foreach (var planDto in request.Plans)
                {
                    var period = ParseBillingPeriod(planDto.BillingPeriod);

                    app.Plans.Add(new ApplicationPlan
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = app.Id,
                        Name = planDto.Name,
                        Description = planDto.Description,
                        BillingPeriod = period,
                        Price = planDto.Price
                    });
                }
            }

            _dbContext.Applications.Add(app);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(app);
        }

        public async Task<ApplicationDto?> UpdateAsync(Guid id, UpdateApplicationRequestDto request, CancellationToken cancellationToken)
        {
            var app = await _dbContext.Applications
                .Include(a => a.Plans)
                .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (app == null)
            {
                return null;
            }

            if (request.Plans != null)
            {
                _dbContext.ApplicationPlans.RemoveRange(app.Plans);
                await _dbContext.SaveChangesAsync(cancellationToken);

                foreach (var planDto in request.Plans)
                {
                    var period = ParseBillingPeriod(planDto.BillingPeriod);

                    var newPlan = new ApplicationPlan
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = app.Id,
                        Name = planDto.Name,
                        Description = planDto.Description,
                        BillingPeriod = period,
                        Price = planDto.Price
                    };

                    _dbContext.ApplicationPlans.Add(newPlan);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            app.Name = request.Name;
            app.Description = request.Description;
            app.Format = ParseFormat(request.Format);
            app.OneShotPrice = request.OneShotPrice;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _dbContext.Entry(app).Collection(a => a.Users).LoadAsync(cancellationToken);

            return ToDto(app);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var app = await _dbContext.Applications
                .Include(a => a.Users)
                .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (app == null)
            {
                return false;
            }

            if (app.Users.Any())
            {
                return false;
            }

            _dbContext.Applications.Remove(app);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<ApplicationUserDto?> AddUserAsync(Guid applicationId, AddApplicationUserRequestDto request, CancellationToken cancellationToken)
        {
            var app = await _dbContext.Applications
                .Include(a => a.Users)
                .SingleOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (app == null)
            {
                return null;
            }

            var email = request.Email.Trim().ToLowerInvariant();

            var exists = await _dbContext.ApplicationUsers
                .AnyAsync(x => x.ApplicationId == applicationId && x.Email == email, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("User with this email already exists for this application.");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Name = request.Name,
                Email = email,
                Origin = UserOrigin.Admin,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ApplicationUsers.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(user);
        }

        public async Task<List<SubscriptionDto>?> GetSubscriptionsAsync(Guid applicationId, Guid applicationUserId, CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.ApplicationUsers
                .AnyAsync(x => x.ApplicationId == applicationId && x.Id == applicationUserId, cancellationToken);

            if (!userExists)
            {
                return null;
            }

            var subscriptions = await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(x => x.ApplicationUserId == applicationUserId)
                .OrderByDescending(x => x.StartedAt)
                .ToListAsync(cancellationToken);

            return subscriptions.Select(ToDto).ToList();
        }

        public async Task<SubscriptionDto?> CreateSubscriptionAsync(Guid applicationId, Guid applicationUserId, CreateSubscriptionRequestDto request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.ApplicationUsers
                .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.Id == applicationUserId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var planExists = await _dbContext.ApplicationPlans
                .AnyAsync(x => x.ApplicationId == applicationId && x.Id == request.ApplicationPlanId, cancellationToken);

            if (!planExists)
            {
                throw new InvalidOperationException("Plan does not belong to this application.");
            }

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = applicationUserId,
                ApplicationPlanId = request.ApplicationPlanId,
                Status = ParseSubscriptionStatus(request.Status),
                StartedAt = request.StartedAt,
                EndedAt = request.EndedAt,
                CurrentPeriodEnd = request.CurrentPeriodEnd,
                TrialEndAt = request.TrialEndAt
            };

            _dbContext.Subscriptions.Add(subscription);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(subscription);
        }

        public async Task<List<ApplicationFeatureDto>?> GetFeaturesAsync(Guid applicationId, CancellationToken cancellationToken)
        {
            var appExists = await _dbContext.Applications.AnyAsync(x => x.Id == applicationId, cancellationToken);
            if (!appExists)
            {
                return null;
            }

            var features = await _dbContext.ApplicationFeatures
                .AsNoTracking()
                .Include(x => x.PlanValues)
                .Where(x => x.ApplicationId == applicationId)
                .OrderBy(x => x.Key)
                .ToListAsync(cancellationToken);

            return features.Select(ToDto).ToList();
        }

        public async Task<List<ApplicationFeatureDto>?> UpsertFeaturesAsync(Guid applicationId, UpsertApplicationFeaturesRequestDto request, CancellationToken cancellationToken)
        {
            var appExists = await _dbContext.Applications.AnyAsync(x => x.Id == applicationId, cancellationToken);
            if (!appExists)
            {
                return null;
            }

            var validPlanIds = await _dbContext.ApplicationPlans
                .Where(x => x.ApplicationId == applicationId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var validPlanSet = validPlanIds.ToHashSet();

            foreach (var feature in request.Features)
            {
                var hasInvalidPlan = feature.PlanValues.Any(x => !validPlanSet.Contains(x.ApplicationPlanId));
                if (hasInvalidPlan)
                {
                    throw new InvalidOperationException("One or more plan ids do not belong to this application.");
                }
            }

            var existingFeatures = await _dbContext.ApplicationFeatures
                .Include(x => x.PlanValues)
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync(cancellationToken);

            _dbContext.ApplicationPlanFeatureValues.RemoveRange(existingFeatures.SelectMany(x => x.PlanValues));
            _dbContext.ApplicationFeatures.RemoveRange(existingFeatures);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var featureRequest in request.Features)
            {
                var feature = new ApplicationFeature
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    Key = featureRequest.Key,
                    Type = ParseFeatureType(featureRequest.Type)
                };

                foreach (var planValue in featureRequest.PlanValues)
                {
                    feature.PlanValues.Add(new ApplicationPlanFeatureValue
                    {
                        ApplicationPlanId = planValue.ApplicationPlanId,
                        ApplicationFeatureId = feature.Id,
                        Value = planValue.Value
                    });
                }

                _dbContext.ApplicationFeatures.Add(feature);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await GetFeaturesAsync(applicationId, cancellationToken);
        }

        private static ApplicationDto ToDto(Application app)
        {
            return new ApplicationDto
            {
                Id = app.Id,
                Name = app.Name,
                Description = app.Description,
                Format = FormatToString(app.Format),
                OneShotPrice = app.OneShotPrice,
                CreatedAt = app.CreatedAt,
                Plans = app.Plans.Select(ToDto).ToList(),
                Users = app.Users.Select(ToDto).ToList()
            };
        }

        private static ApplicationPlanDto ToDto(ApplicationPlan plan)
        {
            return new ApplicationPlanDto
            {
                Name = plan.Name,
                Description = plan.Description,
                BillingPeriod = BillingPeriodToString(plan.BillingPeriod),
                Price = plan.Price
            };
        }

        private static ApplicationUserDto ToDto(ApplicationUser user)
        {
            return new ApplicationUserDto
            {
                Id = user.Id,
                ApplicationId = user.ApplicationId,
                Name = user.Name,
                Email = user.Email,
                Origin = UserOriginToString(user.Origin),
                CreatedAt = user.CreatedAt,
                Plan = user.Subscriptions
                    .OrderByDescending(x => x.StartedAt)
                    .Select(x => x.ApplicationPlan.Name)
                    .FirstOrDefault()
            };
        }

        private static ApplicationFormat ParseFormat(string value)
        {
            return value switch
            {
                "free" => ApplicationFormat.Free,
                "oneShot" => ApplicationFormat.OneShot,
                "subscription" => ApplicationFormat.Subscription,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported application format.")
            };
        }

        private static string FormatToString(ApplicationFormat format)
        {
            return format switch
            {
                ApplicationFormat.Free => "free",
                ApplicationFormat.OneShot => "oneShot",
                ApplicationFormat.Subscription => "subscription",
                _ => "free"
            };
        }

        private static BillingPeriod ParseBillingPeriod(string value)
        {
            return value switch
            {
                "monthly" => BillingPeriod.Monthly,
                "quarterly" => BillingPeriod.Quarterly,
                "semiannual" => BillingPeriod.Semiannual,
                "annual" => BillingPeriod.Annual,
                "biennial" => BillingPeriod.Biennial,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported billing period.")
            };
        }

        private static string BillingPeriodToString(BillingPeriod period)
        {
            return period switch
            {
                BillingPeriod.Monthly => "monthly",
                BillingPeriod.Quarterly => "quarterly",
                BillingPeriod.Semiannual => "semiannual",
                BillingPeriod.Annual => "annual",
                BillingPeriod.Biennial => "biennial",
                _ => "monthly"
            };
        }

        private static string UserOriginToString(UserOrigin origin)
        {
            return origin switch
            {
                UserOrigin.Admin => "admin",
                UserOrigin.Self => "self",
                _ => "admin"
            };
        }

        private static SubscriptionDto ToDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                ApplicationUserId = subscription.ApplicationUserId,
                ApplicationPlanId = subscription.ApplicationPlanId,
                Status = SubscriptionStatusToString(subscription.Status),
                StartedAt = subscription.StartedAt,
                EndedAt = subscription.EndedAt,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                TrialEndAt = subscription.TrialEndAt
            };
        }

        private static ApplicationFeatureDto ToDto(ApplicationFeature feature)
        {
            return new ApplicationFeatureDto
            {
                Id = feature.Id,
                Key = feature.Key,
                Type = FeatureTypeToString(feature.Type),
                PlanValues = feature.PlanValues
                    .Select(x => new ApplicationFeaturePlanValueDto
                    {
                        ApplicationPlanId = x.ApplicationPlanId,
                        Value = x.Value
                    })
                    .OrderBy(x => x.ApplicationPlanId)
                    .ToList()
            };
        }

        private static SubscriptionStatus ParseSubscriptionStatus(string value)
        {
            return value switch
            {
                "active" => SubscriptionStatus.Active,
                "canceled" => SubscriptionStatus.Canceled,
                "trialing" => SubscriptionStatus.Trialing,
                "pastDue" => SubscriptionStatus.PastDue,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported subscription status.")
            };
        }

        private static string SubscriptionStatusToString(SubscriptionStatus status)
        {
            return status switch
            {
                SubscriptionStatus.Active => "active",
                SubscriptionStatus.Canceled => "canceled",
                SubscriptionStatus.Trialing => "trialing",
                SubscriptionStatus.PastDue => "pastDue",
                _ => "active"
            };
        }

        private static ApplicationFeatureType ParseFeatureType(string value)
        {
            return value switch
            {
                "integer" => ApplicationFeatureType.Integer,
                "boolean" => ApplicationFeatureType.Boolean,
                "string" => ApplicationFeatureType.String,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported feature type.")
            };
        }

        private static string FeatureTypeToString(ApplicationFeatureType type)
        {
            return type switch
            {
                ApplicationFeatureType.Integer => "integer",
                ApplicationFeatureType.Boolean => "boolean",
                ApplicationFeatureType.String => "string",
                _ => "string"
            };
        }
    }
}

