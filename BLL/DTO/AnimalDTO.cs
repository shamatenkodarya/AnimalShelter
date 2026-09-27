using DAL.Entities;         
using System;
using System.Collections.Generic;

namespace BLL.DTO
{
    public class AnimalDTO
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

        public AnimalDetailsDTO Details { get; set; }
        public List<VaccinationDTO> Vaccinations { get; set; } = new List<VaccinationDTO>();

    }
}