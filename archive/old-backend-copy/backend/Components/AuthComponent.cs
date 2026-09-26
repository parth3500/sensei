using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sensei.Core.Interfaces;

namespace Sensei.Components;

public class AuthComponent : IAuthComponent
{
    private readonly string _configuredPassphrase;

    public AuthComponent(IConfiguration configuration)
    {
        string? fromEnv = Environment.GetEnvironmentVariable("APP_PASSPHRASE");
        string? fromConfig = configuration["AppPassphrase"];
        _configuredPassphrase = (fromEnv ?? fromConfig ?? "your-secure-passphrase-here").Trim();
    }

    public string GetConfiguredPassphrase() => _configuredPassphrase;

    public string HashPassphrase(string phrase)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(phrase));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyPassphrase(string input)
    {
        if (string.IsNullOrEmpty(_configuredPassphrase) || string.IsNullOrEmpty(input))
            return false;

        string trimmedInput = input.Trim();

        // Direct match
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(trimmedInput),
                Encoding.UTF8.GetBytes(_configuredPassphrase)))
        {
            return true;
        }

        // SHA-256 hash match (in case user stored hashed in config or passed hash)
        string inputHash = HashPassphrase(trimmedInput);
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(inputHash),
                Encoding.UTF8.GetBytes(_configuredPassphrase)))
        {
            return true;
        }

        return false;
    }
}
