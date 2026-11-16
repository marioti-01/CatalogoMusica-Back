namespace CatalogoMusica.WebAPI.Models;

public class FilaReproducao
{
    public int IdFila { get; set; }

    public int IdUsuario { get; set; }

    public int IdMusica { get; set; }

    public int Posicao { get; set; }

    public DateTime DataAdicao { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Musica Musica { get; set; } = null!;
}
