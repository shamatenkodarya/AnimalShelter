using DAL.Entities;

namespace DAL.Repositories
{
    public interface IReviewRepository
    {
        Task AddAsync(Review review);
        Task<IEnumerable<Review>> GetRecentAsync(int count);
        Task<IEnumerable<Review>> GetByAnimalAsync(int animalId);
    }
}