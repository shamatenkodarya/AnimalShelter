using System;
using System.ComponentModel.DataAnnotations;          

namespace BLL.DTO
{
    public class ReviewDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст отзыва не может быть пустым")]        
        [Display(Name = "Текст отзыва")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public int AnimalId { get; set; }

        [Display(Name = "Животное")]
        public string AnimalName { get; set; }

        public string UserId { get; set; }
        [Display(Name = "Автор")]
        public string UserName { get; set; }
    }
}