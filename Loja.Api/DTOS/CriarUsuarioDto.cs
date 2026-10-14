using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class CriarUsuarioDto
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu documento.")]
    [StringLength(20)]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a rua.")]
    [StringLength(100)]
    public string Rua { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o bairro.")]
    [StringLength(60)]
    public string Bairro { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CEP.")]
    [RegularExpression(
        @"^\d{5}-?\d{3}$",
        ErrorMessage = "Informe o CEP com 8 números, com ou sem hífen.")]
    public string Cep { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o número do endereço.")]
    [StringLength(10)]
    public string Numero { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Complemento { get; set; }

    [Required(ErrorMessage = "Informe seu telefone.")]
    [StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma senha.")]
    [StringLength(
        128,
        MinimumLength = 8,
        ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}