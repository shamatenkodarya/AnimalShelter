using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AdoptionRepository : IAdoptionRepository
    {
        private readonly ApplicationDbContext _context;

        public AdoptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Adoption adoption)
        {
            await _context.Adoptions.AddAsync(adoption);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Adoption>> GetByPeriodAsync(DateTime start, DateTime end)
        {
            return await _context.Adoptions
                .Include(a => a.Animal)
                .Where(a => a.AdoptionDate >= start && a.AdoptionDate <= end)
                .ToListAsync();
        }

        public async Task<IEnumerable<Adoption>> GetByAnimalTypeAsync(AnimalType type)
        {
            return await _context.Adoptions
                .Include(a => a.Animal)
                .Where(a => a.Animal.Type == type)
                .ToListAsync();
        }
    }
}
