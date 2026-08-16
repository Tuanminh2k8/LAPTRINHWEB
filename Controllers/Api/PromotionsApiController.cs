using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers.Api
{
    [Route("api/promotions")]
    [ApiController]
    public class PromotionsApiController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionsApiController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        private int? CurrentUserId => UserClaimsHelper.GetUserId(User);
        private string? CurrentRole => UserClaimsHelper.GetRole(User);

        #region Public (User)

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PublicList() =>
            Ok(await _promotionService.GetPublicPromotionsAsync());

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Detail(int id)
        {
            var promo = await _promotionService.GetByIdAsync(id);
            return promo == null ? NotFound() : Ok(promo);
        }

        public record ValidateRequest(string? Code, decimal Subtotal, int? UserId);
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> Validate([FromBody] ValidateRequest req)
        {
            var userId = req.UserId ?? CurrentUserId;
            var result = await _promotionService.ValidateAsync(req.Code, req.Subtotal, userId);
            return Ok(new
            {
                result.Success,
                result.Message,
                result.DiscountAmount,
                Code = result.Promo?.Code,
                Status = result.EvaluatedStatus?.ToString()
            });
        }

        #endregion

        #region Admin

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminList() =>
            Ok(await _promotionService.GetAllAsync());

        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCreate([FromBody] PromoCode model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _promotionService.CreateAsync(model, nameof(PromotionOwnerRole.Admin), null, User.Identity?.Name);
            return CreatedAtAction(nameof(Detail), new { id = created.Id }, created);
        }

        [HttpPut("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdate(int id, [FromBody] PromoCode model)
        {
            try
            {
                var updated = await _promotionService.UpdateAsync(id, model, CurrentUserId, "Admin", User.Identity?.Name);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("admin/{id:int}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminPublish(int id)
        {
            await _promotionService.PublishAsync(id, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        public record ScheduleRequest(DateTime Start, DateTime? End);
        [HttpPost("admin/{id:int}/schedule")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminSchedule(int id, [FromBody] ScheduleRequest req)
        {
            await _promotionService.ScheduleAsync(id, req.Start, req.End, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        [HttpPost("admin/{id:int}/pause")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminPause(int id)
        {
            await _promotionService.PauseAsync(id, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        [HttpPost("admin/{id:int}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminActivate(int id)
        {
            await _promotionService.ActivateAsync(id, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        [HttpPost("admin/{id:int}/expire")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminExpire(int id)
        {
            await _promotionService.ExpireAsync(id, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        public record EarlyPublishRequest(bool UsableEarly);
        [HttpPost("admin/{id:int}/early-publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEarlyPublish(int id, [FromBody] EarlyPublishRequest req)
        {
            await _promotionService.EarlyPublishAsync(id, req.UsableEarly, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        [HttpDelete("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDelete(int id)
        {
            await _promotionService.SoftDeleteAsync(id, CurrentUserId, "Admin");
            return Ok(new { success = true });
        }

        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminStatistics([FromQuery] int? promotionId) =>
            Ok(await _promotionService.GetStatisticsAsync(promotionId));

        [HttpGet("admin/{id:int}/usages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUsages(int id, [FromQuery] int page = 1) =>
            Ok(await _promotionService.GetUsageHistoryAsync(id, page));

        #endregion

        #region Seller

        [HttpGet("seller")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> SellerList() =>
            Ok(await _promotionService.GetAllAsync(nameof(PromotionOwnerRole.Seller), CurrentUserId));

        [HttpPost("seller")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> SellerCreate([FromBody] PromoCode model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _promotionService.CreateAsync(model, nameof(PromotionOwnerRole.Seller), CurrentUserId, User.Identity?.Name);
            return CreatedAtAction(nameof(Detail), new { id = created.Id }, created);
        }

        [HttpPut("seller/{id:int}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> SellerUpdate(int id, [FromBody] PromoCode model)
        {
            try
            {
                var updated = await _promotionService.UpdateAsync(id, model, CurrentUserId, nameof(PromotionOwnerRole.Seller), User.Identity?.Name);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("seller/{id:int}/publish")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> SellerPublish(int id)
        {
            try { await _promotionService.PublishAsync(id, CurrentUserId, nameof(PromotionOwnerRole.Seller)); return Ok(new { success = true }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("seller/{id:int}/pause")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> SellerPause(int id)
        {
            try { await _promotionService.PauseAsync(id, CurrentUserId, nameof(PromotionOwnerRole.Seller)); return Ok(new { success = true }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        #endregion
    }
}
