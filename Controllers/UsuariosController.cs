using app_curso_claude.Models;
using app_curso_claude.Services;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IUsuarioRepository _repository;

        public UsuariosController(IUsuarioRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View(_repository.GetAll());
        }

        public IActionResult Create()
        {
            return View(new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Nombre,Apellido,Email")] Usuario usuario)
        {
            if (ModelState.IsValid && _repository.ExisteEmail(usuario.Email))
            {
                ModelState.AddModelError(nameof(Usuario.Email), "Ya existe un usuario con ese email.");
            }

            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            _repository.Add(usuario);
            TempData["Mensaje"] = $"Usuario {usuario.Nombre} {usuario.Apellido} dado de alta correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
