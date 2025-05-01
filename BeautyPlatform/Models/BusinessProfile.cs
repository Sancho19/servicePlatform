using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace BeautyPlatform.Models
{
    public class BusinessProfile
    {
        
            public int Id { get; set; }

            public string UserId { get; set; }

            [Required]
            public string BusinessName { get; set; }

            public string Description { get; set; }
            public string ImageUrl { get; set; }
        [NotMapped]  // This will prevent EF from trying to map the IFormFile property
        public IFormFile ImageFile { get; set; } // This property is for handling the uploaded file

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    }
}
