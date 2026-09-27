using BLL.DTO;
using DAL.Entities;

namespace BLL.IServices
{
    public interface IAdoptionService
    {
        Task CreateAdoptionAsync(AdoptionDTO adoptionDto);
        Task<IEnumerable<AdoptionDTO>> GetAdoptionsByPeriodAsync(DateTime start, DateTime end);
        Task<IEnumerable<AdoptionDTO>> GetAdoptionsByAnimalTypeAsync(AnimalType type);
    }
}