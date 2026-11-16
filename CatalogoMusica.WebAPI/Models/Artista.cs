namespace CatalogoMusica.WebAPI.Models;

public class Artista
{
    public int IdArtista { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
