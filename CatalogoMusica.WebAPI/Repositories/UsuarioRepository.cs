using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly CatalogoMusicaContext _context;

    public UsuarioRepository(CatalogoMusicaContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> Listar()
    {
        return await _context.Usuario.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();
    }

    public async Task<Usuario?> BuscarPorId(int id)
    {
        return await _context.Usuario.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
    }

    public async Task<Usuario?> BuscarPorEmailESenha(string email, string senha)
    {
        var usuarioBuscado = await _context.Usuario.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (usuarioBuscado is null) return null;

        return Criptografia.CompararHash(senha, usuarioBuscado.Senha) ? usuarioBuscado : null;
    }

    public async Task<bool> EmailEmUso(string email, int? ignorarId = null)
    {
        return await _context.Usuario.AnyAsync(u => u.Email == email && (ignorarId == null || u.IdUsuario != ignorarId));
    }

    public async Task Cadastrar(Usuario usuario)
    {
        usuario.Senha = Criptografia.GerarHash(usuario.Senha);

        await _context.Usuario.AddAsync(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int id, Usuario usuario)
    {
        var usuarioExistente = await _context.Usuario.FindAsync(id);
        if (usuarioExistente is null) return;

        usuarioExistente.Nome = usuario.Nome;
        usuarioExistente.Email = usuario.Email;
        usuarioExistente.TipoUsuario = usuario.TipoUsuario;
        usuarioExistente.Ativo = usuario.Ativo;

        if (!string.IsNullOrEmpty(usuario.Senha))
            usuarioExistente.Senha = Criptografia.GerarHash(usuario.Senha);

        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatus(int id, bool ativo)
    {
        await _context.Usuario
            .Where(u => u.IdUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Ativo, ativo));
    }
}
