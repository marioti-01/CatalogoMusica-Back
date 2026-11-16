namespace CatalogoMusica.WebAPI.Models;

public class Playlist
{
    public int IdPlaylist { get; set; }

    public int IdUsuario { get; set; }

    public string Nome { get; set; } = null!;

    public string? Descricao { get; set; }

    public DateTime DataCriacao { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<PlaylistMusica> Musicas { get; set; } = new List<PlaylistMusica>();
}
