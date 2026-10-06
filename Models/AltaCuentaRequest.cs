using System.ComponentModel.DataAnnotations;

namespace app_curso_claude.Models
{
    public class AltaCuentaRequest
    {
        [Required(ErrorMessage = "El titular es obligatorio.")]
        [StringLength(100, ErrorMessage = "El titular no puede superar los 100 caracteres.")]
        public string Titular { get; set; } = string.Empty;

        [Range(0, 1_000_000_000, ErrorMessage = "El saldo inicial debe estar entre 0 y 1.000.000.000.")]
        public decimal SaldoInicial { get; set; }
    }
}
