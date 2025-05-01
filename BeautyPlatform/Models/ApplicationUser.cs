using Microsoft.AspNetCore.Identity;
namespace BeautyPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
