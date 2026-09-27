using BLL.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class RecentReviewsViewComponent : ViewComponent      
{
    private readonly IReviewService _reviewService;

    public RecentReviewsViewComponent(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<IViewComponentResult> InvokeAsync(int count)
    {
        var reviews = await _reviewService.GetRecentReviewsAsync(count);
        return View(reviews);      
    }
}
