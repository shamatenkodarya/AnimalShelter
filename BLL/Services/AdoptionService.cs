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
    public class AdoptionService : IAdoptionService
    {
        private readonly IAdoptionRepository _adoptionRepository;
        private readonly IAnimalRepository _animalRepository;
        private readonly IMapper _mapper;  

        public AdoptionService(
            IAdoptionRepository adoptionRepository,
            IAnimalRepository animalRepository,
            IMapper mapper)  
        {
            _adoptionRepository = adoptionRepository;
            _animalRepository = animalRepository;
            _mapper = mapper;  
        }

        public async Task CreateAdoptionAsync(AdoptionDTO adoptionDto)
        {
            var animal = await _animalRepository.GetByIdAsync(adoptionDto.AnimalId);
            if (animal == null)
                throw new InvalidOperationException("Животное не найдено.");
            if (animal.IsAdopted)
                throw new InvalidOperationException("Животное уже усыновлено.");

            var adoption = _mapper.Map<Adoption>(adoptionDto);
            adoption.AdoptionDate = DateTime.UtcNow;    

            await _adoptionRepository.CreateAsync(adoption);

            animal.IsAdopted = true;
            await _animalRepository.UpdateAsync(animal);    
        }

        public async Task<IEnumerable<AdoptionDTO>> GetAdoptionsByPeriodAsync(DateTime start, DateTime end)
        {
            var adoptions = await _adoptionRepository.GetByPeriodAsync(start, end);
            return _mapper.Map<IEnumerable<AdoptionDTO>>(adoptions);
        }

        public async Task<IEnumerable<AdoptionDTO>> GetAdoptionsByAnimalTypeAsync(AnimalType type)
        {
            var adoptions = await _adoptionRepository.GetByAnimalTypeAsync(type);
            return _mapper.Map<IEnumerable<AdoptionDTO>>(adoptions);
        }
    }
}