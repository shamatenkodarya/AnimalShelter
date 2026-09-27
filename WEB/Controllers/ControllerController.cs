using BLL.IServices;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WEB.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

[Authorize(Roles = "Controller")]
public class ControllerController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILogger<ControllerController> _logger;

    public ControllerController(IReportService reportService, ILogger<ControllerController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult AdoptionReport()
    {
        _logger.LogInformation("GET AdoptionReport requested.");
        var model = new ControllerReportViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdoptionReport(ControllerReportViewModel model)
    {
        _logger.LogInformation("POST AdoptionReport submitted. StartDate: {StartDate}, EndDate: {EndDate}, Filter: {Filter}",
            model.StartDate, model.EndDate, model.AnimalTypeFilter);

        ModelState.Remove(nameof(model.ReportResult));
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid in AdoptionReport POST (after removing ReportResult error).");
            LogModelStateErrors();    
            return View(model);
        }

        try
        {
            if (model.AnimalTypeFilter.HasValue)
            {
                _logger.LogInformation("Generating adoption report for type: {AnimalType}", model.AnimalTypeFilter.Value);
                model.ReportResult = await _reportService.GenerateAdoptionReportForType(
                    model.StartDate, model.EndDate, model.AnimalTypeFilter.Value);
                ViewBag.ReportTitle = $"Отчет по усыновлениям ({model.AnimalTypeFilter.Value})";
            }
            else
            {
                _logger.LogInformation("Generating general adoption report.");
                model.ReportResult = await _reportService.GenerateAdoptionReport(
                    model.StartDate, model.EndDate);
                ViewBag.ReportTitle = "Общий отчет по усыновлениям";
            }

            _logger.LogInformation("Report generated. Total Adoptions: {Total}, AdoptionsByType Count: {Count}",
                 model.ReportResult?.TotalAdoptions ?? 0, model.ReportResult?.AdoptionsByType?.Count ?? 0);

            ViewBag.ChartData = model.ReportResult?.AdoptionsByType;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating adoption report.");
            ModelState.AddModelError("", "Произошла ошибка при генерации отчета.");
            TempData["ErrorMessage"] = "Ошибка генерации отчета.";
            return View(model);
        }
    }

    private void LogModelStateErrors()
    {
        foreach (var state in ModelState)
        {
            if (state.Value.Errors.Any())
            {
                _logger.LogWarning("ModelState Error - Field: {FieldName}, Error: {ErrorMessage}",
                    state.Key, state.Value.Errors.First().ErrorMessage);
                if (state.Value.Errors.First().Exception != null)
                {
                    _logger.LogWarning("ModelState Error - Field: {FieldName}, Exception: {Exception}",
                       state.Key, state.Value.Errors.First().Exception.Message);
                }
            }
        }
    }
}