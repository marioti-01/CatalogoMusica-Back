using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Repositories;

public class ArtistaRepository : IArtistaRepository
{
    private readonly CatalogoMusicaContext _context;

    public ArtistaRepository(CatalogoMusicaContext context)
    {
        _context = context;
    }

    public async Task<List<Artista>> Listar(string? busca = null)
    {
        var query = _context.Artista.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(a => a.Nome.Contains(termo));
        }

        return await query.OrderBy(a => a.Nome).ToListAsync();
    }

    public async Task<Artista?> BuscarPorId(int id)
    {
        return await _context.Artista.AsNoTracking().FirstOrDefaultAsync(a => a.IdArtista == id);
    }

    public async Task<bool> Existe(int id)
    {
        return await _context.Artista.AnyAsync(a => a.IdArtista == id);
    }

    public async Task<bool> NomeEmUso(string nome, int? ignorarId = null)
    {
        return await _context.Artista.AnyAsync(a => a.Nome == nome && (ignorarId == null || a.IdArtista != ignorarId));
    }

    public async Task Cadastrar(Artista artista)
    {
        await _context.Artista.AddAsync(artista);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int id, string nome)
    {
        var artistaExistente = await _context.Artista.FindAsync(id);
        if (artistaExistente is null) return;

        artistaExistente.Nome = nome;
        await _context.SaveChangesAsync();
    }
}
