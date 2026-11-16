using System.ComponentModel.DataAnnotations;
using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.DTO;

public class GeneroDTO
{
    [Required(ErrorMessage = "O nome do gênero é obrigatório.")]
    [StringLength(50, ErrorMessage = "O nome do gênero deve ter no máximo 50 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}

public record GeneroRespostaDTO(int IdGenero, string Nome)
{
    public static GeneroRespostaDTO Converter(Genero genero) => new(genero.IdGenero, genero.Nome);
}
