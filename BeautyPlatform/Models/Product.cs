using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyPlatform.Models
{
    public class Product
    {
        public int Id { get; set; }

        
        public int BusinessProfileId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        [NotMapped]  // This will prevent EF from trying to map the IFormFile property
        public IFormFile ImageFile { get; set; } // This property is for handling the uploaded file


        public virtual BusinessProfile BusinessProfile { get; set; }
    }
}
