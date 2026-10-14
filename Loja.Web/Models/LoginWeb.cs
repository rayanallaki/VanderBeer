using System.ComponentModel.DataAnnotations;

namespace Loja.Web.Models;

public class LoginWeb
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;
}

public class UsuarioWeb
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}