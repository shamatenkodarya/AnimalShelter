using AutoMapper;
using BLL.DTO;
using BLL.IServices;
using DAL.Entities;
using DAL.Repositories;
using System;    
using System.Collections.Generic;     
using System.Linq;    
using System.Threading.Tasks;    

namespace BLL.Services
{
    public class AnimalService : IAnimalService
    {
        private readonly IAnimalRepository _animalRepository;
        private readonly IMapper _mapper;
        public AnimalService(
            IAnimalRepository animalRepository,
            IMapper mapper
                 )
        {
            _animalRepository = animalRepository;
            _mapper = mapper;
        }

        public async Task AddAnimalAsync(AnimalDTO animalDto)
        {
            animalDto.AdmissionDate = DateTime.UtcNow;
            animalDto.IsAdopted = false;     

            var animal = _mapper.Map<Animal>(animalDto);

            await _animalRepository.AddAsync(animal);
        }

        public async Task UpdateAnimalAsync(AnimalDTO animalDto)
        {
            var animal = await _animalRepository.GetByIdAsync(animalDto.Id);
            if (animal == null)
                throw new InvalidOperationException($"Животное не найдено.");

            _mapper.Map(animalDto, animal);

            if (animalDto.Details != null)
            {
                if (animal.Details == null)
                {
                    animal.Details = _mapper.Map<AnimalDetails>(animalDto.Details);
                }
                else
                {
                    _mapper.Map(animalDto.Details, animal.Details);
                }
            }
            if (animalDto.Vaccinations != null)
            {
                var dtoVaccinationIds = animalDto.Vaccinations.Select(v => v.Id).Where(id => id > 0).ToList();
                var existingVaccinations = animal.Vaccinations?.ToList() ?? new List<Vaccination>();

                var vaccinationsToRemove = existingVaccinations
                   .Where(ev => ev.Id > 0 && !dtoVaccinationIds.Contains(ev.Id))
                   .ToList();

                foreach (var vaccToRemove in vaccinationsToRemove)
                {
                    animal.Vaccinations.Remove(vaccToRemove);
                }


                foreach (var vaccDto in animalDto.Vaccinations)
                {
                    var existingVacc = existingVaccinations.FirstOrDefault(ev => ev.Id == vaccDto.Id && ev.Id > 0);
                    if (existingVacc != null)
                    {
                        _mapper.Map(vaccDto, existingVacc);
                    }
                    else           
                    {
                        bool alreadyExists = animal.Vaccinations.Any(v => v.Id == 0 &&
                            v.Name.Equals(vaccDto.Name, StringComparison.OrdinalIgnoreCase) &&
                            v.DateAdministered.Date == vaccDto.DateAdministered.Date);

                        if (!alreadyExists)
                        {
                            var newVacc = _mapper.Map<Vaccination>(vaccDto);
                            animal.Vaccinations.Add(newVacc);
                        }
                    }
                }
            }
            else           
            {
                animal.Vaccinations?.Clear();
            }


            await _animalRepository.UpdateAsync(animal);
        }


        public async Task DeleteAnimalAsync(int id)
        {
            await _animalRepository.DeleteAsync(id);
        }

        public async Task<AnimalDTO> GetAnimalByIdAsync(int id)
        {
            var animal = await _animalRepository.GetByIdAsync(id);       
            return _mapper.Map<AnimalDTO>(animal);
        }

        public async Task<IEnumerable<AnimalDTO>> GetAllAnimalsAsync()
        {
            var animals = await _animalRepository.GetAllAsync();     
            return _mapper.Map<IEnumerable<AnimalDTO>>(animals);
        }

        public async Task<IEnumerable<AnimalDTO>> GetAvailableAnimalsAsync()
        {
            var animals = await _animalRepository.GetAvailableAnimalsAsync();     
            return _mapper.Map<IEnumerable<AnimalDTO>>(animals);
        }

        public async Task<IEnumerable<AnimalDTO>> GetAnimalsByTypeAsync(AnimalType type)
        {
            var animals = await _animalRepository.GetByTypeAsync(type);     
            return _mapper.Map<IEnumerable<AnimalDTO>>(animals);
        }

    }
}