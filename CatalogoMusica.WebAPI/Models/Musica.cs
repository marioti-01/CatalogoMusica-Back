namespace CatalogoMusica.WebAPI.Models;

public class Musica
{
    public int IdMusica { get; set; }

    public string Titulo { get; set; } = null!;

    public int IdArtista { get; set; }

    public string? Album { get; set; }

    public DateOnly DataLancamento { get; set; }

    /// <summary>Duração em segundos.</summary>
    public int Duracao { get; set; }

    public string? ImagemCapa { get; set; }

    public virtual Artista Artista { get; set; } = null!;

    /// <summary>Gêneros da música (tabela MusicaGenero).</summary>
    public virtual ICollection<Genero> Generos { get; set; } = new List<Genero>();

    public virtual ICollection<PlaylistMusica> Playlists { get; set; } = new List<PlaylistMusica>();

    public virtual ICollection<FilaReproducao> Filas { get; set; } = new List<FilaReproducao>();
}
