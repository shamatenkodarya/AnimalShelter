using BLL.DTO;
using DAL.Entities;

namespace BLL.IServices
{
    public interface IAnimalService
    {
        Task AddAnimalAsync(AnimalDTO animalDto);
        Task UpdateAnimalAsync(AnimalDTO animalDto);
        Task DeleteAnimalAsync(int id);
        Task<AnimalDTO> GetAnimalByIdAsync(int id);
        Task<IEnumerable<AnimalDTO>> GetAllAnimalsAsync();
        Task<IEnumerable<AnimalDTO>> GetAvailableAnimalsAsync();
        Task<IEnumerable<AnimalDTO>> GetAnimalsByTypeAsync(AnimalType type);
    }
}