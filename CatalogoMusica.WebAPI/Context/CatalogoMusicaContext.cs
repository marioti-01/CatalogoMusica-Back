using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Context;

/// <summary>
/// Contexto do banco CatalogoMusica. O mapeamento segue o script CatalogoMusica.sql
/// (mesmos nomes de tabelas, chaves, índices e regras de exclusão em cascata).
/// A connection string vem do appsettings.json (ConnectionStrings:DefaultConnection).
/// </summary>
public class CatalogoMusicaContext : DbContext
{
    public CatalogoMusicaContext(DbContextOptions<CatalogoMusicaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Usuario> Usuario { get; set; }
    public virtual DbSet<Artista> Artista { get; set; }
    public virtual DbSet<Genero> Genero { get; set; }
    public virtual DbSet<Musica> Musica { get; set; }
    public virtual DbSet<Playlist> Playlist { get; set; }
    public virtual DbSet<PlaylistMusica> PlaylistMusica { get; set; }
    public virtual DbSet<FilaReproducao> FilaReproducao { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasKey(e => e.IdUsuario).HasName("PK_Usuario");

            entity.Property(e => e.Nome).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Senha).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.TipoUsuario).HasMaxLength(20).IsUnicode(false);

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_Usuario_Email");
        });

        modelBuilder.Entity<Artista>(entity =>
        {
            entity.ToTable("Artista");
            entity.HasKey(e => e.IdArtista).HasName("PK_Artista");

            entity.Property(e => e.Nome).HasMaxLength(100).IsUnicode(false);

            entity.HasIndex(e => e.Nome).IsUnique().HasDatabaseName("UQ_Artista_Nome");
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.ToTable("Genero");
            entity.HasKey(e => e.IdGenero).HasName("PK_Genero");

            entity.Property(e => e.Nome).HasMaxLength(50).IsUnicode(false);

            entity.HasIndex(e => e.Nome).IsUnique().HasDatabaseName("UQ_Genero_Nome");
        });

        modelBuilder.Entity<Musica>(entity =>
        {
            entity.ToTable("Musica");
            entity.HasKey(e => e.IdMusica).HasName("PK_Musica");

            entity.Property(e => e.Titulo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Album).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ImagemCapa).IsUnicode(false);

            entity.HasIndex(e => e.Titulo).HasDatabaseName("IX_Musica_Titulo");
            entity.HasIndex(e => e.IdArtista).HasDatabaseName("IX_Musica_IdArtista");

            entity.HasOne(e => e.Artista)
                .WithMany(a => a.Musicas)
                .HasForeignKey(e => e.IdArtista)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Musica_Artista");

            entity.HasMany(e => e.Generos)
                .WithMany(g => g.Musicas)
                .UsingEntity<Dictionary<string, object>>(
                    "MusicaGenero",
                    r => r.HasOne<Genero>().WithMany()
                        .HasForeignKey("IdGenero")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_MusicaGenero_Genero"),
                    l => l.HasOne<Musica>().WithMany()
                        .HasForeignKey("IdMusica")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("FK_MusicaGenero_Musica"),
                    j =>
                    {
                        j.ToTable("MusicaGenero");
                        j.HasKey("IdGenero", "IdMusica").HasName("PK_MusicaGenero");
                        j.HasIndex("IdMusica").HasDatabaseName("IX_MusicaGenero_IdMusica");
                    });
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.ToTable("Playlist");
            entity.HasKey(e => e.IdPlaylist).HasName("PK_Playlist");

            entity.Property(e => e.Nome).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Descricao).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.DataCriacao).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(e => new { e.IdUsuario, e.Nome }).IsUnique().HasDatabaseName("UQ_Playlist_Usuario_Nome");
            entity.HasIndex(e => e.IdUsuario).HasDatabaseName("IX_Playlist_IdUsuario");

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Playlists)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Playlist_Usuario");
        });

        modelBuilder.Entity<PlaylistMusica>(entity =>
        {
            entity.ToTable("PlaylistMusica");
            entity.HasKey(e => new { e.IdPlaylist, e.IdMusica }).HasName("PK_PlaylistMusica");

            entity.Property(e => e.DataAdicao).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(e => new { e.IdPlaylist, e.Posicao }).IsUnique().HasDatabaseName("UQ_PlaylistMusica_Posicao");
            entity.HasIndex(e => e.IdMusica).HasDatabaseName("IX_PlaylistMusica_IdMusica");

            entity.HasOne(e => e.Playlist)
                .WithMany(p => p.Musicas)
                .HasForeignKey(e => e.IdPlaylist)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PlaylistMusica_Playlist");

            entity.HasOne(e => e.Musica)
                .WithMany(m => m.Playlists)
                .HasForeignKey(e => e.IdMusica)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PlaylistMusica_Musica");
        });

        modelBuilder.Entity<FilaReproducao>(entity =>
        {
            entity.ToTable("FilaReproducao");
            entity.HasKey(e => e.IdFila).HasName("PK_FilaReproducao");

            entity.Property(e => e.DataAdicao).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(e => new { e.IdUsuario, e.Posicao }).IsUnique().HasDatabaseName("UQ_FilaReproducao_Posicao");
            entity.HasIndex(e => e.IdMusica).HasDatabaseName("IX_FilaReproducao_IdMusica");

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Fila)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_FilaReproducao_Usuario");

            entity.HasOne(e => e.Musica)
                .WithMany(m => m.Filas)
                .HasForeignKey(e => e.IdMusica)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_FilaReproducao_Musica");
        });
    }
}
