using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTO
{
    public class AnimalDetailsDTO
    {
        public int Id { get; set; }
        public string Breed { get; set; }  
        public string Color { get; set; }
        public string HealthNotes { get; set; }
        public string BehaviorDescription { get; set; }
        public int AnimalId { get; set; }
    }
}