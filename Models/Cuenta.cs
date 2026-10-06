namespace app_curso_claude.Models
{
    public class Cuenta
    {
        public int Id { get; set; }

        public string Titular { get; set; } = string.Empty;

        public decimal Saldo { get; set; }

        public DateTime FechaAlta { get; set; }
    }
}
