using Xunit;
using Moq;
using DrugIndications.Application.UseCases;
using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DrugIndications.Tests.UseCases
{
    public class ProcessCopayCardUseCaseTests
    {
        private readonly Mock<ICopayProgramRepository> _mockCopayProgramRepository;
        private readonly Mock<IEligibilityParser> _mockEligibilityParser;
        private readonly ProcessCopayCardUseCase _useCase;

        public ProcessCopayCardUseCaseTests()
        {
            _mockCopayProgramRepository = new Mock<ICopayProgramRepository>();
            _mockEligibilityParser = new Mock<IEligibilityParser>();

            _useCase = new ProcessCopayCardUseCase(
                _mockCopayProgramRepository.Object,
                _mockEligibilityParser.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnCopayProgram()
        {
            // Arrange
            var programId = 11757;
            var programName = "Dupixent MyWay Copay Card";
            var eligibilityDetails = "- Patient must have commercial insurance, including health insurance exchanges, federal employee plans, or state employee plans\n- Not valid for prescriptions paid, in whole or in part, by Medicaid, Medicare, VA, DOD, TRICARE, or other federal or state programs including any state pharmaceutical assistance programs\n- Program offer is not valid for cash-paying patients\n- Patient must be prescribed the Program Product for an FDA-approved indication\n- Patient must be a legal resident of the US or a US territory\n- Patients residing in or receiving treatment in certain states may not be eligible";
            var programURL = "https://www.dupixent.com/support-savings/copay-card";
            var annualMax = "$13,000";
            var programDetails = "-  Eligible patients may pay as little as $0 for every month of Dupixent\n-  The maximum annual patient benefit under the Dupixent MyWay Copay Card Program is $13,000\n-  Patient will receive copay card information via email following online enrollment & eligibility questions\n-  Ongoing follow-up and education are provided by the Nurse Educator to help patients stay on track with DUPIXENT\n-  Patient will be automatically re-enrolled every January 1st provided that their card has been used within 18 months\n-  For assistance or additional information, call 844-387-4936, option 1, Monday-Friday, 8 am-9 pm ET\n-  Pharmacists: for questions, call the LoyaltyScript program at 855-520-3765 (8am-8pm EST, Monday-Friday)";
            var renewalDetails = "Patient will be automatically re-enrolled every January 1st provided that their card has been used within 18 months";
            
            var rawProgramData = JObject.Parse($@"
            {{
                ""ProgramID"": {programId},
                ""ProgramName"": ""{programName}"",
                ""CoverageEligibilities"": [""Commercially insured""],
                ""ProgramURL"": ""{programURL}"",
                ""EligibilityDetails"": ""{eligibilityDetails}"",
                ""IncomeReq"": false,
                ""IncomeDetails"": ""Data Not Available"",
                ""AnnualMax"": ""{annualMax}"",
                ""ProgramDetails"": ""{programDetails}"",
                ""AddRenewalDetails"": ""{renewalDetails}""
            }}");
            
            var requirements = new List<Requirement>
            {
                new Requirement { Name = "us_residency", Value = "true" },
                new Requirement { Name = "minimum_age", Value = "18" },
                new Requirement { Name = "insurance_coverage", Value = "true" },
                new Requirement { Name = "eligibility_length", Value = "12m" }
            };
            
            _mockEligibilityParser.Setup(p => p.ParseEligibilityDetailsAsync(eligibilityDetails))
                .ReturnsAsync(requirements);
            
            _mockCopayProgramRepository.Setup(r => r.AddAsync(It.IsAny<CopayProgram>()))
                .ReturnsAsync(programId);

            // Act
            var result = await _useCase.ExecuteAsync(rawProgramData);

            // Assert
            Assert.Equal(programId, result.ProgramId);
            Assert.Equal(programName, result.ProgramName);
            Assert.Equal("Coupon", result.ProgramType);
            Assert.NotEmpty(result.CoverageEligibilities);
            Assert.Equal(requirements, result.Requirements);
            Assert.NotEmpty(result.Benefits);
            Assert.NotEmpty(result.Forms);
            Assert.NotNull(result.Funding);
            Assert.NotEmpty(result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldExtractBenefitsCorrectly()
        {
            // Arrange
            var programId = 11757;
            var annualMax = "$13,000";
            
            var rawProgramData = JObject.Parse($@"
            {{
                ""ProgramID"": {programId},
                ""ProgramName"": ""Dupixent MyWay Copay Card"",
                ""CoverageEligibilities"": [""Commercially insured""],
                ""ProgramURL"": ""https://www.dupixent.com/support-savings/copay-card"",
                ""EligibilityDetails"": ""Patient must have commercial insurance"",
                ""IncomeReq"": false,
                ""IncomeDetails"": ""Data Not Available"",
                ""AnnualMax"": ""{annualMax}"",
                ""ProgramDetails"": ""Eligible patients may pay as little as $0 for every month of Dupixent"",
                ""AddRenewalDetails"": ""Automatically re-enrolled every January 1st""
            }}");
            
            _mockEligibilityParser.Setup(p => p.ParseEligibilityDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Requirement>());
            
            _mockCopayProgramRepository.Setup(r => r.AddAsync(It.IsAny<CopayProgram>()))
                .ReturnsAsync(programId);

            // Act
            var result = await _useCase.ExecuteAsync(rawProgramData);

            // Assert
            Assert.Contains(result.Benefits, b => b.Name == "max_annual_savings" && b.Value == "13,000");
            Assert.Contains(result.Benefits, b => b.Name == "min_out_of_pocket" && b.Value == "0.00");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAddProgramToRepository()
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
            
            _mockEligibilityParser.Setup(p => p.ParseEligibilityDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Requirement>());
            
            _mockCopayProgramRepository.Setup(r => r.AddAsync(It.IsAny<CopayProgram>()))
                .ReturnsAsync(programId);

            // Act
            await _useCase.ExecuteAsync(rawProgramData);

            // Assert
            _mockCopayProgramRepository.Verify(r => r.AddAsync(It.Is<CopayProgram>(p => 
                p.ProgramId == programId && 
                p.ProgramName == "Dupixent MyWay Copay Card")), Times.Once);
        }
    }
}