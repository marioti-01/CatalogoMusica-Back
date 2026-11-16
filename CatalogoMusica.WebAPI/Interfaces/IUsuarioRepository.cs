using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.Interfaces;

public interface IUsuarioRepository
{
    Task<List<Usuario>> Listar();
    Task<Usuario?> BuscarPorId(int id);
    Task<Usuario?> BuscarPorEmailESenha(string email, string senha);
    Task<bool> EmailEmUso(string email, int? ignorarId = null);
    Task Cadastrar(Usuario usuario);

    /// <summary>Atualiza nome, e-mail, tipo e status. Senha vazia mantém a senha atual.</summary>
    Task Atualizar(int id, Usuario usuario);

    Task AlterarStatus(int id, bool ativo);
}
