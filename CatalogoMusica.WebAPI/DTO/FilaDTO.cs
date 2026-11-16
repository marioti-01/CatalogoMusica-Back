using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.DTO;

/// <summary>Item da fila. O item na posição 1 é a música que está tocando.</summary>
public record FilaItemDTO(int IdFila, int Posicao, DateTime DataAdicao, MusicaRespostaDTO Musica)
{
    public static FilaItemDTO Converter(FilaReproducao item) => new(
        item.IdFila,
        item.Posicao,
        item.DataAdicao,
        MusicaRespostaDTO.Converter(item.Musica));
}
