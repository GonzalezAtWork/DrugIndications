using DrugIndications.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DrugIndications.Infrastructure.Auth
{
    public class AuthService : IAuthService
    {
        private readonly SqlConnection _connection;
        private readonly string _jwtSecret;
        private readonly int _jwtExpirationMinutes;
        
        public AuthService(string connectionString, string jwtSecret, int jwtExpirationMinutes)
        {
            _connection = new SqlConnection(connectionString);
            _jwtSecret = jwtSecret;
            _jwtExpirationMinutes = jwtExpirationMinutes;
        }
        
        public async Task<bool> RegisterUserAsync(string username, string password, string role)
        {
            // Generate salt and hash password
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);
            
            try
            {
                await _connection.OpenAsync();
                using (var command = new SqlCommand(
                @"INSERT INTO Users (Username, PasswordHash, Salt, Role)
                VALUES (@Username, @PasswordHash, @Salt, @Role)", _connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@Role", role);

                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                _connection.Close();
            }
        }

        public async Task<bool> DeleteUserAsync(string username, string password)
        {
            try
            {
                await _connection.OpenAsync();

                string salt = "";
                using (var command = new SqlCommand("SELECT * FROM Users WHERE Username = @Username", _connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            salt = reader.GetString(reader.GetOrdinal("Salt"));
                        }
                    }
                }
                if (salt != "")
                {
                    // Generate hash password
                    var passwordHash = HashPassword(password, salt);

                    using (var command = new SqlCommand(
                    @"DELETE FROM Users where Username = @Username and PasswordHash = @PasswordHash", _connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                _connection.Close();
            }
        }

        public async Task<string> AuthenticateAsync(string username, string password)
        {
            await _connection.OpenAsync();
            UserDto user = null;

            using (var command = new SqlCommand("SELECT * FROM Users WHERE Username = @Username", _connection))
            {
                command.Parameters.AddWithValue("@Username", username);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = new UserDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                            Salt = reader.GetString(reader.GetOrdinal("Salt")),
                            Role = reader.GetString(reader.GetOrdinal("Role"))
                        };
                    }
                }
            }

            _connection.Close();

            if (user == null)
                return null;

            var passwordHash = HashPassword(password, user.Salt);

            if (passwordHash != user.PasswordHash)
                return null;

            // Generate JWT token
            return GenerateJwtToken(user);
        }
        
        private string GenerateSalt()
        {
            var random = new Random();
            var saltBytes = new byte[16];
            random.NextBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
        
        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = string.Concat(password, salt);
                var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hashedBytes = sha256.ComputeHash(saltedPasswordBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }
        
        private string GenerateJwtToken(UserDto user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        private class UserDto
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string Salt { get; set; }
            public string Role { get; set; }
        }
    }
}