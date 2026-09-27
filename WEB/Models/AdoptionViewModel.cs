using System;
using System.ComponentModel.DataAnnotations;
using BLL.DTO;   

namespace WEB.Models
{
    public class AdoptionViewModel
    {
        [Required]
        public int AnimalId { get; set; }

        public AnimalDTO Animal { get; set; }      

        [Required(ErrorMessage = "ФИО усыновителя обязательно")]
        [Display(Name = "ФИО усыновителя")]
        [StringLength(150)]
        public string AdopterFullName { get; set; }

        [Required(ErrorMessage = "Контактные данные обязательны")]
        [Display(Name = "Контактные данные (телефон/email)")]
        [StringLength(200)]
        public string AdopterContacts { get; set; }

        [Required(ErrorMessage = "Адрес обязателен")]
        [Display(Name = "Адрес")]
        [StringLength(250)]
        public string Address { get; set; }

        [Display(Name = "Дата усыновления")]
        [DataType(DataType.Date)]
        public DateTime AdoptionDate { get; set; } = DateTime.Today;
    }
}