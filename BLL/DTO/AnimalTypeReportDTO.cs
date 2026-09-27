using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTO
{
    public class AnimalTypeReportDTO
    {
        public AnimalType AnimalType { get; set; }
        public int TotalAdoptions { get; set; }
        public int VaccinationsCount { get; set; }
    }
}
