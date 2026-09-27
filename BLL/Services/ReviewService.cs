using AutoMapper;
using BLL.DTO;
using BLL.IServices;
using DAL.Entities;
using DAL.Repositories;
using System; 
using System.Collections.Generic; 
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public ReviewService(IReviewRepository reviewRepository, IMapper mapper )
        {
            _reviewRepository = reviewRepository;
            _mapper = mapper;
        }

        public async Task AddReviewAsync(ReviewDTO reviewDto)
        {
            var review = _mapper.Map<Review>(reviewDto);
            review.CreatedAt = DateTime.UtcNow; 
            await _reviewRepository.AddAsync(review);
        }

        public async Task<IEnumerable<ReviewDTO>> GetRecentReviewsAsync(int count)
        {
            var reviews = await _reviewRepository.GetRecentAsync(count);
            var reviewDtos = _mapper.Map<IEnumerable<ReviewDTO>>(reviews);
            return reviewDtos;
        }

        public async Task<IEnumerable<ReviewDTO>> GetReviewsByAnimalAsync(int animalId)
        {
            var reviews = await _reviewRepository.GetByAnimalAsync(animalId);
            return _mapper.Map<IEnumerable<ReviewDTO>>(reviews);
        }
    }
}