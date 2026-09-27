using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTO
{
    public class AdoptionDTO
    {
        public int Id { get; set; }
        public DateTime AdoptionDate { get; set; }
        public string AdopterFullName { get; set; }
        public string AdopterContacts { get; set; }
        public string Address { get; set; }
        public int AnimalId { get; set; }
        public string AnimalName { get; set; }
        public string UserId { get; set; }
    }

}
