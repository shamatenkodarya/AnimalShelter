using BLL.DTO;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.IO;
using WEB.Models;    
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;   
using Microsoft.AspNetCore.Http;   
using Microsoft.Extensions.Logging;   

[Authorize(Roles = "Manager")]      
public class ManagerController : Controller
{
    private readonly IAnimalService _animalService;
    private readonly IReportService _reportService;
    private readonly ILogger<ManagerController> _logger;

    public ManagerController(
        IAnimalService animalService,
        IReportService reportService,
        ILogger<ManagerController> logger)
    {
        _animalService = animalService;
        _reportService = reportService;
        _logger = logger;
    }

    public async Task<IActionResult> AnimalList()
    {
        _logger.LogInformation("GET AnimalList requested.");
        try
        {
            var animals = await _animalService.GetAllAnimalsAsync();
            return View(animals ?? new List<AnimalDTO>());      
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching animal list.");
            TempData["ErrorMessage"] = "Не удалось загрузить список животных.";
            return View(new List<AnimalDTO>());      
        }
    }

    [HttpGet]
    public IActionResult AddAnimal()
    {
        _logger.LogInformation("GET AddAnimal requested.");
        var model = new AnimalViewModel
        {
            AdmissionDate = DateTime.Today      
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]    
    public async Task<IActionResult> AddAnimal(AnimalViewModel model)
    {
        _logger.LogInformation("POST AddAnimal entered.");

        ModelState.Remove(nameof(model.ExistingPhotoBase64));
        ModelState.Remove(nameof(model.Id));
        ModelState.Remove(nameof(model.Vaccinations));     

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid in AddAnimal POST.");
            LogModelStateErrors();    
            return View(model);       
        }

        try
        {
            var animalDto = new AnimalDTO
            {
                Name = model.Name,
                Type = model.Type,
                Age = model.Age,
                Gender = model.Gender,
                Weight = model.Weight,
                Details = new AnimalDetailsDTO
                {
                    Breed = model.Breed,
                    Color = model.Color,
                    HealthNotes = model.HealthNotes,
                    BehaviorDescription = model.BehaviorDescription
                },
                Vaccinations = new List<VaccinationDTO>()    
            };

            if (!string.IsNullOrWhiteSpace(model.NewVaccinationName) && model.NewVaccinationDate.HasValue)
            {
                animalDto.Vaccinations.Add(new VaccinationDTO
                {
                    Name = model.NewVaccinationName,
                    DateAdministered = model.NewVaccinationDate.Value
                });
                _logger.LogInformation("Adding initial vaccination '{VaccName}' for new animal.", model.NewVaccinationName);
            }

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                _logger.LogInformation("Processing uploaded PhotoFile for new animal.");
                if (!await IsValidImage(model.PhotoFile))    
                {
                    return View(model);       
                }

                using var ms = new MemoryStream();
                await model.PhotoFile.CopyToAsync(ms);
                animalDto.PhotoBase64 = Convert.ToBase64String(ms.ToArray());
                _logger.LogInformation("Photo converted to Base64 for new animal.");
            }
            else
            {
                _logger.LogWarning("No PhotoFile provided for new animal '{AnimalName}'.", model.Name);
                animalDto.PhotoBase64 = null;   
            }

            await _animalService.AddAnimalAsync(animalDto);
            _logger.LogInformation("Animal '{AnimalName}' added successfully.", animalDto.Name);
            TempData["SuccessMessage"] = $"Животное '{animalDto.Name}' успешно добавлено.";
            return RedirectToAction(nameof(AnimalList));    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding animal '{AnimalName}'.", model.Name);
            ModelState.AddModelError("", $"Произошла ошибка при добавлении животного: {ex.Message}");
            return View(model);       
        }
    }


    [HttpGet]
    public async Task<IActionResult> EditAnimal(int id)
    {
        _logger.LogInformation("GET EditAnimal requested for ID: {AnimalId}", id);
        var animalDto = await _animalService.GetAnimalByIdAsync(id);
        if (animalDto == null)
        {
            _logger.LogWarning("Animal with ID {AnimalId} not found for editing.", id);
            return NotFound();    
        }

        var model = new AnimalViewModel
        {
            Id = animalDto.Id,
            Name = animalDto.Name,
            Type = animalDto.Type,
            Age = animalDto.Age,
            Gender = animalDto.Gender,
            Weight = animalDto.Weight,
            ExistingPhotoBase64 = animalDto.PhotoBase64,    
            IsAdopted = animalDto.IsAdopted,
            AdmissionDate = animalDto.AdmissionDate,
            Breed = animalDto.Details?.Breed,
            Color = animalDto.Details?.Color,
            HealthNotes = animalDto.Details?.HealthNotes,
            BehaviorDescription = animalDto.Details?.BehaviorDescription,
            Vaccinations = animalDto.Vaccinations ?? new List<VaccinationDTO>()   
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnimal(int id, AnimalViewModel model)
    {
        _logger.LogInformation("POST EditAnimal submitted for ID: {AnimalId}", id);
        if (id != model.Id)
        {
            _logger.LogWarning("Mismatched ID in EditAnimal POST. Route ID: {RouteId}, Model ID: {ModelId}", id, model.Id);
            return BadRequest("Несоответствие ID.");
        }

        ModelState.Remove(nameof(model.PhotoFile));    
        ModelState.Remove(nameof(model.Vaccinations));     

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid in EditAnimal POST for ID {AnimalId}.", id);
            LogModelStateErrors();   

            var currentAnimalDtoForView = await _animalService.GetAnimalByIdAsync(id);
            if (currentAnimalDtoForView != null)
            {
                model.Vaccinations = currentAnimalDtoForView.Vaccinations ?? new List<VaccinationDTO>();
                model.ExistingPhotoBase64 = currentAnimalDtoForView.PhotoBase64;    
                model.AdmissionDate = currentAnimalDtoForView.AdmissionDate;   
                model.IsAdopted = currentAnimalDtoForView.IsAdopted;
            }
            else         
            {
                TempData["ErrorMessage"] = "Животное не найдено.";
                return RedirectToAction(nameof(AnimalList));
            }
            return View(model);      
        }

        try
        {
            var animalDto = await _animalService.GetAnimalByIdAsync(id);
            if (animalDto == null)
            {
                _logger.LogWarning("Animal with ID {AnimalId} not found during POST EditAnimal.", id);
                return NotFound();
            }

            animalDto.Name = model.Name;
            animalDto.Type = model.Type;
            animalDto.Age = model.Age;
            animalDto.Gender = model.Gender;
            animalDto.Weight = model.Weight;

            if (animalDto.Details == null) animalDto.Details = new AnimalDetailsDTO();
            animalDto.Details.Breed = model.Breed;
            animalDto.Details.Color = model.Color;
            animalDto.Details.HealthNotes = model.HealthNotes;
            animalDto.Details.BehaviorDescription = model.BehaviorDescription;

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                _logger.LogInformation("Processing uploaded PhotoFile for editing animal ID {AnimalId}.", id);
                if (!await IsValidImage(model.PhotoFile))   
                {
                    var currentAnimalDtoForView = await _animalService.GetAnimalByIdAsync(id);
                    if (currentAnimalDtoForView != null)
                    {
                        model.Vaccinations = currentAnimalDtoForView.Vaccinations ?? new List<VaccinationDTO>();
                        model.ExistingPhotoBase64 = currentAnimalDtoForView.PhotoBase64;
                        model.AdmissionDate = currentAnimalDtoForView.AdmissionDate;
                        model.IsAdopted = currentAnimalDtoForView.IsAdopted;
                    }
                    return View(model);
                }

                using var ms = new MemoryStream();
                await model.PhotoFile.CopyToAsync(ms);
                animalDto.PhotoBase64 = Convert.ToBase64String(ms.ToArray());   
                _logger.LogInformation("New photo saved to DTO for animal ID {AnimalId}.", id);
            }
            else
            {
                _logger.LogInformation("No new photo uploaded for animal ID {AnimalId}. Keeping existing photo from DTO.", id);
            }

            if (!string.IsNullOrWhiteSpace(model.NewVaccinationName) && model.NewVaccinationDate.HasValue)
            {
                if (animalDto.Vaccinations == null) animalDto.Vaccinations = new List<VaccinationDTO>();

                bool exists = animalDto.Vaccinations.Any(v =>
                    v.Name.Equals(model.NewVaccinationName, StringComparison.OrdinalIgnoreCase)
                    && v.DateAdministered.Date == model.NewVaccinationDate.Value.Date);

                if (!exists)
                {
                    animalDto.Vaccinations.Add(new VaccinationDTO
                    {
                        Id = 0,   
                        Name = model.NewVaccinationName,
                        DateAdministered = model.NewVaccinationDate.Value,
                        AnimalId = animalDto.Id   
                    });
                    _logger.LogInformation("Added new vaccination '{VaccName}' to DTO for animal ID {AnimalId}.", model.NewVaccinationName, id);
                }
                else
                {
                    TempData["WarningMessage"] = "Прививка с таким названием и датой уже существует для этого животного.";
                    _logger.LogWarning("Attempted to add duplicate vaccination '{VaccName}' for animal ID {AnimalId}.", model.NewVaccinationName, id);
                }
            }
            await _animalService.UpdateAnimalAsync(animalDto);
            _logger.LogInformation("UpdateAnimalAsync service call completed for animal ID {AnimalId}.", id);

            TempData["SuccessMessage"] = $"Данные '{animalDto.Name}' успешно обновлены.";
            return RedirectToAction(nameof(AnimalList));    
        }
        catch (InvalidOperationException ex)      
        {
            _logger.LogError(ex, "Invalid operation during editing animal ID {AnimalId}.", id);
            ModelState.AddModelError("", ex.Message);
        }
        catch (Exception ex)    
        {
            _logger.LogError(ex, "Error occurred while editing animal ID {AnimalId}.", id);
            ModelState.AddModelError("", $"Произошла ошибка при обновлении животного: {ex.Message}");
        }

        _logger.LogInformation("Reloading data for EditAnimal view after exception for ID {AnimalId}.", id);
        var currentAnimalDtoRetry = await _animalService.GetAnimalByIdAsync(id);
        if (currentAnimalDtoRetry != null)
        {
            model.Vaccinations = currentAnimalDtoRetry.Vaccinations ?? new List<VaccinationDTO>();
            model.ExistingPhotoBase64 = currentAnimalDtoRetry.PhotoBase64;
            model.AdmissionDate = currentAnimalDtoRetry.AdmissionDate;
            model.IsAdopted = currentAnimalDtoRetry.IsAdopted;
        }
        else    
        {
            TempData["ErrorMessage"] = "Не удалось найти животное для редактирования.";
            return RedirectToAction(nameof(AnimalList));
        }
        return View(model);      
    }


    [HttpGet]
    public async Task<IActionResult> DeleteAnimal(int id)
    {
        _logger.LogInformation("GET DeleteAnimal requested for ID: {AnimalId}", id);
        var animal = await _animalService.GetAnimalByIdAsync(id);
        if (animal == null)
        {
            _logger.LogWarning("Animal with ID {AnimalId} not found for deletion.", id);
            return NotFound();
        }
        return View(animal);      
    }

    [HttpPost, ActionName("DeleteAnimal")]        
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnimalConfirmed(int id)
    {
        _logger.LogInformation("POST DeleteAnimalConfirmed attempting to delete ID: {AnimalId}", id);
        try
        {
            var animal = await _animalService.GetAnimalByIdAsync(id);     
            if (animal == null)
            {
                _logger.LogWarning("Animal with ID {AnimalId} not found during DeleteAnimalConfirmed (already deleted?).", id);
                TempData["WarningMessage"] = $"Животное с ID {id} не найдено (возможно, уже удалено).";
                return RedirectToAction(nameof(AnimalList));
            }

            await _animalService.DeleteAnimalAsync(id);
            _logger.LogInformation("Successfully deleted animal ID {AnimalId}, Name: {AnimalName}", id, animal.Name);
            TempData["SuccessMessage"] = $"Животное '{animal.Name}' успешно удалено.";
            return RedirectToAction(nameof(AnimalList));
        }
        catch (DbUpdateException ex)       
        {
            _logger.LogWarning(ex, "DbUpdateException occurred while deleting animal ID {AnimalId}. Potential FK constraint violation.", id);
            var animal = await _animalService.GetAnimalByIdAsync(id);     
            TempData["ErrorMessage"] = $"Не удалось удалить животное '{animal?.Name ?? "ID: " + id}'. Возможно, оно связано с записями об усыновлении или другими данными.";
            return RedirectToAction(nameof(DeleteAnimal), new { id = id });    
        }
        catch (Exception ex)   
        {
            _logger.LogError(ex, "Error occurred while deleting animal ID {AnimalId}.", id);
            var animal = await _animalService.GetAnimalByIdAsync(id);     
            TempData["ErrorMessage"] = $"Произошла ошибка при удалении животного '{animal?.Name ?? "ID: " + id}'. {ex.Message}";
            return RedirectToAction(nameof(DeleteAnimal), new { id = id });    
        }
    }


    [HttpGet]
    public async Task<IActionResult> Statistics()
    {
        _logger.LogInformation("GET Statistics requested.");
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-6);      

            var adoptionReport = await _reportService.GenerateAdoptionReport(startDate, endDate);
            var animalCounts = await _reportService.GetAnimalCountsByTypeAsync();
            var admissionTrends = await _reportService.GetAdmissionTrendsAsync(12);     
            var allAnimals = await _animalService.GetAllAnimalsAsync();    

            var model = new ManagerStatisticsViewModel
            {
                AdoptionReport = adoptionReport,
                AnimalCountsByType = animalCounts ?? new Dictionary<string, int>(),
                AdmissionTrends = admissionTrends ?? new Dictionary<string, int>(),
                TotalAnimalsInShelter = allAnimals.Count(a => !a.IsAdopted),
                TotalAdoptedAnimals = allAnimals.Count(a => a.IsAdopted)
            };

            _logger.LogInformation("Statistics data loaded successfully.");
            return View(model);   
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading statistics.");
            TempData["ErrorMessage"] = "Не удалось загрузить статистику. Пожалуйста, попробуйте позже.";
            return View(new ManagerStatisticsViewModel());
        }
    }

    private async Task<bool> IsValidImage(IFormFile file)
    {
        if (file.Length > 5 * 1024 * 1024)    
        {
            ModelState.AddModelError(nameof(AnimalViewModel.PhotoFile), "Размер фото не должен превышать 5 МБ.");
            _logger.LogWarning("Uploaded file size exceeds limit ({FileSize} bytes) for {FileName}.", file.Length, file.FileName);
            return false;
        }
        if (!file.ContentType.StartsWith("image/"))
        {
            ModelState.AddModelError(nameof(AnimalViewModel.PhotoFile), "Пожалуйста, загрузите файл изображения (JPEG, PNG, GIF и т.д.).");
            _logger.LogWarning("Uploaded file is not an image ({ContentType}) for {FileName}.", file.ContentType, file.FileName);
            return false;
        }
        return true;     
    }

    private void LogModelStateErrors()
    {
        foreach (var state in ModelState)
        {
            if (state.Value.Errors.Any())
            {
                var fieldName = state.Key;
                var errorMessage = state.Value.Errors.First().ErrorMessage;
                var exceptionMessage = state.Value.Errors.First().Exception?.Message;

                _logger.LogWarning("ModelState Error - Field: {FieldName}, Error: {ErrorMessage}, Exception: {Exception}",
                    fieldName, errorMessage, exceptionMessage ?? "N/A");
            }
        }
    }
}