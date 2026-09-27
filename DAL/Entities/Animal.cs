using System.Reflection;

namespace DAL.Entities
{
    public class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AnimalType Type { get; set; }
        public int Age { get; set; }    
        public Gender Gender { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string PhotoBase64 { get; set; }
        public bool IsAdopted { get; set; }
        public decimal Weight { get; set; }    

        public ICollection<Vaccination> Vaccinations { get; set; } = new List<Vaccination>();
        public AnimalDetails Details { get; set; }
        public ICollection<Adoption> Adoptions { get; set; } = new List<Adoption>();
    }
}