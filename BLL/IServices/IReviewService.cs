using BLL.DTO;
using DAL.Entities;

namespace BLL.IServices
{
    public interface IReviewService
    {
        Task AddReviewAsync(ReviewDTO reviewDto);
        Task<IEnumerable<ReviewDTO>> GetRecentReviewsAsync(int count);
        Task<IEnumerable<ReviewDTO>> GetReviewsByAnimalAsync(int animalId);
    }
}