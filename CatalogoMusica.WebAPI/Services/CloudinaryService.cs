using System.Text.RegularExpressions;
using CatalogoMusica.WebAPI.Utils;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace CatalogoMusica.WebAPI.Services;

public class CloudinaryService : IImageService
{
    private const string PastaCapas = "catalogo-musica/capas";
    private const long TamanhoMaximo = 10 * 1024 * 1024;

    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IOptions<CloudinarySettings> options, ILogger<CloudinaryService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    private Cloudinary Cliente => new(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret));

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.CloudName)
        && !string.IsNullOrWhiteSpace(_settings.ApiKey)
        && !string.IsNullOrWhiteSpace(_settings.ApiSecret);

    public async Task<string> UploadImageAsync(IFormFile arquivo)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Configure as credenciais do Cloudinary.");
        if (arquivo is null || arquivo.Length is <= 0 or > TamanhoMaximo)
            throw new ArgumentException("A imagem deve ter entre 1 byte e 10 MB.", nameof(arquivo));
        if (Path.GetExtension(arquivo.FileName).ToLowerInvariant() is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
            throw new ArgumentException("Use imagens JPG, PNG, WEBP ou GIF.", nameof(arquivo));

        using var dados = arquivo.OpenReadStream();

        ImageUploadResult resultado;
        try
        {
            resultado = await Cliente.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(arquivo.FileName, dados),
                Folder = PastaCapas,
                PublicId = Guid.NewGuid().ToString("N"),
                Overwrite = false
            });
        }
        catch (Exception erro)
        {
            throw new HttpRequestException("Falha ao comunicar com o Cloudinary.", erro);
        }

        return resultado.Error is null && resultado.SecureUrl is not null
            ? resultado.SecureUrl.ToString()
            : throw new HttpRequestException("O Cloudinary não conseguiu salvar a imagem.");
    }

    /// <summary>
    /// Remove a imagem apenas se ela foi enviada por esta API (pasta catalogo-musica/capas).
    /// URLs externas informadas manualmente são ignoradas.
    /// </summary>
    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        if (!IsConfigured
            || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var url)
            || url.Scheme != Uri.UriSchemeHttps
            || !string.Equals(url.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var match = Regex.Match(url.AbsolutePath,
            $@"^/(?i:{Regex.Escape(_settings.CloudName)})/image/upload/(?:v[0-9]+/)?({Regex.Escape(PastaCapas)}/[0-9a-fA-F]{{32}})\.[^/]+$");
        if (!match.Success) return false;

        var publicId = match.Groups[1].Value;

        try
        {
            var resultado = await Cliente.DestroyAsync(new DeletionParams(publicId) { Invalidate = true });
            return resultado.Result == "ok";
        }
        catch (Exception erro)
        {
            _logger.LogWarning(erro, "Não foi possível remover a imagem {PublicId}.", publicId);
            return false;
        }
    }
}
