using BLL.DTO;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WEB.Models;
using System.Security.Claims;
using System;
using Microsoft.Extensions.Logging;

[Authorize(Roles = "Consultant")]
public class ConsultantController : Controller
{
    private readonly IAnimalService _animalService;
    private readonly IAdoptionService _adoptionService;
    private readonly IUserService _userService;
    private readonly ILogger<ConsultantController> _logger;

    public ConsultantController(
        IAnimalService animalService,
        IAdoptionService adoptionService,
        IUserService userService,
        ILogger<ConsultantController> logger)
    {
        _animalService = animalService;
        _adoptionService = adoptionService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> AvailableAnimals()
    {
        _logger.LogInformation("GET AvailableAnimals requested.");
        var animals = await _animalService.GetAvailableAnimalsAsync();
        return View(animals);
    }

    [HttpGet]
    public async Task<IActionResult> CreateAdoption(int animalId)
    {
        _logger.LogInformation("GET CreateAdoption requested for AnimalId: {AnimalId}", animalId);
        var animal = await _animalService.GetAnimalByIdAsync(animalId);
        if (animal == null || animal.IsAdopted)
        {
            _logger.LogWarning("Animal with ID {AnimalId} not found or already adopted for CreateAdoption.", animalId);
            TempData["ErrorMessage"] = "Животное не найдено или уже усыновлено.";
            return RedirectToAction(nameof(AvailableAnimals));
        }

        var model = new AdoptionViewModel
        {
            AnimalId = animal.Id,
            Animal = animal
        };
        _logger.LogInformation("Showing CreateAdoption form for Animal: {AnimalName}", animal.Name);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdoption(AdoptionViewModel model)
    {
        _logger.LogInformation("POST CreateAdoption submitted for AnimalId: {AnimalId}", model.AnimalId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims during POST CreateAdoption.");
            ModelState.AddModelError("", "Не удалось определить пользователя.");
            model.Animal = await _animalService.GetAnimalByIdAsync(model.AnimalId);
            if (model.Animal == null) return RedirectToAction(nameof(AvailableAnimals));
            return View(model);
        }
        _logger.LogInformation("User ID {UserId} identified for adoption.", userId);

        ModelState.Remove(nameof(model.Animal));

        if (ModelState.IsValid)
        {
            _logger.LogInformation("ModelState is valid for CreateAdoption (after removing Animal error).");
            try
            {
                var adoptionDto = new AdoptionDTO
                {
                    AnimalId = model.AnimalId,
                    AdopterFullName = model.AdopterFullName,
                    AdopterContacts = model.AdopterContacts,
                    Address = model.Address,
                    UserId = userId,
                    AdoptionDate = model.AdoptionDate
                };
                _logger.LogInformation("Calling CreateAdoptionAsync service method for AnimalId: {AnimalId}", model.AnimalId);
                await _adoptionService.CreateAdoptionAsync(adoptionDto);
                _logger.LogInformation("CreateAdoptionAsync completed successfully for AnimalId: {AnimalId}", model.AnimalId);

                TempData["SuccessMessage"] = $"Усыновление животного '{model.Animal?.Name ?? "ID: " + model.AnimalId}' успешно оформлено.";
                return RedirectToAction(nameof(AvailableAnimals));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "InvalidOperationException during CreateAdoption for AnimalId: {AnimalId}", model.AnimalId);
                ModelState.AddModelError("", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating adoption for AnimalId: {AnimalId}", model.AnimalId);
                ModelState.AddModelError("", "Произошла ошибка при оформлении усыновления.");
                TempData["ErrorMessage"] = "Произошла ошибка при оформлении усыновления.";
            }
        }
        else
        {
            _logger.LogWarning("ModelState is invalid for CreateAdoption (after removing Animal error). Errors:");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    _logger.LogWarning("- Remaining Field: {FieldName}, Error: {ErrorMessage}",
                        state.Key, state.Value.Errors.First().ErrorMessage);
                }
            }
        }

        _logger.LogInformation("Returning to CreateAdoption view due to invalid ModelState or error.");
        model.Animal = await _animalService.GetAnimalByIdAsync(model.AnimalId);
        if (model.Animal == null)
        {
            _logger.LogWarning("Animal with ID {AnimalId} was not found when returning to CreateAdoption view.", model.AnimalId);
            TempData["ErrorMessage"] = "Животное, которое вы пытались оформить, больше не доступно.";
            return RedirectToAction(nameof(AvailableAnimals));
        }
        return View(model);
    }
}