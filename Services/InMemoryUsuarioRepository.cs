using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    // Almacenamiento en memoria: los datos se pierden al reiniciar la aplicación.
    public class InMemoryUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = new();
        private readonly Lock _lock = new();
        private int _nextId = 1;

        public IReadOnlyList<Usuario> GetAll()
        {
            lock (_lock)
            {
                return _usuarios.ToList();
            }
        }

        public bool ExisteEmail(string email)
        {
            lock (_lock)
            {
                return _usuarios.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Add(Usuario usuario)
        {
            lock (_lock)
            {
                usuario.Id = _nextId++;
                usuario.FechaAlta = DateTime.Now;
                _usuarios.Add(usuario);
            }
        }
    }
}
