using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Utils;

public static class SqlErros
{
    /// <summary>
    /// True quando o SQL Server recusou o comando por violar uma UNIQUE ou PRIMARY KEY
    /// (erros 2601 e 2627). Usado para devolver 409 Conflict em vez de 500.
    /// </summary>
    public static bool ViolouUnicidade(DbUpdateException erro) =>
        erro.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
