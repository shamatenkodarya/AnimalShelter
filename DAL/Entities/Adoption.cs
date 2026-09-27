namespace DAL.Entities
{
    public class Adoption
    {
        public int Id { get; set; }
        public DateTime AdoptionDate { get; set; }
        public string AdopterFullName { get; set; }
        public string AdopterContacts { get; set; }
        public string Address { get; set; }

        public int AnimalId { get; set; }
        public Animal Animal { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}