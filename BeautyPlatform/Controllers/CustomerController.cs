using System.Security.Claims;
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

        [Authorize]
        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = await _context.Appointments
                .Include(a => a.Service)
                    .ThenInclude(s => s.BusinessProfile)
                .Include(a => a.User)
                .Where(a => a.UserId == user.Id )
                .ToListAsync();

            return View(appointments);

        }
        public async Task<IActionResult> AppointmentHistory()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = await _context.Appointments
                .Include(a => a.Service)
                    .ThenInclude(s => s.BusinessProfile)
                .Include(a => a.User)
                .Where(a => a.UserId == user.Id && a.Status == "Completed")
                .ToListAsync();

            return View(appointments);
        }


        public async Task<IActionResult> Cart()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.BusinessProfile) // ✅ This is required
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var grouped = cartItems
                .GroupBy(c => c.Product.BusinessProfile)
                .Select(g => new CartGroupByBusiness
                {
                    Business = g.Key,
                    Items = g.ToList()
                })
                .ToList();

            return View(grouped);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity, string scrollPosition)
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

            TempData["ScrollPosition"] = scrollPosition;
            return Redirect(Request.Headers["Referer"].ToString());
        }
        [HttpPost]
        public async Task<IActionResult> AddToCartAjax([FromBody] AddToCartRequest request)
        {
            var userId = _userManager.GetUserId(User);

            if (request.Quantity <= 0)
            {
                request.Quantity = 1;
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    ProductId = request.ProductId,
                    UserId = userId,
                    Quantity = request.Quantity
                });
            }

            await _context.SaveChangesAsync();

            var newCount = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, count = newCount });
        }

        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
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
        public async Task<IActionResult> MarkBusinessAsCash(int businessId)
        {
            var userId = _userManager.GetUserId(User);

            var appointments = await _context.Appointments
                .Include(a => a.Service)
            .ThenInclude(s => s.BusinessProfile)
                .Where(a => a.UserId == userId
                         && a.Status == "Confirmed"
                         && a.PaymentMethod == "Unpaid"
                         && a.Service.BusinessProfileId == businessId)
                .ToListAsync();

            foreach (var appt in appointments)
            {
                appt.PaymentMethod = "Cash";
            }

            await _context.SaveChangesAsync();
            var businessName = appointments.FirstOrDefault()?.Service?.BusinessProfile?.BusinessName ?? "the selected business";
            TempData["Success"] = $"You selected to pay in cash for services booked at {businessName}.";

            return RedirectToAction("MyAppointments");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCartAsCash(int businessId)
        {
            var userId = _userManager.GetUserId(User);

            var items = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.BusinessProfile)
                .Where(c => c.UserId == userId && c.Product.BusinessProfileId == businessId)
                .ToListAsync();

            if (!items.Any())
            {
                TempData["SuccessMessage"] = "No items found for this vendor.";
                return RedirectToAction("Cart");
            }

            // Optional: You could later convert these into an Order with PaymentMethod = Cash

            TempData["SuccessMessage"] = $"You selected to pay in cash for products from {items.First().Product.BusinessProfile.BusinessName}.";
            return RedirectToAction("Cart");
        }



        [HttpPost("Customer/RemoveFromCart/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = _userManager.GetUserId(User);

            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (item != null)
            {
                var businessId = item.Product.BusinessProfileId;

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                var remainingCount = await _context.CartItems
                    .Where(c => c.UserId == userId && c.Product.BusinessProfileId == businessId)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    businessId = businessId,
                    remainingCount = remainingCount
                });
            }

            // Return dummy response so frontend still works
            return Json(new
            {
                success = false,
                businessId = 0,
                remainingCount = 0
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(int ServiceId, DateTime AppointmentDate, TimeSpan AppointmentTime, string ScrollPosition)
        {
            var userId = _userManager.GetUserId(User);
            var appointmentDateTime = AppointmentDate.Date + AppointmentTime;

            // Time restriction: only 8 AM to 5 PM
            if (AppointmentTime < TimeSpan.FromHours(8) || AppointmentTime > TimeSpan.FromHours(17))
            {
                TempData["ScrollPosition"] = ScrollPosition;
                TempData["AppointmentError"] = "Appointments can only be booked between 8:00 AM and 5:00 PM.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // Check for existing appointment at same time for same service
            bool isSlotTaken = await _context.Appointments
                .AnyAsync(a => a.ServiceId == ServiceId && a.AppointmentDateTime == appointmentDateTime);

            if (isSlotTaken)
            {
                TempData["ScrollPosition"] = ScrollPosition;
                TempData["AppointmentError"] = "This time slot is already booked. Please choose another.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var appointment = new Appointment
            {
                UserId = userId,
                ServiceId = ServiceId,
                AppointmentDateTime = appointmentDateTime,
                PaymentMethod = "Unpaid" // add this to be safe
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["AppointmentBadge"] = true;
            TempData["ScrollPosition"] = ScrollPosition;
            return Redirect(Request.Headers["Referer"].ToString());
        }
        [HttpGet]
        public async Task<IActionResult> GetCartTotal()
        {
            var userId = _userManager.GetUserId(User);
            var total = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Product.Price * c.Quantity);

            return Json(new { total });
        }

        [HttpGet]
        public async Task<IActionResult> GetBusinessCartTotal(int businessId)
        {
            var userId = _userManager.GetUserId(User);
            var total = await _context.CartItems
                .Where(c => c.UserId == userId && c.Product.BusinessProfileId == businessId)
                .SumAsync(c => c.Product.Price * c.Quantity);

            return Json(new { total });
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointmentCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = await _context.Appointments
                .Where(a => a.UserId == userId)
                .CountAsync();

            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.UserId == userId);

            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment cancelled successfully.";
            }

            return RedirectToAction("MyAppointments");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItemAjax([FromBody] CartUpdateRequest request)
        {
            var userId = _userManager.GetUserId(User);

            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == request.ItemId && c.UserId == userId);

            if (item != null)
            {
                item.Quantity = request.Quantity > 0 ? request.Quantity : 1;
                await _context.SaveChangesAsync();

                // Grand total for the entire cart
                var grandTotal = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Product.Price * c.Quantity);

                // Subtotal for this item's business
                var businessId = item.Product.BusinessProfileId;
                var businessTotal = await _context.CartItems
                    .Where(c => c.UserId == userId && c.Product.BusinessProfileId == businessId)
                    .SumAsync(c => c.Product.Price * c.Quantity);

                return Json(new
                {
                    success = true,
                    itemTotal = item.Product.Price * item.Quantity,
                    grandTotal = grandTotal,
                    businessId = businessId,
                    businessTotal = businessTotal
                });
            }

            return Json(new { success = false });
        }

        public class CartUpdateRequest
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
        }

        public class CartGroupByBusiness
        {
            public BusinessProfile Business { get; set; }
            public List<CartItem> Items { get; set; }

            public decimal Total => Items.Sum(i => i.Product.Price * i.Quantity);
        }


    }
}
