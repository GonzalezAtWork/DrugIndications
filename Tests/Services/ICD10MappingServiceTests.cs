using Xunit;
using DrugIndications.Infrastructure.Services;
using System.Threading.Tasks;

namespace DrugIndications.Tests.Services
{
    public class ICD10MappingServiceTests
    {
        private readonly ICD10MappingService _service;

        public ICD10MappingServiceTests()
        {
            _service = new ICD10MappingService();
        }

        [Theory]
        [InlineData("asthma", "J45")]
        [InlineData("Moderate-to-severe asthma", "J45")]
        [InlineData("atopic dermatitis", "L20")]
        [InlineData("chronic rhinosinusitis with nasal polyposis", "J33.9")]
        public async Task MapToICD10CodeAsync_ShouldReturnCorrectCode_WhenIndicationIsKnown(string indication, string expectedCode)
        {
            // Act
            var result = await _service.MapToICD10CodeAsync(indication);

            // Assert
            Assert.Equal(expectedCode, result);
        }

        [Theory]
        [InlineData("unknown disease")]
        [InlineData("not a real indication")]
        public async Task MapToICD10CodeAsync_ShouldReturnUnknown_WhenIndicationIsNotMapped(string indication)
        {
            // Act
            var result = await _service.MapToICD10CodeAsync(indication);

            // Assert
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public async Task MapToICD10CodeAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var indication1 = "ASTHMA";
            var indication2 = "asthma";
            var indication3 = "Asthma";

            // Act
            var result1 = await _service.MapToICD10CodeAsync(indication1);
            var result2 = await _service.MapToICD10CodeAsync(indication2);
            var result3 = await _service.MapToICD10CodeAsync(indication3);

            // Assert
            Assert.Equal("J45", result1);
            Assert.Equal("J45", result2);
            Assert.Equal("J45", result3);
        }
    }
}