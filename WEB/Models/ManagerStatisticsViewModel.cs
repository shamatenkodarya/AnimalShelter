using System.Collections.Generic;

namespace WEB.Models
{
    public class ManagerStatisticsViewModel
    {
        public BLL.DTO.AdoptionReportDTO AdoptionReport { get; set; }

        public Dictionary<string, int> AnimalCountsByType { get; set; }

        public Dictionary<string, int> AdmissionTrends { get; set; }

        public int TotalAnimalsInShelter { get; set; }
        public int TotalAdoptedAnimals { get; set; }
    }
}