namespace CatalogoMusica.WebAPI.Models;

public class Genero
{
    public int IdGenero { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
