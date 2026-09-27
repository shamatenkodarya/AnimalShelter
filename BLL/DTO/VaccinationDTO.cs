using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace BLL.DTO
{
    public class VaccinationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateAdministered { get; set; }
        public int AnimalId { get; set; }
    }
}