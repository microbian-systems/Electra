using Electra.Core.Entities;

namespace Electra.Persistence.Entities
{
    public class ContactMessage : EntityBase<long>
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }
}