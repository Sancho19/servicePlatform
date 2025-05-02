using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BeautyPlatform.Data;
using Microsoft.EntityFrameworkCore;
using BeautyPlatform.Models;
using System.Threading.Tasks;
using System.Data;
using Microsoft.AspNetCore.Hosting;

namespace BeautyPlatform.Controllers
{
   [ Authorize(Roles = "Vendor")]
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VendorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Vendor/CreateBusiness
        public IActionResult CreateBusiness()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard(string searchTerm = null, string filterType = null)
        {
            var user = await _userManager.GetUserAsync(User);

            var businesses = await _context.BusinessProfiles
                .Include(b => b.Products)
                .Include(b => b.Services)
                .Where(b => b.UserId == user.Id)
                .ToListAsync();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                foreach (var business in businesses)
                {
                    business.Products = business.Products
                        .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    business.Services = business.Services
                        .Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            if (!string.IsNullOrEmpty(filterType))
            {
                foreach (var business in businesses)
                {
                    if (filterType == "products")
                        business.Services = new List<Service>(); // hide services
                    else if (filterType == "services")
                        business.Products = new List<Product>(); // hide products
                }
            }

            return View(businesses);
        }
        [HttpGet]
        public async Task<IActionResult> ViewAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Where(a => a.Service.BusinessProfile.UserId == user.Id)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, string newStatus)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return NotFound();

            appointment.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment status updated successfully.";
            return RedirectToAction("ViewAppointments");
        }


        // GET: /Vendor/CreateProduct
        public IActionResult CreateProduct(int businessId)
        {
            ViewData["BusinessId"] = businessId;
            return View();
        }

        // GET: /Vendor/CreateService
        public IActionResult CreateService(int businessId)
        {
            ViewData["BusinessId"] = businessId;
            return View();
        }

        // POST: /Vendor/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(int businessId, Product product)
        {
            ModelState.Remove("BusinessProfile");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("BusinessProfileId");

            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("Model error (Product): " + modelError.ErrorMessage);
                }

                ViewData["BusinessId"] = businessId;
                return View(product);
            }

            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(product.ImageFile.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.ImageFile.CopyToAsync(stream);
                }

                product.ImageUrl = "/images/" + uniqueFileName;
            }
            var business = await _context.BusinessProfiles.FindAsync(businessId);
            if (business == null)
            {
                return NotFound();
            }


            product.BusinessProfileId = businessId;

            _context.Products.Add(product);
            var result = await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }



       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(int businessId, Service service, int DurationHours, int DurationMinutes)
        {
            ModelState.Remove("BusinessProfile");
            ModelState.Remove("BusinessProfileId");
            ModelState.Remove("Duration");
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("Model error (Service): " + modelError.ErrorMessage);
                }

                ViewData["BusinessId"] = businessId;
                return View(service);
            }
            if (service.ImageFile != null && service.ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(service.ImageFile.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await service.ImageFile.CopyToAsync(stream);
                }

                service.ImageUrl = "/images/" + uniqueFileName;
            }

            var business = await _context.BusinessProfiles.FindAsync(businessId);
            if (business == null)
            {
                return NotFound();
            }

            service.BusinessProfileId = businessId;
            service.Duration = new TimeSpan(DurationHours, DurationMinutes, 0);

            _context.Services.Add(service);
            var result = await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }


        // POST: /Vendor/CreateBusiness
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBusiness(BusinessProfile model)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("Model error: " + modelError.ErrorMessage);
                }

                return View(model);
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(model.ImageFile.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                model.ImageUrl = "/images/" + uniqueFileName;
            }

            var user = await _userManager.GetUserAsync(User);
            model.UserId = user.Id;

            _context.BusinessProfiles.Add(model);
            var result = await _context.SaveChangesAsync();
            Console.WriteLine("Saved rows: " + result);

            return RedirectToAction("Dashboard");
        }
        // GET: /Vendor/EditProduct/5
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: /Vendor/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Ignore these fields during validation
            ModelState.Remove("BusinessProfile");
            ModelState.Remove("BusinessProfileId");
            ModelState.Remove("ImageFile"); // <---- ADD THIS LINE

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(existingProduct);
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;

            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(product.ImageFile.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.ImageFile.CopyToAsync(stream);
                }

                existingProduct.ImageUrl = "/images/" + uniqueFileName;
            }
            // Else, keep existingProduct.ImageUrl

            _context.Update(existingProduct);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }



        // POST: /Vendor/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }
        // GET: /Vendor/EditService/5
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // POST: /Vendor/EditService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, Service service, int DurationHours, int DurationMinutes)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            // Ignore these fields during validation
            ModelState.Remove("BusinessProfile");
            ModelState.Remove("BusinessProfileId");
            ModelState.Remove("ImageFile"); // <---- ADD THIS LINE


            var existingService = await _context.Services.FindAsync(id);

            if (existingService == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {

                    existingService.Name = service.Name;
                    existingService.Description = service.Description;
                    existingService.Price = service.Price;
                    existingService.Duration = new TimeSpan(DurationHours, DurationMinutes, 0);
                    if (service.ImageFile != null && service.ImageFile.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(service.ImageFile.FileName);
                        string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        Directory.CreateDirectory(uploadPath);
                        string filePath = Path.Combine(uploadPath, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await service.ImageFile.CopyToAsync(stream);
                        }

                        existingService.ImageUrl = "/images/" + uniqueFileName;
                    }
                    // Else, keep existingProduct.ImageUrl

                    _context.Update(existingService);
                    await _context.SaveChangesAsync();
               
                }
                return RedirectToAction(nameof(Dashboard));
           
        }
        // POST: /Vendor/DeleteService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }


    }
}
