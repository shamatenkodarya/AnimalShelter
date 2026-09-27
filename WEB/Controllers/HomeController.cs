using System.Diagnostics;
using BLL.IServices;
using Microsoft.AspNetCore.Mvc;
using WEB.Models;

namespace WEB.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IReviewService _reviewService;
    private readonly IAnimalService _animalService;

    public HomeController(
        ILogger<HomeController> logger,
        IReviewService reviewService,
        IAnimalService animalService)
    {
        _logger = logger;
        _reviewService = reviewService;
        _animalService = animalService;
    }

    public async Task<IActionResult> Index()
    {
        var animals = await _animalService.GetAllAnimalsAsync();
        var reviews = await _reviewService.GetRecentReviewsAsync(3);

        ViewBag.RecentReviews = reviews;
        return View(animals);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
}
