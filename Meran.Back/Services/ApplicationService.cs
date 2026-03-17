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
                CreatedAt = DateTime.UtcNow,
                Plan = request.Plan
            };

            _dbContext.ApplicationUsers.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(user);
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
                Plan = user.Plan
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
    }
}

