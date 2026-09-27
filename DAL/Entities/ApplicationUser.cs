using Microsoft.AspNetCore.Identity;

namespace DAL.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? Position
        {
            get; set;
        }     

        public ICollection<Adoption> Adoptions { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}