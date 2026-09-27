using DAL.Entities;

namespace DAL.Repositories
{
    public interface IAdoptionRepository
    {
        Task CreateAsync(Adoption adoption);
        Task<IEnumerable<Adoption>> GetByPeriodAsync(DateTime start, DateTime end);
        Task<IEnumerable<Adoption>> GetByAnimalTypeAsync(AnimalType type);
    }
}