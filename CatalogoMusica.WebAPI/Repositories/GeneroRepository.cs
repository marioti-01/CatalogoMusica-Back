using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Repositories;

public class GeneroRepository : IGeneroRepository
{
    private readonly CatalogoMusicaContext _context;

    public GeneroRepository(CatalogoMusicaContext context)
    {
        _context = context;
    }

    public async Task<List<Genero>> Listar()
    {
        return await _context.Genero.AsNoTracking().OrderBy(g => g.Nome).ToListAsync();
    }

    public async Task<Genero?> BuscarPorId(int id)
    {
        return await _context.Genero.AsNoTracking().FirstOrDefaultAsync(g => g.IdGenero == id);
    }

    /// <summary>Retorna os gêneros rastreados pelo contexto, prontos para vincular a uma música.</summary>
    public async Task<List<Genero>> BuscarPorIds(IReadOnlyCollection<int> ids)
    {
        var idsArray = ids.ToArray();
        return await _context.Genero.Where(g => idsArray.Contains(g.IdGenero)).ToListAsync();
    }

    public async Task<bool> NomeEmUso(string nome, int? ignorarId = null)
    {
        return await _context.Genero.AnyAsync(g => g.Nome == nome && (ignorarId == null || g.IdGenero != ignorarId));
    }

    public async Task Cadastrar(Genero genero)
    {
        await _context.Genero.AddAsync(genero);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int id, string nome)
    {
        var generoExistente = await _context.Genero.FindAsync(id);
        if (generoExistente is null) return;

        generoExistente.Nome = nome;
        await _context.SaveChangesAsync();
    }
}
