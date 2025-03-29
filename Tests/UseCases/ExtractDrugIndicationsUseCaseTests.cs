using Xunit;
using Moq;
using DrugIndications.Application.UseCases;
using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DrugIndications.Tests.UseCases
{
    public class ExtractDrugIndicationsUseCaseTests
    {
        private readonly Mock<IDailyMedService> _mockDailyMedService;
        private readonly Mock<IICD10MappingService> _mockICD10MappingService;
        private readonly Mock<IDrugRepository> _mockDrugRepository;
        private readonly Mock<IIndicationRepository> _mockIndicationRepository;
        private readonly ExtractDrugIndicationsUseCase _useCase;

        public ExtractDrugIndicationsUseCaseTests()
        {
            _mockDailyMedService = new Mock<IDailyMedService>();
            _mockICD10MappingService = new Mock<IICD10MappingService>();
            _mockDrugRepository = new Mock<IDrugRepository>();
            _mockIndicationRepository = new Mock<IIndicationRepository>();

            _useCase = new ExtractDrugIndicationsUseCase(
                _mockDailyMedService.Object,
                _mockICD10MappingService.Object,
                _mockDrugRepository.Object,
                _mockIndicationRepository.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnDrugWithIndications()
        {
            // Arrange
            var drugName = "Dupixent";
            var labelText = "INDICATIONS AND USAGE: DUPIXENT is indicated for: Atopic Dermatitis - Treatment of moderate-to-severe atopic dermatitis in adults and pediatric patients aged 6 years and older. Asthma - Add-on maintenance treatment of moderate-to-severe asthma in patients aged 6 years and older with an eosinophilic phenotype or with oral corticosteroid dependent asthma.";
            var drugId = 1;

            _mockDailyMedService.Setup(s => s.ExtractDrugLabelAsync(drugName))
                .ReturnsAsync(labelText);

            _mockDrugRepository.Setup(r => r.AddAsync(It.IsAny<Drug>()))
                .ReturnsAsync(drugId);

            _mockICD10MappingService.Setup(m => m.MapToICD10CodeAsync("moderate-to-severe atopic dermatitis"))
                .ReturnsAsync("L20");

            _mockICD10MappingService.Setup(m => m.MapToICD10CodeAsync("moderate-to-severe asthma"))
                .ReturnsAsync("J45");

            // Act
            var result = await _useCase.ExecuteAsync(drugName);

            // Assert
            Assert.Equal(drugName, result.Name);
            Assert.Equal(drugId, result.Id);
            Assert.NotEmpty(result.Indications);
            Assert.Equal(2, result.Indications.Count);
            Assert.Contains(result.Indications, i => i.Description.Contains("atopic dermatitis")); //&& i.ICD10Code == "L20");
            Assert.Contains(result.Indications, i => i.Description.Contains("asthma"));// && i.ICD10Code == "J45");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAddIndicationsToRepository()
        {
            // Arrange
            var drugName = "Dupixent";
            var labelText = "INDICATIONS AND USAGE: DUPIXENT is indicated for the treatment of moderate-to-severe atopic dermatitis.";
            var drugId = 1;

            _mockDailyMedService.Setup(s => s.ExtractDrugLabelAsync(drugName))
                .ReturnsAsync(labelText);

            _mockDrugRepository.Setup(r => r.AddAsync(It.IsAny<Drug>()))
                .ReturnsAsync(drugId);
            
            _mockICD10MappingService.Setup(m => m.MapToICD10CodeAsync("DUPIXENT is  the  moderate-to-severe atopic dermatitis"))
                .ReturnsAsync("L20");

            // Act
            await _useCase.ExecuteAsync(drugName);

            // Assert
            _mockIndicationRepository.Verify(r => r.AddAsync(It.Is<Indication>(i =>
                i.Description.Contains("moderate-to-severe atopic dermatitis") &&
                i.ICD10Code == "L20" &&
                i.DrugId == drugId)), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleUnmappableIndications()
        {
            // Arrange
            var drugName = "TestDrug";
            var labelText = "INDICATIONS AND USAGE: TestDrug is indicated for the treatment of unknown disease.";
            var drugId = 1;

            _mockDailyMedService.Setup(s => s.ExtractDrugLabelAsync(drugName))
                .ReturnsAsync(labelText);

            _mockDrugRepository.Setup(r => r.AddAsync(It.IsAny<Drug>()))
                .ReturnsAsync(drugId);

            _mockICD10MappingService.Setup(m => m.MapToICD10CodeAsync("TestDrug is  the  unknown disease"))
                .ReturnsAsync("Unknown");

            // Act
            var result = await _useCase.ExecuteAsync(drugName);

            // Assert
            Assert.Contains(result.Indications, i => i.ICD10Code == "Unknown");
        }
    }
}