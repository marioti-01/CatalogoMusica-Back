namespace CatalogoMusica.WebAPI.Models;

public class PlaylistMusica
{
    public int IdPlaylist { get; set; }

    public int IdMusica { get; set; }

    public int Posicao { get; set; }

    public DateTime DataAdicao { get; set; }

    public virtual Playlist Playlist { get; set; } = null!;

    public virtual Musica Musica { get; set; } = null!;
}
