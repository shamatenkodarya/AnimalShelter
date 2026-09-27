using DAL.Entities;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using BLL.DTO;

namespace WEB.Models
{
    public class AnimalViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
        [Display(Name = "Имя")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Тип животного обязателен")]
        [Display(Name = "Тип животного")]
        public AnimalType Type { get; set; }

        [Required(ErrorMessage = "Возраст обязателен")]
        [Range(0, 50, ErrorMessage = "Возраст должен быть от 0 до 50")]
        [Display(Name = "Возраст (лет)")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Пол обязателен")]
        [Display(Name = "Пол")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Вес обязателен")]
        [Range(0.1, 200, ErrorMessage = "Вес должен быть между 0.1 и 200 кг")]
        [Display(Name = "Вес (кг)")]
        [RegularExpression(@"^[0-9]+([.,][0-9]{1,2})?$", ErrorMessage = "Введите корректный вес (например, 5.2 или 10)")]
        public decimal Weight { get; set; }

        [Display(Name = "Фото")]
        public IFormFile PhotoFile { get; set; }

        public string ExistingPhotoBase64 { get; set; }

        public bool IsAdopted { get; set; }

        [Display(Name = "Дата поступления")]
        [DataType(DataType.Date)]
        public DateTime AdmissionDate { get; set; }


        [Display(Name = "Порода")]
        [StringLength(100)]
        public string Breed { get; set; }

        [Display(Name = "Окрас")]
        [StringLength(100)]
        public string Color { get; set; }

        [Display(Name = "Заметки о здоровье")]
        [DataType(DataType.MultilineText)]
        public string HealthNotes { get; set; }

        [Display(Name = "Описание поведения")]
        [DataType(DataType.MultilineText)]
        public string BehaviorDescription { get; set; }

        public List<VaccinationDTO> Vaccinations { get; set; } = new List<VaccinationDTO>();

        [Display(Name = "Название новой прививки")]
        public string NewVaccinationName { get; set; }
        [Display(Name = "Дата новой прививки")]
        [DataType(DataType.Date)]
        public DateTime? NewVaccinationDate { get; set; } = DateTime.Today;

    }
}