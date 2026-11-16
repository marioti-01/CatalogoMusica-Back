namespace CatalogoMusica.WebAPI.Models;

public static class TiposUsuario
{
    public const string Admin = "Admin";
    public const string Comum = "Comum";
}

public class Usuario
{
    public int IdUsuario { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Senha { get; set; } = null!;

    public string TipoUsuario { get; set; } = TiposUsuario.Comum;

    public bool Ativo { get; set; } = true;

    public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();

    public virtual ICollection<FilaReproducao> Fila { get; set; } = new List<FilaReproducao>();
}
