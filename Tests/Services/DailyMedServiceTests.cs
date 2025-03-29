using Xunit;
using Moq;
using System.Net.Http;
using DrugIndications.Infrastructure.Services;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Moq.Protected;

namespace DrugIndications.Tests.Services
{
    public class DailyMedServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly DailyMedService _service;

        public DailyMedServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new DailyMedService(_httpClient);
        }

        [Fact]
        public async Task ExtractDrugLabelAsync_ShouldReturnLabelText_WhenDrugExists()
        {
            // Arrange
            var drugName = "Dupixent";
            var expectedResponse = "<html><body><div class='indications'>For treatment of asthma, atopic dermatitis</div></body></html>";
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedResponse)
                });

            // Act
            var result = await _service.ExtractDrugLabelAsync(drugName);

            // Assert
            Assert.Contains("asthma", result);
            Assert.Contains("atopic dermatitis", result);
        }

        [Fact]
        public async Task ExtractDrugLabelAsync_ShouldThrowException_WhenApiCallFails()
        {
            // Arrange
            var drugName = "NonExistentDrug";
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.ExtractDrugLabelAsync(drugName));
        }
    }
}