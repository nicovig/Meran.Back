using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Meran.Back.DTO;
using Meran.Back.Services;

namespace Meran.Back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationsController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ApplicationDto>>> GetAll(CancellationToken cancellationToken)
        {
            var result = await _applicationService.GetAllAsync(cancellationToken);
            Response.Headers.Append("X-Meran-Hit", "ApplicationsController.GetAll");
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ApplicationDto>> Create(CreateApplicationRequestDto request, CancellationToken cancellationToken)
        {
            var created = await _applicationService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAll), new { }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApplicationDto>> Update(Guid id, UpdateApplicationRequestDto request, CancellationToken cancellationToken)
        {
            var updated = await _applicationService.UpdateAsync(id, request, cancellationToken);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _applicationService.DeleteAsync(id, cancellationToken);

            if (!success)
            {
                return Conflict(new { message = "Application cannot be deleted because it does not exist or has associated users." });
            }

            return NoContent();
        }

        [HttpPost("{applicationId:guid}/users")]
        public async Task<ActionResult<ApplicationUserDto>> AddUser(Guid applicationId, AddApplicationUserRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _applicationService.AddUserAsync(applicationId, request, cancellationToken);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{applicationId:guid}/features")]
        public async Task<ActionResult<List<ApplicationFeatureDto>>> GetFeatures(Guid applicationId, CancellationToken cancellationToken)
        {
            var features = await _applicationService.GetFeaturesAsync(applicationId, cancellationToken);
            if (features == null)
            {
                return NotFound();
            }

            return Ok(features);
        }

        [HttpPut("{applicationId:guid}/features")]
        public async Task<ActionResult<List<ApplicationFeatureDto>>> UpsertFeatures(Guid applicationId, UpsertApplicationFeaturesRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var features = await _applicationService.UpsertFeaturesAsync(applicationId, request, cancellationToken);
                if (features == null)
                {
                    return NotFound();
                }

                return Ok(features);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{applicationId:guid}/users/{applicationUserId:guid}/subscriptions")]
        public async Task<ActionResult<List<SubscriptionDto>>> GetSubscriptions(Guid applicationId, Guid applicationUserId, CancellationToken cancellationToken)
        {
            var subscriptions = await _applicationService.GetSubscriptionsAsync(applicationId, applicationUserId, cancellationToken);
            if (subscriptions == null)
            {
                return NotFound();
            }

            return Ok(subscriptions);
        }

        [HttpPost("{applicationId:guid}/users/{applicationUserId:guid}/subscriptions")]
        public async Task<ActionResult<SubscriptionDto>> CreateSubscription(Guid applicationId, Guid applicationUserId, CreateSubscriptionRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var subscription = await _applicationService.CreateSubscriptionAsync(applicationId, applicationUserId, request, cancellationToken);
                if (subscription == null)
                {
                    return NotFound();
                }

                return Ok(subscription);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}

