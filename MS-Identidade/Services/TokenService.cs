using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MS_Identidade.DTOs.Auth;
using MS_Identidade.Entities;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponseDTO GerarToken(
        Usuario usuario,
        long empresaId)
    {
        var chave = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chave JWT não configurada.");

        var expiracao = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:ExpiracaoMinutos"));

        var claims = new[]
        {
            new Claim("usuario_id", usuario.Id.ToString()),
            new Claim("empresa_id", empresaId.ToString()),
            new Claim("nome", usuario.Nome),
            new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(chave));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiracao,
            signingCredentials: credentials);

        return new LoginResponseDTO
        {
            UsuarioId = usuario.Id,
            EmpresaId = empresaId,
            Nome = usuario.Nome,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiraEm = expiracao
        };
    }
}