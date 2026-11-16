using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Utils;

/// <summary>
/// O script SQL cria o usuário 'admin@catalogo.com' com a senha vazia, e senha vazia
/// nunca passa no login. Ao iniciar, a API grava o hash da senha definida em
/// AdminInicial:Senha (appsettings.json) para todo administrador que ainda estiver sem senha.
/// Depois do primeiro login, troque a senha pelo endpoint PUT /api/Usuario/perfil.
/// </summary>
public static class AdminInicial
{
    public static async Task ConfigurarSenhaAsync(WebApplication app)
    {
        var senha = app.Configuration["AdminInicial:Senha"];
        if (string.IsNullOrWhiteSpace(senha)) return;

        if (senha.Length < 8)
        {
            app.Logger.LogWarning("AdminInicial:Senha precisa ter pelo menos 8 caracteres. Senha do admin não configurada.");
            return;
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var contexto = scope.ServiceProvider.GetRequiredService<CatalogoMusicaContext>();

            var adminsSemSenha = await contexto.Usuario
                .Where(u => u.TipoUsuario == TiposUsuario.Admin && u.Senha == "")
                .ToListAsync();

            if (adminsSemSenha.Count == 0) return;

            foreach (var admin in adminsSemSenha)
                admin.Senha = Criptografia.GerarHash(senha);

            await contexto.SaveChangesAsync();
            app.Logger.LogInformation("Senha inicial definida para {Quantidade} administrador(es).", adminsSemSenha.Count);
        }
        catch (Exception erro)
        {
            app.Logger.LogWarning(erro, "Não foi possível verificar a senha do administrador inicial. Confira a connection string.");
        }
    }
}
