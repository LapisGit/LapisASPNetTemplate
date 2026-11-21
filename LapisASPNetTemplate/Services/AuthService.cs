using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LapisASPNetTemplate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LapisASPNetTemplate.Services;


public class AuthService
{
    private readonly RsaSecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly DBContext _db;

    public AuthService(DBContext db)
    {
        _db = db;

        var rsa = Signatures.GetRsaInstance()
                  ?? throw new InvalidOperationException("jwt - sigs service was not found");

        _securityKey = new RsaSecurityKey(rsa) { KeyId = Program.keyId };

        _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.RsaSha256);
    }

    public string GenerateToken(string accountId, string platform = "N/A", string role = "client")
    {
        var now = DateTime.UtcNow;
        var exp = now.AddYears(999);

        var claims = new List<Claim>
        {
            new Claim("iss", "https://librerate.lapiscreates.tech"),
            new Claim("nbf", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString()),
            new Claim("iat", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString()),
            new Claim("exp", ((DateTimeOffset)exp).ToUnixTimeSeconds().ToString()),
            new Claim("aud", "https://librerate.lapiscreates.tech/resources"),
            new Claim("amr", "login"),
            new Claim("client_id", "lr_client"),
            new Claim("sub", accountId),
            new Claim("auth_time", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString()),
            new Claim("idp", "local"),
            new Claim("platform", platform),
            new Claim("ver", "1.0-beta"),
            new Claim("role", role),
            new Claim("jti", Guid.NewGuid().ToString("N")[..32].ToUpper())
        };

        claims.Add(new Claim("scope", "mpf.save"));
        claims.Add(new Claim("scope", "mpf.leaderboard"));
        claims.Add(new Claim("scope", "mpf.accounts"));
        claims.Add(new Claim("scope", "mpf.api"));
        claims.Add(new Claim("scope", "offline_access"));

        var identity = new ClaimsIdentity(claims, "jwt");

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            issuer: "https://librerate.lapiscreates.tech",
            audience: "https://librerate.lapiscreates.tech/resources",
            subject: identity,
            notBefore: now,
            issuedAt: now,
            expires: exp,
            signingCredentials: _signingCredentials);

        token.Payload["amr"] = new List<string> { "login" };

        return handler.WriteToken(token);
    }

    // invalidates all tokens issued for this account before now
    // TODO: make this run whenever a player changed account info (email, pass, username)
    public async Task ExpireTokensForAccount(int accountId)
    {
        var expiry = await _db.TokenExpiries.FindAsync(accountId);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (expiry == null)
        {
            _db.TokenExpiries.Add(new TokenExpiry { AccountId = accountId, ExpiredAtUnix = now });
        }
        else
        {
            expiry.ExpiredAtUnix = now;
            _db.TokenExpiries.Update(expiry);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsTokenExpired(ClaimsPrincipal principal)
    {
        var sub = principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var iat = principal.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;

        if (!int.TryParse(sub, out var accountId) || !long.TryParse(iat, out var issuedAtUnix))
            return true;

        var expiry = await _db.TokenExpiries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.AccountId == accountId);

        return expiry != null && issuedAtUnix < expiry.ExpiredAtUnix;
    }

    public async Task<string?> ValidateAndGetAccountId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey,
                ValidateIssuer = true,
                ValidIssuer = "https://librerate.lapiscreates.tech",
                ValidateAudience = true,
                ValidAudience = "https://librerate.lapiscreates.tech/resources",
                ValidateLifetime = true,
                RequireExpirationTime = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, parameters, out SecurityToken validatedToken);

            if (await IsTokenExpired(principal))
                return null;

            var accountIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub");

            return accountIdClaim?.Value;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<int?> GetAccountIdFromContext(HttpContext context)
    {
        try
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var token = authHeader.Substring("Bearer ".Length);
            var accountId = await ValidateAndGetAccountId(token);

            if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId, out var id))
                return null;

            return id;
        }
        catch
        {
            return null;
        }
    }
}