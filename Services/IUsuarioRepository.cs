using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    public interface IUsuarioRepository
    {
        IReadOnlyList<Usuario> GetAll();
        bool ExisteEmail(string email);
        void Add(Usuario usuario);
    }
}
