using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using DrugIndications.API.Controllers;
using DrugIndications.Application.UseCases;
using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using DrugIndications.API.Models;

namespace DrugIndications.Tests.Controllers
{
    public class DrugsControllerTests
    {
        private readonly Mock<IDrugRepository> _mockDrugRepository;
        private readonly Mock<ExtractDrugIndicationsUseCase> _mockExtractDrugIndicationsUseCase;
        private readonly DrugsController _controller;
        public DrugsControllerTests()
        {
            _mockDrugRepository = new Mock<IDrugRepository>();
            _mockExtractDrugIndicationsUseCase = new Mock<ExtractDrugIndicationsUseCase>(MockBehavior.Loose,
                new Mock<IDailyMedService>().Object,
                new Mock<IICD10MappingService>().Object,
                new Mock<IDrugRepository>().Object,
                new Mock<IIndicationRepository>().Object);

            _controller = new DrugsController(_mockDrugRepository.Object, _mockExtractDrugIndicationsUseCase.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOkResult_WithListOfDrugs()
        {
            // Arrange
            var expectedDrugs = new List<Drug>
        {
            new Drug
            {
                Id = 1,
                Name = "Dupixent",
                Manufacturer = "Sanofi",
                Indications = new List<Indication>
                {
                    new Indication { Id = 1, Description = "Atopic dermatitis", ICD10Code = "L20", DrugId = 1 }
                }
            },
            new Drug
            {
                Id = 2,
                Name = "Humira",
                Manufacturer = "AbbVie",
                Indications = new List<Indication>
                {
                    new Indication { Id = 2, Description = "Rheumatoid arthritis", ICD10Code = "M05", DrugId = 2 }
                }
            }
        };

            _mockDrugRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(expectedDrugs);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<DrugResponseModel>>(okResult.Value);
            Assert.Equal(expectedDrugs.Count, returnValue.Count);
            Assert.Equal(expectedDrugs[0].Name, returnValue[0].Name);
            Assert.Single(returnValue[0].Indications);
            Assert.Equal(expectedDrugs[1].Name, returnValue[1].Name);
            Assert.Single(returnValue[1].Indications);
        }

        [Fact]
        public async Task GetById_ShouldReturnOkResult_WhenDrugExists()
        {
            // Arrange
            var drugId = 1;
            var expectedDrug = new Drug
            {
                Id = drugId,
                Name = "Dupixent",
                Manufacturer = "Sanofi",
                Indications = new List<Indication>
            {
                new Indication { Id = 1, Description = "Atopic dermatitis", ICD10Code = "L20", DrugId = drugId },
                new Indication { Id = 2, Description = "Asthma", ICD10Code = "J45", DrugId = drugId }
            }
            };

            _mockDrugRepository.Setup(repo => repo.GetByIdAsync(drugId))
                .ReturnsAsync(expectedDrug);

            // Act
            var result = await _controller.GetById(drugId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<DrugResponseModel>(okResult.Value);
            Assert.Equal(expectedDrug.Id, returnValue.Id);
            Assert.Equal(expectedDrug.Name, returnValue.Name);
            Assert.Equal(expectedDrug.Manufacturer, returnValue.Manufacturer);
            Assert.Equal(2, returnValue.Indications.Count);
            Assert.Equal("Atopic dermatitis", returnValue.Indications[0].Description);
            Assert.Equal("L20", returnValue.Indications[0].ICD10Code);
            Assert.Equal("Asthma", returnValue.Indications[1].Description);
            Assert.Equal("J45", returnValue.Indications[1].ICD10Code);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenDrugDoesNotExist()
        {
            // Arrange
            var drugId = 999;

            _mockDrugRepository.Setup(repo => repo.GetByIdAsync(drugId))
                .ReturnsAsync((Drug)null);

            // Act
            var result = await _controller.GetById(drugId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedAtActionResult_WithNewDrug()
        {
            // Arrange
            var drugRequest = new DrugRequestModel
            {
                Name = "Dupixent",
                Manufacturer = "Sanofi"
            };

            var newDrug = new Drug
            {
                Id = 1,
                Name = drugRequest.Name,
                Manufacturer = drugRequest.Manufacturer,
                Indications = new List<Indication>
            {
                new Indication { Id = 1, Description = "Atopic dermatitis", ICD10Code = "L20", DrugId = 1 }
            }
            };

            _mockExtractDrugIndicationsUseCase.Setup(useCase => useCase.ExecuteAsync(drugRequest.Name))
                .ReturnsAsync(newDrug);

            // Act
            var result = await _controller.Create(drugRequest);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetById), createdAtActionResult.ActionName);
            Assert.Equal(newDrug.Id, createdAtActionResult.RouteValues["id"]);

            var returnValue = Assert.IsType<DrugResponseModel>(createdAtActionResult.Value);
            Assert.Equal(newDrug.Id, returnValue.Id);
            Assert.Equal(newDrug.Name, returnValue.Name);
            Assert.Equal(newDrug.Manufacturer, returnValue.Manufacturer);
            Assert.Single(returnValue.Indications);
            Assert.Equal("Atopic dermatitis", returnValue.Indications[0].Description);
            Assert.Equal("L20", returnValue.Indications[0].ICD10Code);
        }
    }
}