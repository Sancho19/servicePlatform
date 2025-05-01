using BeautyPlatform.Data;
using BeautyPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyPlatform.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Cart()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var userId = _userManager.GetUserId(User);

            if (quantity <= 0)
            {
                quantity = 1;
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    ProductId = productId,
                    UserId = userId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();

            // Redirect back to the referring URL
            return Redirect(Request.Headers["Referer"].ToString());
        }


        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { count });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookService(int serviceId, DateTime date, string time)
        {
            var userId = _userManager.GetUserId(User);
            var parsedTime = TimeSpan.Parse(time);
            var appointmentDateTime = date.Date + parsedTime;

            var appointment = new Appointment
            {
                ServiceId = serviceId,
                UserId = userId,
                AppointmentDateTime = appointmentDateTime
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart([FromBody] List<CartItem> updatedItems)
        {
            var userId = _userManager.GetUserId(User);

            foreach (var updatedItem in updatedItems)
            {
                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.Id == updatedItem.Id && c.UserId == userId);

                if (existing != null)
                {
                    existing.Quantity = updatedItem.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("Customer/RemoveFromCart/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = _userManager.GetUserId(User);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }
    }
}
