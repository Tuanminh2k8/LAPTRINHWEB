using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Controllers.Api
{
    [Route("api/combos")]
    [ApiController]
    public class CombosApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CombosApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/combos
        [HttpGet]
        public async Task<ActionResult> GetCombos()
        {
            var combos = await _context.Combos
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Price,
                    c.Description,
                    c.ImageUrl,
                    c.IsOnSale,
                    c.OriginalPrice,
                    Items = c.ComboDetails.Select(cd => new
                    {
                        cd.Quantity,
                        FoodId = cd.FastFood != null ? cd.FastFood.Id : 0,
                        FoodName = cd.FastFood != null ? cd.FastFood.Name : "",
                        FoodPrice = cd.FastFood != null ? cd.FastFood.Price : 0,
                        CategoryName = cd.FastFood != null && cd.FastFood.Category != null ? cd.FastFood.Category.Name : ""
                    })
                })
                .ToListAsync();

            return Ok(combos);
        }

        // GET: api/combos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetCombo(int id)
        {
            var combo = await _context.Combos
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Price,
                    c.Description,
                    c.ImageUrl,
                    c.IsOnSale,
                    c.OriginalPrice,
                    Items = c.ComboDetails.Select(cd => new
                    {
                        cd.Quantity,
                        FoodId = cd.FastFood != null ? cd.FastFood.Id : 0,
                        FoodName = cd.FastFood != null ? cd.FastFood.Name : "",
                        FoodPrice = cd.FastFood != null ? cd.FastFood.Price : 0,
                        CategoryName = cd.FastFood != null && cd.FastFood.Category != null ? cd.FastFood.Category.Name : ""
                    })
                })
                .FirstOrDefaultAsync();

            if (combo == null)
            {
                return NotFound(new { message = "Không tìm thấy combo." });
            }

            return Ok(combo);
        }
    }
}
