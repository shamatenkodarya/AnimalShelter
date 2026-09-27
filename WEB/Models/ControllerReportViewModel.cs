using System;
using System.ComponentModel.DataAnnotations;
using DAL.Entities;
using BLL.DTO;

namespace WEB.Models
{
    public class ControllerReportViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Начальная дата")]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Конечная дата")]
        public DateTime EndDate { get; set; } = DateTime.Now;

        [Display(Name = "Тип животного (для спец. отчета)")]
        public AnimalType? AnimalTypeFilter { get; set; }

        public AdoptionReportDTO ReportResult { get; set; }
    }
}