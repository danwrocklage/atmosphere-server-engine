using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AGame.Core.Identity;

/// <inheritdoc />
[Log(Category = "Jwt")]
internal class JwtService : Jwt
{
    public JwtService(IConfiguration configuration, ILogger<JwtService> logger) : 
        base(configuration.Get(() => JwtConfig.Default), logger.ToLogger<Jwt>())
    {
    }
}

/// <inheritdoc />
[Log(Category = "Jwt")]
public class Jwt : IJwtService
{
    private readonly ILogger<Jwt> mLogger;
    private readonly TokenValidationParameters mValidationParameters;
    private readonly SigningCredentials mSigningCredentials;
    private readonly TimeSpan mTokenLifetime;
    private readonly JwtSecurityTokenHandler mTokenHandler;

    public Jwt(JwtConfig config, ILogger<Jwt> logger = null)
    {
        mLogger = logger;
        mSigningCredentials = new SigningCredentials(config.SecurityKey, SecurityAlgorithms.HmacSha256);
        mTokenLifetime = TimeSpan.Parse(config.Lifetime);
        mValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config.Issuer,

            ValidateAudience = true,
            ValidAudience = config.Audience,
            ValidateLifetime = true,

            IssuerSigningKey = config.SecurityKey,
            ValidateIssuerSigningKey = true,
        };
        mTokenHandler = new JwtSecurityTokenHandler();
    }
    
    /// <inheritdoc />
    public string Generate(IEnumerable<Claim> claims, out DateTime expires)
    {
        expires = DateTime.UtcNow.Add(mTokenLifetime);
        return mTokenHandler.WriteToken(new JwtSecurityToken(
            audience: mValidationParameters.ValidAudience,
            issuer: mValidationParameters.ValidIssuer,
            claims: claims,
            expires: expires,
            signingCredentials: mSigningCredentials));
    }

    /// <inheritdoc />
    public ClaimsPrincipal GetPrincipal(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) 
            throw new ArgumentNullException(nameof(jwt));
        
        if (!mTokenHandler.CanReadToken(jwt))
            return null;

        try
        {
            return mTokenHandler.ValidateToken(jwt, mValidationParameters, out _);
        }
        catch (Exception e)
        {
            mLogger?.Warn("Token validation failed", e);
            return null;
        }
    }
    
    #region Utils

    /// <summary>
    /// Configuration for creation and validation JWT
    /// </summary>
    [Configuration("jwt")]
    public class JwtConfig
    {
        /// <summary>
        /// Secret security key as string
        /// </summary>
        public string Key { get; set; }
            
        /// <summary>
        /// Who create JWT
        /// </summary>
        public string Issuer { get; set; }
            
        /// <summary>
        /// For whom JWT was created
        /// </summary>
        public string Audience { get; set; }
            
        /// <summary>
        /// Default JWT lifetime as string
        /// </summary>
        public string Lifetime { get; set; }
        
        /// <summary>
        /// Secret key as <see cref="SymmetricSecurityKey"/> object
        /// </summary>
        public SymmetricSecurityKey SecurityKey => new(Encoding.ASCII.GetBytes(Key));

        /// <summary>
        /// Default configuration
        /// </summary>
        internal static JwtConfig Default => new()
        {
            Key = "Atmosphere.Engine.Default.Key.1234567890!",
            Issuer = "Atmosphere.Engine",
            Audience = "Atmosphere.Engine",
            Lifetime = "01:00:00"
        };
    }

    #endregion
}