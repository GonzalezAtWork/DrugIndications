using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using DrugIndications.API.Controllers;
using DrugIndications.Infrastructure.Auth;
using System.Threading.Tasks;
using DrugIndications.API.Models;
using DrugIndications.Application.Interfaces;

namespace DrugIndications.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task Register_ShouldReturnOkResult_WhenRegistrationSucceeds()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Username = "testuser",
                Password = "testpassword",
                Role = "User"
            };

            _mockAuthService.Setup(service => service.RegisterUserAsync(registerModel.Username, registerModel.Password, registerModel.Role))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Register(registerModel);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenRegistrationFails()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Username = "testuser",
                Password = "testpassword",
                Role = "User"
            };

            _mockAuthService.Setup(service => service.RegisterUserAsync(registerModel.Username, registerModel.Password, registerModel.Role))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Register(registerModel);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ShouldReturnOkWithToken_WhenCredentialsAreValid()
        {
            // Arrange
            var loginModel = new LoginModel
            {
                Username = "testuser",
                Password = "testpassword"
            };

            var expectedToken = "valid-jwt-token";

            _mockAuthService.Setup(service => service.AuthenticateAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
            Assert.Equal(expectedToken, tokenResponse.Token);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginModel = new LoginModel
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(service => service.AuthenticateAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync((string)null);

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
    }
}