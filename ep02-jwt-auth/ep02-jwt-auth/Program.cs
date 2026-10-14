// NuGet Package Required: System.IdentityModel.Tokens.Jwt
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace JwtFixDemo
{
    class Program
    {
        // The server's actual private signing key
        private static byte[] randomBytes;
        private static string SecretKey;
        private const string Issuer = "https://auth.mycompany.com";
        private const string Audience = "https://api.mycompany.com";

        // static constructor initializes static fields once
        static Program()
        {
            randomBytes = new byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            SecretKey = Convert.ToBase64String(randomBytes);
        }

        static void Main(string[] args)
        {
            // 1. Forged token with fake signature
            string forgedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NSIsIm5hbWUiOiJBdHRhY2tlciIsInJvbGUiOiJBZG1pbiJ9.fake_signature_that_was_never_signed";

            // 2. Legitimate token signed with SecretKey
            string legitimateToken = GenerateValidToken("12345", "Alice", "User");

            Console.WriteLine("==================================================");
            Console.WriteLine(" 🔴 SCENARIO 1: NAIVE AI CODE (ReadJwtToken)");
            Console.WriteLine("==================================================");
            Console.WriteLine("--- Testing Forged Admin Token ---");
            NaiveValidateAndAuthenticate(forgedToken);

            Console.WriteLine("\n==================================================");
            Console.WriteLine(" 🟢 SCENARIO 2: SENIOR REFACTORED CODE (ValidateToken)");
            Console.WriteLine("==================================================");
            Console.WriteLine("--- Testing Forged Admin Token ---");
            ValidateAndAuthenticate(forgedToken);

            Console.WriteLine("\n--- Testing Legitimate Token ---");
            ValidateAndAuthenticate(legitimateToken);
        }

        /// <summary>
        /// VULNERABLE AI IMPLEMENTATION: Uses ReadJwtToken() which decodes Base64 text 
        /// WITHOUT cryptographically verifying the token signature!
        /// </summary>
        static void NaiveValidateAndAuthenticate(string tokenString)
        {
            var handler = new JwtSecurityTokenHandler();

            // SECURITY FLAW: ReadJwtToken merely parses JSON text from Base64.
            // It NEVER validates if the signature matches SecretKey!
            var jsonToken = handler.ReadJwtToken(tokenString);

            string? userRole = jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            string? userName = jsonToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name)?.Value
                                ?? jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            Console.WriteLine($"[AI VULNERABLE]: Token payload decoded without signature check!");
            Console.WriteLine($"Decoded User: {userName}, Role: {userRole}");
            if (userRole == "Admin")
            {
                Console.WriteLine("🔴 SECURITY RISK: Granted ADMIN access to a forged token!");
            }
        }

        /// <summary>
        /// SECURE IMPLEMENTATION: Uses ValidateToken() to cryptographically verify signature & expiration.
        /// </summary>
        static void ValidateAndAuthenticate(string tokenString)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            // Configure strict validation rules
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                // ValidateToken checks cryptographic signature using SecretKey
                ClaimsPrincipal principal = handler.ValidateToken(tokenString, validationParameters, out SecurityToken validatedToken);

                string? userRole = principal.FindFirst("role")?.Value;
                string? userName = principal.FindFirst(ClaimTypes.Name)?.Value;

                Console.WriteLine($"[SUCCESS]: Token Signature Validated!");
                Console.WriteLine($"Authenticated User: {userName}, Role: {userRole}");
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                Console.WriteLine("[BLOCKED]: Signature check failed! Token was forged or key missing.");
            }
            catch (SecurityTokenException ex)
            {
                Console.WriteLine($"[BLOCKED]: Token validation failed ({ex.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLOCKED]: Invalid token format or execution error: {ex.Message}");
            }
        }

        // Helper method to create a legitimately signed token for comparison
        static string GenerateValidToken(string userId, string userName, string role)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userName),
                    new Claim("role", role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = Issuer,
                Audience = Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}