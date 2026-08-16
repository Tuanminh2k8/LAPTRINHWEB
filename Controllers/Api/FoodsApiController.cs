using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers.Api
{
    [Route("api/foods")]
    [ApiController]
    public class FoodsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FoodsApiController> _logger;

        public FoodsApiController(AppDbContext context, ILogger<FoodsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/foods?name=&categoryId=&minPrice=&maxPrice=&categoryIds=1,2,3
        [HttpGet]
        public async Task<ActionResult> GetFoods(
            [FromQuery] string? name,
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery]
            [ModelBinder(BinderType = typeof(CommaSeparatedIntModelBinder))]
            List<int>? categoryIds)
        {
            var query = _context.FastFoods.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.Length > 100) name = name[..100];
                query = query.Where(f => f.Name.Contains(name));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
            }

            if (categoryIds is { Count: > 0 })
            {
                query = query.Where(f => categoryIds.Contains(f.CategoryId));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(f => f.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(f => f.Price <= maxPrice.Value);
            }

            var foods = await query
                .OrderBy(f => f.Name)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Price,
                    f.Description,
                    f.ImageUrl,
                    f.CategoryId,
                    f.SoldCount,
                    f.IsAvailable,
                    f.IsBestSeller,
                    avgRating = f.Reviews.Any() ? f.Reviews.Average(r => (double)r.Rating) : 0,
                    reviewCount = f.Reviews.Count,
                    CategoryName = f.Category != null ? f.Category.Name : ""
                })
                .ToListAsync();

            return Ok(foods);
        }

        // GET: api/foods/bestsellers — món bán chạy cho trang chủ / widget
        [HttpGet("bestsellers")]
        public async Task<ActionResult> GetBestsellers()
        {
            var foods = await _context.FastFoods
                .AsNoTracking()
                .Where(f => f.IsBestSeller && f.IsAvailable)
                .OrderByDescending(f => f.SoldCount)
                .Take(8)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Price,
                    f.Description,
                    f.ImageUrl,
                    f.CategoryId,
                    f.SoldCount,
                    f.Theme,
                    avgRating = f.Reviews.Any() ? f.Reviews.Average(r => (double)r.Rating) : 0,
                    reviewCount = f.Reviews.Count,
                    CategoryName = f.Category != null ? f.Category.Name : ""
                })
                .ToListAsync();
            return Ok(foods);
        }

        // GET: api/foods/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetFood(int id)
        {
            var food = await _context.FastFoods
                .AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Price,
                    f.Description,
                    f.ImageUrl,
                    f.CategoryId,
                    f.Theme,
                    f.SoldCount,
                    f.IsAvailable,
                    f.IsBestSeller,
                    f.Sku,
                    avgRating = f.Reviews.Any() ? f.Reviews.Average(r => (double)r.Rating) : 0,
                    reviewCount = f.Reviews.Count,
                    CategoryName = f.Category != null ? f.Category.Name : "",
                    // Phân loại (FoodVariant) — size/SKU kèm tồn kho thật
                    variants = f.Variants
                        .Where(v => v.IsAvailable && v.StockQuantity > 0)
                        .OrderBy(v => v.SortOrder)
                        .Select(v => new
                        {
                            v.Id,
                            v.Name,
                            v.Size,
                            v.Color,
                            v.Price,
                            v.OriginalPrice,
                            v.StockQuantity,
                            v.IsAvailable,
                            v.IsDefault,
                            v.ImageUrl,
                            displayName = v.DisplayName
                        })
                        .ToList(),
                    // Shop / người bán
                    seller = f.Seller != null ? new
                    {
                        f.Seller.Id,
                        f.Seller.FullName,
                        f.Seller.Email,
                        productCount = f.SellerId.HasValue
                            ? _context.FastFoods.Count(sf => sf.SellerId == f.Seller!.Id)
                            : 0
                    } : null,
                    modifierGroups = f.ModifierGroups
                        .OrderBy(g => g.SortOrder)
                        .Select(g => new
                        {
                            g.Id,
                            g.Name,
                            g.Description,
                            g.IsMultiple,
                            g.MaxOptions,
                            g.MinOptions,
                            options = g.Options
                                .OrderBy(o => o.SortOrder)
                                .Select(o => new
                                {
                                    o.Id,
                                    o.Name,
                                    o.Price,
                                    o.IsDefault,
                                    o.IsAvailable
                                })
                        })
                })
                .FirstOrDefaultAsync();

            if (food == null)
            {
                return NotFound(new { message = "Không tìm thấy món ăn." });
            }

            // Thư viện ảnh (gallery) — ưu tiên FoodImage, fallback ảnh chính
            var images = await _context.FoodImages
                .AsNoTracking()
                .Where(fi => fi.FastFoodId == id)
                .OrderBy(fi => fi.SortOrder)
                .Select(fi => new { fi.ImageUrl, fi.IsPrimary, fi.AltText })
                .ToListAsync();

            if (images.Count == 0)
            {
                images.Add(new { ImageUrl = food.ImageUrl, IsPrimary = true, AltText = food.Name });
            }

            return Ok(new
            {
                food.Id,
                food.Name,
                food.Price,
                food.Description,
                food.ImageUrl,
                food.CategoryId,
                food.Theme,
                food.SoldCount,
                food.IsAvailable,
                food.IsBestSeller,
                food.Sku,
                food.avgRating,
                food.reviewCount,
                food.CategoryName,
                food.variants,
                food.seller,
                food.modifierGroups,
                images
            });
        }

        // GET: api/foods/5/variants — danh sách phân loại kèm tồn kho thật
        [HttpGet("{id:int}/variants")]
        public async Task<ActionResult> GetFoodVariants(int id)
        {
            var exists = await _context.FastFoods.AnyAsync(f => f.Id == id);
            if (!exists) return NotFound(new { message = "Không tìm thấy món ăn." });

            var variants = await _context.FoodVariants
                .AsNoTracking()
                .Where(v => v.FastFoodId == id)
                .OrderBy(v => v.SortOrder)
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Size,
                    v.Color,
                    v.Price,
                    v.OriginalPrice,
                    v.StockQuantity,
                    v.IsAvailable,
                    v.IsDefault,
                    v.ImageUrl,
                    displayName = v.DisplayName
                })
                .ToListAsync();

            return Ok(new { foodId = id, variants });
        }

        // POST: api/foods
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<FastFood>> CreateFood(FoodInputDto input)
        {
            if (input == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu món ăn không hợp lệ." });
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == input.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Chủng loại không tồn tại." });
            }

            var food = new FastFood
            {
                Name = input.Name!.Trim(),
                Price = input.Price,
                Description = input.Description?.Trim() ?? string.Empty,
                ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? "/images/default_food.jpg" : input.ImageUrl.Trim(),
                CategoryId = input.CategoryId,
                Theme = input.Theme?.Trim() ?? string.Empty
            };

            _context.FastFoods.Add(food);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API created food: {FoodName} (Price={Price})", food.Name, food.Price);

            var created = await _context.FastFoods
                .AsNoTracking()
                .Where(f => f.Id == food.Id)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Price,
                    f.Description,
                    f.ImageUrl,
                    f.CategoryId,
                    f.Theme,
                    CategoryName = f.Category != null ? f.Category.Name : ""
                })
                .FirstOrDefaultAsync();
            return CreatedAtAction(nameof(GetFood), new { id = food.Id }, created);
        }

        // PUT: api/foods/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFood(int id, FoodInputDto input)
        {
            if (input == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu món ăn không hợp lệ." });
            }

            var food = await _context.FastFoods.FindAsync(id);
            if (food == null)
            {
                return NotFound(new { message = "Không tìm thấy món ăn." });
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == input.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Chủng loại không tồn tại." });
            }

            food.Name = input.Name!.Trim();
            food.Price = input.Price;
            food.Description = input.Description?.Trim() ?? string.Empty;
            food.CategoryId = input.CategoryId;
            food.Theme = input.Theme?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(input.ImageUrl))
            {
                food.ImageUrl = input.ImageUrl.Trim();
            }

            _context.Update(food);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API updated food: {FoodName} (ID={Id})", food.Name, food.Id);
            return NoContent();
        }

        // DELETE: api/foods/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFood(int id)
        {
            var food = await _context.FastFoods.FindAsync(id);
            if (food == null)
            {
                return NotFound(new { message = "Không tìm thấy món ăn." });
            }

            var inOrders = await _context.OrderDetails.AnyAsync(od => od.FastFoodId == id);
            var inCombos = await _context.ComboDetails.AnyAsync(cd => cd.FastFoodId == id);
            if (inOrders || inCombos)
            {
                return BadRequest(new { message = "Không thể xóa món ăn đang được sử dụng trong combo hoặc đơn hàng." });
            }

            _context.FastFoods.Remove(food);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API deleted food: ID={Id}", id);
            return NoContent();
        }
    }

    public class FoodInputDto
    {
        [Required(ErrorMessage = "Tên món ăn không được để trống")]
        [StringLength(100, ErrorMessage = "Tên món ăn không được quá 100 ký tự")]
        public string? Name { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string? Theme { get; set; }
    }
}
