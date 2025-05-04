using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyPlatform.Models
{
    public class Appointment
    {
        public int Id { get; set; }

       
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

       
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        
        public DateTime AppointmentDateTime { get; set; }

        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "Unpaid"; // Options: Unpaid, Cash, Online

    }
}
