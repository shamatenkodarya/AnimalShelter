using BLL.DTO;
using DAL.Entities;    
using System;  
using System.Collections.Generic;  
using System.Threading.Tasks;  

namespace BLL.IServices
{
    public interface IReportService
    {
        Task<AdoptionReportDTO> GenerateAdoptionReport(DateTime start, DateTime end);

        Task<AdoptionReportDTO> GenerateAdoptionReportForType(DateTime start, DateTime end, AnimalType animalType);

        Task<Dictionary<string, int>> GetAnimalCountsByTypeAsync();

        Task<Dictionary<string, int>> GetAdmissionTrendsAsync(int months = 6);

    }
}