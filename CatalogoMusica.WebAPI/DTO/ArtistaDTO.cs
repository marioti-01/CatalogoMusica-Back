using System.ComponentModel.DataAnnotations;
using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.DTO;

public class ArtistaDTO
{
    [Required(ErrorMessage = "O nome do artista é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome do artista deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}

public record ArtistaRespostaDTO(int IdArtista, string Nome)
{
    public static ArtistaRespostaDTO Converter(Artista artista) => new(artista.IdArtista, artista.Nome);
}
