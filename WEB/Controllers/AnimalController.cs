using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;  
using WEB.Models;        
using System.Linq;    

public class AnimalController : Controller
{
    private readonly IAnimalService _animalService;
    private readonly IReviewService _reviewService;

    public AnimalController(
        IAnimalService animalService,
        IReviewService reviewService)
    {
        _animalService = animalService;
        _reviewService = reviewService;
    }

    [AllowAnonymous]     
    public async Task<IActionResult> All()
    {
        var animals = await _animalService.GetAllAnimalsAsync();
        return View(animals);    
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var animalDto = await _animalService.GetAnimalByIdAsync(id);
        if (animalDto == null)
        {
            return NotFound();       
        }

        var reviews = await _reviewService.GetReviewsByAnimalAsync(id);
        ViewBag.Reviews = reviews?.ToList() ?? new List<BLL.DTO.ReviewDTO>();     

        return View(animalDto);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Available()
    {
        var animals = await _animalService.GetAvailableAnimalsAsync();
        return View(animals);     
    }

}