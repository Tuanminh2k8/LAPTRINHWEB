using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Controllers.Api
{
    [Route("api/modifiers")]
    [ApiController]
    public class ModifiersApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ModifiersApiController> _logger;

        public ModifiersApiController(AppDbContext context, ILogger<ModifiersApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/modifiers/food/5 — danh sách nhóm + tùy chọn của một món
        [HttpGet("food/{foodId:int}")]
        public async Task<ActionResult> GetByFood(int foodId)
        {
            var foodExists = await _context.FastFoods.AnyAsync(f => f.Id == foodId);
            if (!foodExists) return NotFound(new { message = "Không tìm thấy món ăn." });

            var groups = await _context.ModifierGroups
                .AsNoTracking()
                .Where(g => g.FastFoodId == foodId)
                .OrderBy(g => g.SortOrder)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    g.IsMultiple,
                    g.MaxOptions,
                    g.SortOrder,
                    options = g.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new
                        {
                            o.Id,
                            o.Name,
                            o.Price,
                            o.IsDefault,
                            o.IsAvailable,
                            o.SortOrder
                        })
                })
                .ToListAsync();

            return Ok(groups);
        }

        // POST: api/modifiers/groups — tạo nhóm tùy chọn cho món
        [HttpPost("groups")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateGroup(ModifierGroupInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name) || input.FastFoodId <= 0)
            {
                return BadRequest(new { message = "Dữ liệu nhóm tùy chọn không hợp lệ." });
            }

            var foodExists = await _context.FastFoods.AnyAsync(f => f.Id == input.FastFoodId);
            if (!foodExists) return BadRequest(new { message = "Món ăn không tồn tại." });

            var maxSort = await _context.ModifierGroups
                .Where(g => g.FastFoodId == input.FastFoodId)
                .Select(g => (int?)g.SortOrder)
                .MaxAsync();

            var group = new ModifierGroup
            {
                Name = input.Name.Trim(),
                Description = input.Description?.Trim(),
                IsMultiple = input.IsMultiple,
                MaxOptions = Math.Max(1, input.MaxOptions),
                SortOrder = (maxSort ?? -1) + 1,
                FastFoodId = input.FastFoodId
            };

            _context.ModifierGroups.Add(group);
            await _context.SaveChangesAsync();

            if (input.Options != null)
            {
                var sort = 0;
                foreach (var opt in input.Options)
                {
                    if (string.IsNullOrWhiteSpace(opt.Name)) continue;
                    _context.ModifierOptions.Add(new ModifierOption
                    {
                        ModifierGroupId = group.Id,
                        Name = opt.Name.Trim(),
                        Price = opt.Price,
                        IsDefault = opt.IsDefault,
                        IsAvailable = true,
                        SortOrder = sort++
                    });
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("API created modifier group: {Group} for food {FoodId}", group.Name, group.FastFoodId);
            return Ok(new { success = true, message = "Đã thêm nhóm tùy chọn.", id = group.Id });
        }

        // PUT: api/modifiers/groups/5 — cập nhật nhóm tùy chọn
        [HttpPut("groups/{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroup(int id, ModifierGroupInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                return BadRequest(new { message = "Dữ liệu nhóm tùy chọn không hợp lệ." });
            }

            var group = await _context.ModifierGroups.FindAsync(id);
            if (group == null) return NotFound(new { message = "Không tìm thấy nhóm tùy chọn." });

            group.Name = input.Name.Trim();
            group.Description = input.Description?.Trim();
            group.IsMultiple = input.IsMultiple;
            group.MaxOptions = Math.Max(1, input.MaxOptions);

            _context.Update(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API updated modifier group: {Group} (ID={Id})", group.Name, group.Id);
            return NoContent();
        }

        // DELETE: api/modifiers/groups/5 — xóa nhóm và toàn bộ tùy chọn của nó
        [HttpDelete("groups/{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var group = await _context.ModifierGroups
                .Include(g => g.Options)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound(new { message = "Không tìm thấy nhóm tùy chọn." });

            _context.ModifierOptions.RemoveRange(group.Options);
            _context.ModifierGroups.Remove(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API deleted modifier group: ID={Id}", id);
            return NoContent();
        }

        // POST: api/modifiers/options — thêm tùy chọn vào nhóm
        [HttpPost("options")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateOption(ModifierOptionInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name) || input.ModifierGroupId <= 0)
            {
                return BadRequest(new { message = "Dữ liệu tùy chọn không hợp lệ." });
            }

            var groupExists = await _context.ModifierGroups.AnyAsync(g => g.Id == input.ModifierGroupId);
            if (!groupExists) return BadRequest(new { message = "Nhóm tùy chọn không tồn tại." });

            var maxSort = await _context.ModifierOptions
                .Where(o => o.ModifierGroupId == input.ModifierGroupId)
                .Select(o => (int?)o.SortOrder)
                .MaxAsync();

            var option = new ModifierOption
            {
                ModifierGroupId = input.ModifierGroupId,
                Name = input.Name.Trim(),
                Price = input.Price,
                IsDefault = input.IsDefault,
                IsAvailable = true,
                SortOrder = (maxSort ?? -1) + 1
            };

            _context.ModifierOptions.Add(option);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã thêm tùy chọn.", id = option.Id });
        }

        // PUT: api/modifiers/options/5 — cập nhật tùy chọn
        [HttpPut("options/{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOption(int id, ModifierOptionInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                return BadRequest(new { message = "Dữ liệu tùy chọn không hợp lệ." });
            }

            var option = await _context.ModifierOptions.FindAsync(id);
            if (option == null) return NotFound(new { message = "Không tìm thấy tùy chọn." });

            option.Name = input.Name.Trim();
            option.Price = input.Price;
            option.IsDefault = input.IsDefault;

            _context.Update(option);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/modifiers/options/5 — xóa tùy chọn
        [HttpDelete("options/{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOption(int id)
        {
            var option = await _context.ModifierOptions.FindAsync(id);
            if (option == null) return NotFound(new { message = "Không tìm thấy tùy chọn." });

            _context.ModifierOptions.Remove(option);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API deleted modifier option: ID={Id}", id);
            return NoContent();
        }

        public class ModifierGroupInput
        {
            [Required]
            [StringLength(100)]
            public string? Name { get; set; }

            [StringLength(200)]
            public string? Description { get; set; }

            public bool IsMultiple { get; set; }

            [Range(1, 100)]
            public int MaxOptions { get; set; } = 1;

            public int FastFoodId { get; set; }

            public List<ModifierOptionInput>? Options { get; set; }
        }

        public class ModifierOptionInput
        {
            [Required]
            [StringLength(100)]
            public string? Name { get; set; }

            [Range(0, 10000000)]
            public decimal Price { get; set; }

            public bool IsDefault { get; set; }

            public int ModifierGroupId { get; set; }
        }
    }
}
