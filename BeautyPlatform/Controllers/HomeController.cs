using System.Diagnostics;
using BeautyPlatform.Data;
using BeautyPlatform.Models;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Mvc;

namespace BeautyPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var businesses = await _context.BusinessProfiles.ToListAsync();
            return View(businesses);
        }
        public async Task<IActionResult> BusinessDetails(int id, string searchQuery = "", string sortOrder = "")
        {
            var business = await _context.BusinessProfiles
                .Include(b => b.Products)
                .Include(b => b.Services)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (business == null)
            {
                return NotFound();
            }

            // Filter Products based on searchQuery
            var filteredProducts = business.Products.Where(p =>
                string.IsNullOrEmpty(searchQuery) ||
                p.Name.ToLower().Contains(searchQuery.ToLower()) ||
                p.Description.ToLower().Contains(searchQuery.ToLower())
            ).ToList();

            // Filter Services based on searchQuery
            var filteredServices = business.Services.Where(s =>
                string.IsNullOrEmpty(searchQuery) ||
                s.Name.ToLower().Contains(searchQuery.ToLower()) ||
                s.Description.ToLower().Contains(searchQuery.ToLower())
            ).ToList();

            // Apply Sorting for Products
            if (!string.IsNullOrEmpty(sortOrder))
            {
                filteredProducts = sortOrder == "asc" ?
                    filteredProducts.OrderBy(p => p.Price).ToList() :
                    filteredProducts.OrderByDescending(p => p.Price).ToList();
            }

            // Apply Sorting for Services
            if (!string.IsNullOrEmpty(sortOrder))
            {
                filteredServices = sortOrder == "asc" ?
                    filteredServices.OrderBy(s => s.Price).ToList() :
                    filteredServices.OrderByDescending(s => s.Price).ToList();
            }

            // Set the filtered and sorted products and services back to the business
            business.Products = filteredProducts;
            business.Services = filteredServices;

            return View(business);
        }



    }
}
