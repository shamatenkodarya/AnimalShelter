using BLL.DTO;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[Authorize(Roles = "Visitor")]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IAnimalService _animalService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(
        IReviewService reviewService,
        IAnimalService animalService,
        ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _animalService = animalService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int animalId)
    {
        _logger.LogInformation("GET Create Review requested for AnimalId: {AnimalId}", animalId);
        var animal = await _animalService.GetAnimalByIdAsync(animalId);
        if (animal == null)
        {
            _logger.LogWarning("Animal with ID {AnimalId} not found for creating review.", animalId);
            TempData["ErrorMessage"] = "Животное не найдено.";
            return RedirectToAction("All", "Animal");
        }

        var model = new ReviewDTO
        {
            AnimalId = animalId,
            AnimalName = animal.Name
        };

        ViewData["AnimalName"] = animal.Name;
        _logger.LogInformation("Showing Create Review form for Animal: {AnimalName}", animal.Name);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewDTO reviewDto)
    {
        _logger.LogInformation("POST Create Review submitted for AnimalId: {AnimalId}", reviewDto.AnimalId);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims during POST Create Review.");
            ModelState.AddModelError("", "Не удалось определить пользователя.");
            ViewData["AnimalName"] = (await _animalService.GetAnimalByIdAsync(reviewDto.AnimalId))?.Name ?? "Животное";
            if (string.IsNullOrEmpty(reviewDto.AnimalName)) reviewDto.AnimalName = ViewData["AnimalName"] as string;
            return View(reviewDto);
        }
        _logger.LogInformation("User ID {UserId} identified for review.", userId);
        reviewDto.UserId = userId;      

        ModelState.Remove(nameof(reviewDto.UserId));
        ModelState.Remove(nameof(reviewDto.UserName));
        if (ModelState.IsValid)
        {
            _logger.LogInformation("ModelState is valid for Create Review.");
            try
            {
                _logger.LogInformation("Calling AddReviewAsync service method for AnimalId: {AnimalId}", reviewDto.AnimalId);
                reviewDto.UserName = null;      
                await _reviewService.AddReviewAsync(reviewDto);
                _logger.LogInformation("AddReviewAsync completed successfully for AnimalId: {AnimalId}", reviewDto.AnimalId);
                TempData["SuccessMessage"] = "Ваш отзыв успешно добавлен!";
                return RedirectToAction("Details", "Animal", new { id = reviewDto.AnimalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding review for AnimalId: {AnimalId}", reviewDto.AnimalId);
                ModelState.AddModelError("", "Произошла ошибка при добавлении отзыва.");
                TempData["ErrorMessage"] = "Не удалось добавить отзыв.";
            }
        }
        else
        {
            _logger.LogWarning("ModelState is invalid for Create Review (after removing UserId/UserName errors). Errors:");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    _logger.LogWarning("- Remaining Field: {FieldName}, Error: {ErrorMessage}",
                        state.Key, state.Value.Errors.First().ErrorMessage);
                }
            }
        }

        _logger.LogInformation("Returning to Create Review view due to invalid ModelState or error.");
        ViewData["AnimalName"] = (await _animalService.GetAnimalByIdAsync(reviewDto.AnimalId))?.Name ?? "Животное";
        if (string.IsNullOrEmpty(reviewDto.AnimalName))
        {
            reviewDto.AnimalName = ViewData["AnimalName"] as string;
        }
        return View(reviewDto);
    }
}