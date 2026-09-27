namespace DAL.Entities
{
    public class AnimalDetails
    {
        public int Id { get; set; }
        public string Breed { get; set; }  
        public string Color { get; set; }
        public string HealthNotes { get; set; }
        public string BehaviorDescription { get; set; }

        public int AnimalId { get; set; }
        public Animal Animal { get; set; }
    }
}