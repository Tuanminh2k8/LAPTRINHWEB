using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers.Api
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsApiController(AppDbContext context)
        {
            _context = context;
        }

        public class CreateReviewRequest
        {
            [Required]
            public int OrderId { get; set; }

            [Required]
            public int FoodId { get; set; }

            [Required]
            [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
            public int Rating { get; set; } = 5;

            [StringLength(1000)]
            public string? Comment { get; set; }
        }

        // GET: api/reviews/food/5 — danh sách đánh giá của một món ăn (public)
        [HttpGet("food/{foodId:int}")]
        public async Task<ActionResult> GetFoodReviews(int foodId)
        {
            var food = await _context.FastFoods.AnyAsync(f => f.Id == foodId);
            if (!food) return NotFound(new { message = "Không tìm thấy món ăn." });

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.FastFoodId == foodId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    reviewerName = r.User != null ? r.User.FullName : "Khách hàng"
                })
                .ToListAsync();

            var ratingGroups = await _context.Reviews
                .Where(r => r.FastFoodId == foodId && r.IsApproved)
                .GroupBy(r => r.Rating)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            var totalReviews = ratingGroups.Sum(g => g.Count);
            var avgRating = totalReviews > 0
                ? ratingGroups.Sum(g => g.Key * g.Count) / (double)totalReviews
                : 0.0;

            var summary = new
            {
                avgRating = Math.Round(avgRating, 1),
                totalReviews,
                breakdown = ratingGroups.ToDictionary(g => g.Key, g => g.Count)
            };

            return Ok(new
            {
                foodId,
                summary,
                reviews
            });
        }

        // POST: api/reviews — khách đánh giá món đã mua (đơn phải Delivered và có món này)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu đánh giá không hợp lệ." });
            }

            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var hasDelivered = await _context.Orders
                .AnyAsync(o => o.Id == request.OrderId &&
                               o.UserId == userId.Value &&
                               o.Status == OrderStatus.Delivered &&
                               o.OrderDetails.Any(d => d.FastFoodId == request.FoodId));

            if (!hasDelivered) return BadRequest(new { message = "Chỉ được đánh giá món ăn thuộc đơn đã giao thành công." });

            var existing = await _context.Reviews.AnyAsync(r => r.OrderId == request.OrderId && r.FastFoodId == request.FoodId && r.UserId == userId.Value);
            if (existing) return BadRequest(new { message = "Bạn đã đánh giá món này trong đơn này rồi." });

            var review = new Review
            {
                OrderId = request.OrderId,
                FastFoodId = request.FoodId,
                UserId = userId.Value,
                Rating = request.Rating,
                Comment = request.Comment?.Trim(),
                CreatedAt = DateTime.Now,
                IsApproved = true
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cảm ơn bạn đã đánh giá!",
                reviewId = review.Id
            });
        }
    }
}