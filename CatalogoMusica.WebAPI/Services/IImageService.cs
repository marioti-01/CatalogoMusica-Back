namespace CatalogoMusica.WebAPI.Services;

/// <summary>Upload e remoção das imagens de capa das músicas.</summary>
public interface IImageService
{
    bool IsConfigured { get; }
    Task<string> UploadImageAsync(IFormFile arquivo);
    Task<bool> DeleteImageAsync(string imageUrl);
}
