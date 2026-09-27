namespace DAL.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public int AnimalId { get; set; }
        public Animal Animal { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
