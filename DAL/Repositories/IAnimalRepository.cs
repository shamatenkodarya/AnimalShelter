using DAL.Entities;

namespace DAL.Repositories
{
    public interface IAnimalRepository
    {
        Task<Animal> GetByIdAsync(int id);
        Task<IEnumerable<Animal>> GetAllAsync();
        Task AddAsync(Animal animal);
        Task UpdateAsync(Animal animal);
        Task DeleteAsync(int id);
        Task<IEnumerable<Animal>> GetByTypeAsync(AnimalType type);
        Task<IEnumerable<Animal>> GetAvailableAnimalsAsync();
    }
}