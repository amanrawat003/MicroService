using System.ComponentModel.DataAnnotations;

namespace ProductService.Models
{
    public class ProcessedEvent
    {
        [Key]
        public Guid EventId { get; set; }
    }

}
