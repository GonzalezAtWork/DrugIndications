using Xunit;
using Moq;
using System.Net.Http;
using DrugIndications.Infrastructure.Services;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Moq.Protected;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DrugIndications.Tests.Services
{
    public class EligibilityParserTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly EligibilityParser _parser;
        private readonly string _openAIApiKey;

        public EligibilityParserTests()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Get OpenAI API key from configuration
            _openAIApiKey = configuration["OpenAI:ApiKey"];

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Pass only the API key to the parser
            _parser = new EligibilityParser(_openAIApiKey);

            // Use reflection to set the private HttpClient field for testing
            var httpClientField = typeof(EligibilityParser).GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(_parser, _httpClient);
        }

        [Fact]
        public async Task ParseEligibilityDetailsAsync_ShouldReturnStructuredRequirements()
        {
            // Arrange
            var eligibilityText = "Patient must have commercial insurance and be a legal resident of the US";

            var openAIResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonConvert.SerializeObject(new List<Requirement>
                            {
                                new Requirement { Name = "us_residency", Value = "true" },
                                new Requirement { Name = "insurance_coverage", Value = "true" }
                            })
                        }
                    }
                }
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(openAIResponse))
                });

            // Act
            var result = await _parser.ParseEligibilityDetailsAsync(eligibilityText);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, r => r.Name == "us_residency" && r.Value == "true");
            Assert.Contains(result, r => r.Name == "insurance_coverage" && r.Value == "true");
        }

        [Fact]
        public async Task ParseEligibilityDetailsAsync_ShouldHandleEmptyResponse()
        {
            // Arrange
            var eligibilityText = "";

            var openAIResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "[]"
                        }
                    }
                }
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(openAIResponse))
                });

            // Act
            var result = await _parser.ParseEligibilityDetailsAsync(eligibilityText);

            // Assert
            Assert.NotNull(result);
        }
        [Fact]
        public async Task ParseEligibilityDetailsAsync_ShouldHandleAPIError()
        {
            // Arrange
            var eligibilityText = "Patient must have commercial insurance";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("API Error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _parser.ParseEligibilityDetailsAsync(eligibilityText));
        }
    }
}