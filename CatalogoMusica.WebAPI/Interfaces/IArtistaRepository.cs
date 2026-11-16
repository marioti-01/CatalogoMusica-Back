using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.Interfaces;

public interface IArtistaRepository
{
    Task<List<Artista>> Listar(string? busca = null);
    Task<Artista?> BuscarPorId(int id);
    Task<bool> Existe(int id);
    Task<bool> NomeEmUso(string nome, int? ignorarId = null);
    Task Cadastrar(Artista artista);
    Task Atualizar(int id, string nome);
}
