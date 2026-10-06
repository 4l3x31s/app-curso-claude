using System.ComponentModel.DataAnnotations;

namespace app_curso_claude.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
        [StringLength(256, ErrorMessage = "El email no puede superar los 256 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Fecha de alta")]
        public DateTime FechaAlta { get; set; }
    }
}
