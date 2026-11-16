using System.ComponentModel.DataAnnotations;
using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.DTO;

/// <summary>Cadastro de usuário feito pelo administrador (RF13).</summary>
public class UsuarioCadastroDTO
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(50, ErrorMessage = "O e-mail deve ter no máximo 50 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 72 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de usuário é obrigatório.")]
    [RegularExpression("^(Admin|Comum)$", ErrorMessage = "O tipo de usuário deve ser 'Admin' ou 'Comum'.")]
    public string TipoUsuario { get; set; } = TiposUsuario.Comum;
}

/// <summary>Cadastro público (qualquer pessoa cria a própria conta). Sempre cria usuário Comum.</summary>
public class CadastroPublicoDTO
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(50, ErrorMessage = "O e-mail deve ter no máximo 50 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 72 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Edição de usuário feita pelo administrador (RF14). Senha em branco mantém a atual.</summary>
public class UsuarioAtualizacaoDTO
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(50, ErrorMessage = "O e-mail deve ter no máximo 50 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(72, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 72 caracteres.")]
    public string? Senha { get; set; }

    [Required(ErrorMessage = "O tipo de usuário é obrigatório.")]
    [RegularExpression("^(Admin|Comum)$", ErrorMessage = "O tipo de usuário deve ser 'Admin' ou 'Comum'.")]
    public string TipoUsuario { get; set; } = TiposUsuario.Comum;
}

/// <summary>
/// Edição do próprio perfil (RF07 / RN09). Para trocar a senha, informe a senha atual.
/// </summary>
public class PerfilAtualizacaoDTO : IValidatableObject
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(50, ErrorMessage = "O e-mail deve ter no máximo 50 caracteres.")]
    public string Email { get; set; } = string.Empty;

    public string? SenhaAtual { get; set; }

    [StringLength(72, MinimumLength = 8, ErrorMessage = "A nova senha deve ter entre 8 e 72 caracteres.")]
    public string? NovaSenha { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(NovaSenha) && string.IsNullOrEmpty(SenhaAtual))
            yield return new ValidationResult(
                "Informe a senha atual para definir uma nova senha.",
                new[] { nameof(SenhaAtual) });
    }
}

public record UsuarioRespostaDTO(int IdUsuario, string Nome, string Email, string TipoUsuario, bool Ativo)
{
    public static UsuarioRespostaDTO Converter(Usuario usuario) => new(
        usuario.IdUsuario,
        usuario.Nome,
        usuario.Email,
        usuario.TipoUsuario,
        usuario.Ativo);
}
