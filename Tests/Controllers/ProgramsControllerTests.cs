using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using DrugIndications.API.Controllers;
using DrugIndications.Application.UseCases;
using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DrugIndications.API.Models;

namespace DrugIndications.Tests.Controllers
{
    public class ProgramsControllerTests
    {
        private readonly Mock<ICopayProgramRepository> _mockProgramRepository;
        private readonly Mock<ProcessCopayCardUseCase> _mockProcessCopayCardUseCase;
        private readonly ProgramsController _controller;

        public ProgramsControllerTests()
        {
            _mockProgramRepository = new Mock<ICopayProgramRepository>();
            _mockProcessCopayCardUseCase = new Mock<ProcessCopayCardUseCase>();
            _controller = new ProgramsController(_mockProgramRepository.Object, _mockProcessCopayCardUseCase.Object);
        }

        [Fact]
        public async Task GetById_ShouldReturnOkResult_WhenProgramExists()
        {
            // Arrange
            var programId = 11757;
            var expectedProgram = new CopayProgram
            {
                ProgramId = programId,
                ProgramName = "Dupixent MyWay Copay Card",
                ProgramType = "Coupon",
                CoverageEligibilities = new List<string> { "Commercially insured" },
                Requirements = new List<Requirement>
                {
                    new Requirement { Name = "us_residency", Value = "true" },
                    new Requirement { Name = "minimum_age", Value = "18" }
                },
                Benefits = new List<Benefit>
                {
                    new Benefit { Name = "max_annual_savings", Value = "13000.00" },
                    new Benefit { Name = "min_out_of_pocket", Value = "0.00" }
                },
                Forms = new List<Form>
                {
                    new Form { Name = "Enrollment Form", Link = "https://www.dupixent.com/support-savings/copay-card" }
                },
                Funding = new Funding { Evergreen = "true", CurrentFundingLevel = "Data Not Available" },
                Details = new List<ProgramDetail>
                {
                    new ProgramDetail
                    {
                        Eligibility = "Patient must have commercial insurance and be a legal resident of the US",
                        Program = "Patients may pay as little as $0 for every month of Dupixent",
                        Renewal = "Automatically re-enrolled every January 1st if used within 18 months",
                        Income = "Not required"
                    }
                }
            };

            _mockProgramRepository.Setup(repo => repo.GetByIdAsync(programId))
                .ReturnsAsync(expectedProgram);

            // Act
            var result = await _controller.GetById(programId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<CopayProgramResponseModel>(okResult.Value);
            Assert.Equal(expectedProgram.ProgramName, returnValue.program_name);
            Assert.Equal(expectedProgram.ProgramType, returnValue.program_type);
            Assert.Equal(expectedProgram.CoverageEligibilities, returnValue.coverage_eligibilities);
            Assert.Equal(expectedProgram.Requirements.Count, returnValue.requirements.Count);
            Assert.Equal(expectedProgram.Benefits.Count, returnValue.benefits.Count);
            Assert.Equal(expectedProgram.Forms.Count, returnValue.forms.Count);
            Assert.NotNull(returnValue.funding);
            Assert.Equal(expectedProgram.Details.Count, returnValue.details.Count);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenProgramDoesNotExist()
        {
            // Arrange
            var programId = 999;

            _mockProgramRepository.Setup(repo => repo.GetByIdAsync(programId))
                .ReturnsAsync((CopayProgram)null);

            // Act
            var result = await _controller.GetById(programId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedAtActionResult_WithNewProgram()
        {
            // Arrange
            var programId = 11757;
            var rawProgramData = JObject.Parse($@"
            {{
                ""ProgramID"": {programId},
                ""ProgramName"": ""Dupixent MyWay Copay Card"",
                ""CoverageEligibilities"": [""Commercially insured""],
                ""ProgramURL"": ""https://www.dupixent.com/support-savings/copay-card"",
                ""EligibilityDetails"": ""Patient must have commercial insurance"",
                ""IncomeReq"": false,
                ""IncomeDetails"": ""Data Not Available"",
                ""AnnualMax"": ""$13,000"",
                ""ProgramDetails"": ""Eligible patients may pay as little as $0 for every month of Dupixent"",
                ""AddRenewalDetails"": ""Automatically re-enrolled every January 1st""
            }}");

            var newProgram = new CopayProgram
            {
                ProgramId = programId,
                ProgramName = "Dupixent MyWay Copay Card",
                ProgramType = "Coupon",
                CoverageEligibilities = new List<string> { "Commercially insured" },
                Requirements = new List<Requirement>
                {
                    new Requirement { Name = "us_residency", Value = "true" },
                    new Requirement { Name = "minimum_age", Value = "18" }
                },
                Benefits = new List<Benefit>
                {
                    new Benefit { Name = "max_annual_savings", Value = "13000.00" },
                    new Benefit { Name = "min_out_of_pocket", Value = "0.00" }
                },
                Forms = new List<Form>
                {
                    new Form { Name = "Enrollment Form", Link = "https://www.dupixent.com/support-savings/copay-card" }
                },
                Funding = new Funding { Evergreen = "true", CurrentFundingLevel = "Data Not Available" },
                Details = new List<ProgramDetail>
                {
                    new ProgramDetail
                    {
                        Eligibility = "Patient must have commercial insurance",
                        Program = "Eligible patients may pay as little as $0 for every month of Dupixent",
                        Renewal = "Automatically re-enrolled every January 1st",
                        Income = "Not required"
                    }
                }
            };

            _mockProcessCopayCardUseCase.Setup(useCase => useCase.ExecuteAsync(It.IsAny<JObject>()))
                .ReturnsAsync(newProgram);

            // Act
            var result = await _controller.Create(rawProgramData);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetById), createdAtActionResult.ActionName);
            Assert.Equal(newProgram.ProgramId, createdAtActionResult.RouteValues["programId"]);

            var returnValue = Assert.IsType<CopayProgramResponseModel>(createdAtActionResult.Value);
            Assert.Equal(newProgram.ProgramName, returnValue.program_name);
            Assert.Equal(newProgram.ProgramType, returnValue.program_type);
            Assert.Equal(newProgram.CoverageEligibilities, returnValue.coverage_eligibilities);
            Assert.Equal(newProgram.Requirements.Count, returnValue.requirements.Count);
            Assert.Equal(newProgram.Benefits.Count, returnValue.benefits.Count);
            Assert.Equal(newProgram.Forms.Count, returnValue.forms.Count);
            Assert.NotNull(returnValue.funding);
            Assert.Equal(newProgram.Details.Count, returnValue.details.Count);
        }
    }
}