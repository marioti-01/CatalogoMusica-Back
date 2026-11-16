using System.ComponentModel.DataAnnotations;
using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.DTO;

/// <summary>
/// Cadastro e edição de música (multipart/form-data, por causa do arquivo da capa).
/// Regras: RN05 (pelo menos um gênero), RN06 (obrigatórios), RN07 (duração &gt; 0), RN08 (data).
/// </summary>
public class MusicaDTO : IValidatableObject
{
    public static readonly DateOnly DataMinima = new(1860, 1, 1);

    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O artista é obrigatório.")]
    public int? IdArtista { get; set; }

    [StringLength(100, ErrorMessage = "O álbum deve ter no máximo 100 caracteres.")]
    public string? Album { get; set; }

    [Required(ErrorMessage = "A data de lançamento é obrigatória.")]
    public DateOnly? DataLancamento { get; set; }

    /// <summary>Duração em segundos.</summary>
    [Required(ErrorMessage = "A duração é obrigatória.")]
    [Range(1, int.MaxValue, ErrorMessage = "A duração deve ser maior que zero (em segundos).")]
    public int? Duracao { get; set; }

    /// <summary>URL de uma imagem já hospedada. Use isto OU ArquivoImagem.</summary>
    [Url(ErrorMessage = "Informe uma URL válida para a capa.")]
    public string? ImagemCapa { get; set; }

    /// <summary>Arquivo de imagem enviado para o Cloudinary. Use isto OU ImagemCapa.</summary>
    public IFormFile? ArquivoImagem { get; set; }

    /// <summary>Na edição: true remove a capa atual.</summary>
    public bool RemoverCapa { get; set; }

    [Required(ErrorMessage = "Informe pelo menos um gênero.")]
    [MinLength(1, ErrorMessage = "Informe pelo menos um gênero.")]
    public List<int> IdGeneros { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ArquivoImagem is not null && !string.IsNullOrWhiteSpace(ImagemCapa))
            yield return new ValidationResult(
                "Envie ArquivoImagem ou ImagemCapa, não os dois.",
                new[] { nameof(ArquivoImagem), nameof(ImagemCapa) });

        if (RemoverCapa && (ArquivoImagem is not null || !string.IsNullOrWhiteSpace(ImagemCapa)))
            yield return new ValidationResult(
                "Não envie uma nova capa junto com RemoverCapa.",
                new[] { nameof(RemoverCapa) });

        if (DataLancamento is { } data)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            if (data < DataMinima || data > hoje)
                yield return new ValidationResult(
                    "A data de lançamento deve estar entre 01/01/1860 e a data de hoje.",
                    new[] { nameof(DataLancamento) });
        }
    }
}

public record MusicaRespostaDTO(
    int IdMusica,
    string Titulo,
    ArtistaRespostaDTO Artista,
    string? Album,
    DateOnly DataLancamento,
    int Duracao,
    string? ImagemCapa,
    List<GeneroRespostaDTO> Generos)
{
    public static MusicaRespostaDTO Converter(Musica musica) => new(
        musica.IdMusica,
        musica.Titulo,
        ArtistaRespostaDTO.Converter(musica.Artista),
        musica.Album,
        musica.DataLancamento,
        musica.Duracao,
        musica.ImagemCapa,
        musica.Generos.OrderBy(g => g.Nome).Select(GeneroRespostaDTO.Converter).ToList());
}

public record PaginaDTO<T>(List<T> Items, int Total, int Pagina, int TamanhoPagina);
