using Microsoft.Extensions.Caching.Memory;

namespace CatalogoMusica.WebAPI.Services;

/// <summary>
/// Guarda os tokens encerrados pelo logout (RF02) até eles expirarem.
/// A lista fica em memória: se a API reiniciar, ela é perdida, mas os tokens
/// continuam expirando normalmente pelo tempo configurado em Jwt:ExpirationMinutes.
/// </summary>
public interface ITokenRevogadoService
{
    void Revogar(string jti, DateTimeOffset expiracao);
    bool EstaRevogado(string jti);
}

public class TokenRevogadoService : ITokenRevogadoService
{
    private const string Prefixo = "token-revogado:";
    private readonly IMemoryCache _cache;

    public TokenRevogadoService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Revogar(string jti, DateTimeOffset expiracao)
    {
        if (expiracao > DateTimeOffset.UtcNow)
            _cache.Set(Prefixo + jti, true, expiracao);
    }

    public bool EstaRevogado(string jti) => _cache.TryGetValue(Prefixo + jti, out _);
}
