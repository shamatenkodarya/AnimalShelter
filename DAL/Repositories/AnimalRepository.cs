using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;  
using System.Linq;  
using System.Threading.Tasks;  


namespace DAL.Repositories
{
    public class AnimalRepository : IAnimalRepository
    {
        private readonly ApplicationDbContext _context;

        public AnimalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Animal> GetByIdAsync(int id)
        {
            return await _context.Animals
                .Include(a => a.Details)          
                .Include(a => a.Vaccinations)     
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Animal>> GetAllAsync()
        {
            return await _context.Animals
                .Include(a => a.Details)   
                .ToListAsync();
        }

        public async Task AddAsync(Animal animal)
        {
            await _context.Animals.AddAsync(animal);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Animal animal)
        {
            _context.Animals.Update(animal);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var animal = await _context.Animals.FindAsync(id);
            if (animal != null)
            {
                _context.Animals.Remove(animal);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Animal>> GetByTypeAsync(AnimalType type)
        {
            return await _context.Animals
                 .Include(a => a.Details)
                .Where(a => a.Type == type)
                .ToListAsync();
        }

        public async Task<IEnumerable<Animal>> GetAvailableAnimalsAsync()
        {
            return await _context.Animals
                .Include(a => a.Details)
                .Where(a => !a.IsAdopted)
                .ToListAsync();
        }
    }
}