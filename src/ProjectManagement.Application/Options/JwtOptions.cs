using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.Options;

/// <summary>Signing settings of the issued JWT, bound from the "Jwt" configuration section.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key; must be at least 32 characters for HMAC-SHA256.</summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    [Range(1, 720)] public int ExpiresHours { get; set; } = 8;
}
