using System.ComponentModel.DataAnnotations;
using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.DTO;

/// <summary>Criação e edição de playlist (RF17 / RF20 / RN15).</summary>
public class PlaylistDTO
{
    [Required(ErrorMessage = "O nome da playlist é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome da playlist deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
    public string? Descricao { get; set; }
}

/// <summary>Usado para adicionar uma música a uma playlist ou à fila.</summary>
public class AdicionarMusicaDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe o IdMusica.")]
    public int IdMusica { get; set; }
}

/// <summary>
/// Nova ordem completa. Na playlist, envie os IdMusica; na fila, envie os IdFila.
/// A lista deve conter exatamente os itens atuais, na ordem desejada.
/// </summary>
public class OrdemDTO
{
    [Required(ErrorMessage = "Informe a nova ordem.")]
    [MinLength(1, ErrorMessage = "Informe a nova ordem.")]
    public List<int> Ids { get; set; } = new();
}

public record PlaylistResumoDTO(
    int IdPlaylist,
    string Nome,
    string? Descricao,
    DateTime DataCriacao,
    int QuantidadeMusicas,
    int DuracaoTotal)
{
    public static PlaylistResumoDTO Converter(Playlist playlist) => new(
        playlist.IdPlaylist,
        playlist.Nome,
        playlist.Descricao,
        playlist.DataCriacao,
        playlist.Musicas.Count,
        playlist.Musicas.Sum(pm => pm.Musica.Duracao));
}

public record PlaylistItemDTO(int Posicao, DateTime DataAdicao, MusicaRespostaDTO Musica);

public record PlaylistDetalheDTO(
    int IdPlaylist,
    string Nome,
    string? Descricao,
    DateTime DataCriacao,
    int QuantidadeMusicas,
    int DuracaoTotal,
    List<PlaylistItemDTO> Musicas)
{
    public static PlaylistDetalheDTO Converter(Playlist playlist) => new(
        playlist.IdPlaylist,
        playlist.Nome,
        playlist.Descricao,
        playlist.DataCriacao,
        playlist.Musicas.Count,
        playlist.Musicas.Sum(pm => pm.Musica.Duracao),
        playlist.Musicas
            .OrderBy(pm => pm.Posicao)
            .Select(pm => new PlaylistItemDTO(pm.Posicao, pm.DataAdicao, MusicaRespostaDTO.Converter(pm.Musica)))
            .ToList());
}
