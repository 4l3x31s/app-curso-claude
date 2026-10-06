using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    // Almacenamiento en memoria por proceso: cada instancia de la app (y cada test) tiene sus propios datos,
    // así que sesiones en paralelo no comparten ni pisan una base de datos.
    public class InMemoryCuentaRepository : ICuentaRepository
    {
        private readonly List<Cuenta> _cuentas = new();
        private readonly Lock _lock = new();
        private int _nextId = 1;

        public Cuenta Add(string titular, decimal saldoInicial)
        {
            lock (_lock)
            {
                var cuenta = new Cuenta
                {
                    Id = _nextId++,
                    Titular = titular,
                    Saldo = saldoInicial,
                    FechaAlta = DateTime.Now
                };
                _cuentas.Add(cuenta);
                return cuenta;
            }
        }
    }
}
