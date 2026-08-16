using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers
{
    [Authorize(Roles = "Kol")]
    public class KolController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<KolController> _logger;

        public KolController(AppDbContext context, ILogger<KolController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var kolId = UserClaimsHelper.GetUserId(User);
            // TODO: Implement KOL dashboard
            return View();
        }

        [HttpGet]
        public IActionResult Links()
        {
            // TODO: Implement affiliate link generation
            return View();
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            // TODO: Implement KOL dashboard with stats
            return View();
        }
    }
}