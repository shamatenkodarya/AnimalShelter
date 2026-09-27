using AutoMapper;
using BLL.IServices;
using Microsoft.EntityFrameworkCore;
using BLL.DTO;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public ReportService(ApplicationDbContext context, IMapper mapper     )
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<AdoptionReportDTO> GenerateAdoptionReport(DateTime start, DateTime end)
        {
            var adoptionsInPeriod = _context.Adoptions
                .Include(a => a.Animal)
                .Where(a => a.AdoptionDate >= start && a.AdoptionDate <= end);

            var totalAdoptions = await adoptionsInPeriod.CountAsync();

            var adoptionsByType = await adoptionsInPeriod
                .GroupBy(a => a.Animal.Type)
                .Select(g => new { AnimalType = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(g => g.AnimalType, g => g.Count);

            var report = new AdoptionReportDTO
            {
                StartDate = start,
                EndDate = end,
                TotalAdoptions = totalAdoptions,
                AdoptionsByType = adoptionsByType ?? new Dictionary<string, int>()
            };

            return report;
        }

        public async Task<Dictionary<string, int>> GetAnimalCountsByTypeAsync()
        {
            return await _context.Animals
                .Where(a => !a.IsAdopted)
                .GroupBy(a => a.Type)
                .Select(g => new { AnimalType = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(g => g.AnimalType, g => g.Count);
        }

        public async Task<Dictionary<string, int>> GetAdmissionTrendsAsync(int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var trendsData = await _context.Animals
                .Where(a => a.AdmissionDate >= startDate)
                .GroupBy(a => new { Year = a.AdmissionDate.Year, Month = a.AdmissionDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                 .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();   

            var resultDictionary = trendsData
                .ToDictionary(
                    g => $"{g.Month:00}-{g.Year}",    
                    g => g.Count
                );

            return resultDictionary;
        }
        public async Task<AdoptionReportDTO> GenerateAdoptionReportForType(DateTime start, DateTime end, AnimalType animalType)
        {
            var adoptionsInPeriod = _context.Adoptions
                .Include(a => a.Animal)
                .Where(a => a.AdoptionDate >= start && a.AdoptionDate <= end && a.Animal.Type == animalType);

            var totalAdoptions = await adoptionsInPeriod.CountAsync();

            var report = new AdoptionReportDTO
            {
                StartDate = start,
                EndDate = end,
                TotalAdoptions = totalAdoptions,
                AdoptionsByType = new Dictionary<string, int> { { animalType.ToString(), totalAdoptions } }
            };
            return report;
        }
    }
}