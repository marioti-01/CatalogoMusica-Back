using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.Interfaces;

public interface IGeneroRepository
{
    Task<List<Genero>> Listar();
    Task<Genero?> BuscarPorId(int id);
    Task<List<Genero>> BuscarPorIds(IReadOnlyCollection<int> ids);
    Task<bool> NomeEmUso(string nome, int? ignorarId = null);
    Task Cadastrar(Genero genero);
    Task Atualizar(int id, string nome);
}
