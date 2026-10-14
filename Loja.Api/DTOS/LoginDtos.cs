using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [StringLength(128)]
    public string Senha { get; set; } = string.Empty;
}