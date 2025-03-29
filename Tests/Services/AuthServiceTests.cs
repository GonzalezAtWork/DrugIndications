using Xunit;
using Moq;
using System.Data.SqlClient;
using DrugIndications.Infrastructure.Auth;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace DrugIndications.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly string _jwtSecret = "YourSuperSecretKeyForJWTAuthenticationWithAtLeast32Characters";
        private readonly string _testConnectionString;
        private readonly int _jwtExpirationMinutes = 60;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Get OpenAI API key from configuration
            _testConnectionString = configuration["ConnectionStrings:TestConnection"];
            _jwtSecret = configuration["Jwt:Secret"];

            _mockConnection = new Mock<IDbConnection>();
            _authService = new AuthService(_testConnectionString, _jwtSecret, _jwtExpirationMinutes);
        }

        [Fact]
        public async Task A_RegisterUserAsync_ShouldReturnTrue_WhenRegistrationSucceeds()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";
            var role = "User";

            // Act
            var result = await _authService.RegisterUserAsync(username, password, role);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task E_DeleteUserAsync_ShouldReturnTrue_WhenDeletionSucceeds()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";

            // Act
            var result = await _authService.DeleteUserAsync(username, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task D_AuthenticateAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";

            // Act
            var token = await _authService.AuthenticateAsync(username, password);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task C_AuthenticateAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var username = "nonexistentuser";
            var password = "testpassword";

            // Act
            var token = await _authService.AuthenticateAsync(username, password);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task B_AuthenticateAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
        {
            // Arrange
            var username = "testuser";
            var password = "wrongpassword";

            // Act
            var token = await _authService.AuthenticateAsync(username, password);

            // Assert
            Assert.Null(token);
        }
    }
}