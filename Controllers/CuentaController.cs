using app_curso_claude.Models;
using app_curso_claude.Services;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ICuentaRepository _repository;

        public CuentaController(ICuentaRepository repository)
        {
            _repository = repository;
        }

        // POST /Cuenta/Alta
        [HttpPost]
        public IActionResult Alta([FromBody] AltaCuentaRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var cuenta = _repository.Add(request.Titular.Trim(), request.SaldoInicial);
            return StatusCode(StatusCodes.Status201Created, cuenta);
        }
    }
}
