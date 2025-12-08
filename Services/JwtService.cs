using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace PalpitheionApi.Services;

public class JwtService(IConfiguration configuration, ILogger<JwtService> logger)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JwtService> _logger = logger;

    /// <summary>
    /// Retorna as claims básicas do usuário + roles (se fornecidas).
    /// </summary>
    public IEnumerable<Claim> GetClaims(IdentityUser user, IEnumerable<string>? roles = null)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (roles is not null)
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

        return claims;
    }

    /// <summary>
    /// Gera um token JWT usando as roles já conhecidas.
    /// Chave/Issuer/Audience/ExpireMinutes devem estar configurados em "Jwt" no IConfiguration.
    /// </summary>
    public string GenerateToken(IdentityUser user, IEnumerable<string>? roles = null)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var keyString = _configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(keyString))
            throw new InvalidOperationException("Configuração inválida: 'JwtSettings:Secret' não encontrada.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireDays = 1;
        if (!string.IsNullOrWhiteSpace(_configuration["JwtSettings:ExpirationDays"]) &&
            int.TryParse(_configuration["JwtSettings:ExpirationDays"], out var parsed))
        {
            expireDays = parsed;
        }

        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];

        var claims = GetClaims(user, roles);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Recupera as roles do UserManager e gera o token de forma assíncrona.
    /// </summary>
    public async Task<string> GenerateTokenAsync(IdentityUser user, UserManager<IdentityUser> userManager)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (userManager is null) throw new ArgumentNullException(nameof(userManager));

        var roles = await userManager.GetRolesAsync(user);
        return GenerateToken(user, roles);
    }
}