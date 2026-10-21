using System.ComponentModel.DataAnnotations;

namespace IRoute.Api.Contracts;

/// <summary>
/// Credenciales para iniciar sesión.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Usuario registrado.
    /// </summary>
    /// <example>admin</example>
    [Required]
    [StringLength(100)]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    /// <example>Admin123!</example>
    [Required]
    [StringLength(200)]
    public string Password { get; init; } = string.Empty;
}
