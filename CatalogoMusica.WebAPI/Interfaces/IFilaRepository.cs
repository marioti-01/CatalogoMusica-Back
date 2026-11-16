using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.Interfaces;

/// <summary>Fila de reprodução individual de cada usuário (RN19).</summary>
public interface IFilaRepository
{
    Task<List<FilaReproducao>> Listar(int idUsuario);

    /// <summary>Adiciona as músicas ao final da fila, na ordem recebida. Repetições são permitidas (RN20).</summary>
    Task Adicionar(int idUsuario, IReadOnlyList<int> idsMusicas);

    Task<bool> Remover(int idUsuario, int idFila);
    Task Limpar(int idUsuario);

    /// <summary>Retorna false se a lista não tiver exatamente os itens atuais da fila.</summary>
    Task<bool> Reordenar(int idUsuario, IReadOnlyList<int> idsFila);

    /// <summary>Remove a música atual (posição 1) e sobe as demais (RN21). False se a fila estiver vazia.</summary>
    Task<bool> Avancar(int idUsuario);
}
