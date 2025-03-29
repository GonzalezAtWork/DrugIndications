using Xunit;
using Moq;
using DrugIndications.Infrastructure.Repositories;
using DrugIndications.Domain.Entities;
using DrugIndications.Application.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DrugIndications.Tests.Repositories
{
    public class CopayProgramRepositoryTests
    {
        private readonly string _connectionString;
        private readonly CopayProgramRepository _repository;
        public CopayProgramRepositoryTests()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Get connection string from configuration
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _repository = new CopayProgramRepository(_connectionString);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCopayProgram_WhenProgramExists()
        {
            // Arrange
            var testProgram = await CreateTestCopayProgramAsync();

            try
            {
                // Act
                var result = await _repository.GetByIdAsync(testProgram.ProgramId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(testProgram.ProgramId, result.ProgramId);
                Assert.Equal(testProgram.ProgramName, result.ProgramName);
                Assert.Equal(testProgram.ProgramType, result.ProgramType);
                Assert.NotEmpty(result.CoverageEligibilities);
                Assert.NotEmpty(result.Requirements);
                Assert.NotEmpty(result.Benefits);
                Assert.NotEmpty(result.Forms);
                Assert.NotNull(result.Funding);
                Assert.NotEmpty(result.Details);
            }
            finally
            {
                // Cleanup
                await DeleteTestCopayProgramAsync(testProgram.ProgramId);
            }
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenProgramDoesNotExist()
        {
            // Arrange
            var nonExistentProgramId = 999999;

            // Act
            var result = await _repository.GetByIdAsync(nonExistentProgramId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_ShouldReturnProgramId_WhenProgramAdded()
        {
            // Arrange
            var newProgram = CreateCopayProgramModel();

            try
            {
                // Act
                var result = await _repository.AddAsync(newProgram);

                // Assert
                Assert.Equal(newProgram.ProgramId, result);

                // Verify the program was added
                var addedProgram = await _repository.GetByIdAsync(result);
                Assert.NotNull(addedProgram);
                Assert.Equal(newProgram.ProgramName, addedProgram.ProgramName);
                Assert.Equal(newProgram.ProgramType, addedProgram.ProgramType);
                Assert.Equal(newProgram.CoverageEligibilities.Count, addedProgram.CoverageEligibilities.Count);
                Assert.Equal(newProgram.Requirements.Count, addedProgram.Requirements.Count);
                Assert.Equal(newProgram.Benefits.Count, addedProgram.Benefits.Count);
                Assert.Equal(newProgram.Forms.Count, addedProgram.Forms.Count);
                Assert.NotNull(addedProgram.Funding);
                Assert.Equal(newProgram.Details.Count, addedProgram.Details.Count);
            }
            finally
            {
                // Cleanup
                await DeleteTestCopayProgramAsync(newProgram.ProgramId);
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateProgram_WhenProgramExists()
        {
            // Arrange
            var testProgram = await CreateTestCopayProgramAsync();

            // Modify the program
            testProgram.ProgramName = "Updated Program Name";
            testProgram.ProgramType = "Discount Card";
            testProgram.CoverageEligibilities.Add("Medicare Part D");
            testProgram.Requirements.Add(new Requirement { Name = "minimum_age", Value = "21" });
            testProgram.Benefits.Add(new Benefit { Name = "min_out_of_pocket", Value = "10.00" });

            try
            {
                // Act
                await _repository.UpdateAsync(testProgram);

                // Assert
                var updatedProgram = await _repository.GetByIdAsync(testProgram.ProgramId);
                Assert.NotNull(updatedProgram);
                Assert.Equal("Updated Program Name", updatedProgram.ProgramName);
                Assert.Equal("Discount Card", updatedProgram.ProgramType);
                Assert.Contains("Medicare Part D", updatedProgram.CoverageEligibilities);
                Assert.Contains(updatedProgram.Requirements, r => r.Name == "minimum_age" && r.Value == "21");
                Assert.Contains(updatedProgram.Benefits, b => b.Name == "min_out_of_pocket" && b.Value == "10.00");
            }
            finally
            {
                // Cleanup
                await DeleteTestCopayProgramAsync(testProgram.ProgramId);
            }
        }

        private async Task<CopayProgram> CreateTestCopayProgramAsync()
        {
            var program = CreateCopayProgramModel();
            await _repository.AddAsync(program);
            return program;
        }

        private CopayProgram CreateCopayProgramModel()
        {
            return new CopayProgram
            {
                ProgramId = new Random().Next(100000, 999999), // Generate a random ID to avoid conflicts
                ProgramName = $"Test Copay Program {Guid.NewGuid()}",
                ProgramType = "Coupon",
                CoverageEligibilities = new List<string> { "Commercially insured" },
                Requirements = new List<Requirement>
            {
                new Requirement { Name = "us_residency", Value = "true" },
                new Requirement { Name = "minimum_age", Value = "18" }
            },
                Benefits = new List<Benefit>
            {
                new Benefit { Name = "max_annual_savings", Value = "5000.00" },
                new Benefit { Name = "min_out_of_pocket", Value = "0.00" }
            },
                Forms = new List<Form>
            {
                new Form { Name = "Enrollment Form", Link = "https://example.com/form" }
            },
                Funding = new Funding
                {
                    Evergreen = "true",
                    CurrentFundingLevel = "Data Not Available"
                },
                Details = new List<ProgramDetail>
            {
                new ProgramDetail
                {
                    Eligibility = "Patient must have commercial insurance",
                    Program = "Patients may pay as little as $0",
                    Renewal = "Automatically re-enrolled every January 1st",
                    Income = "Not required"
                }
            }
            };
        }

        private async Task DeleteTestCopayProgramAsync(int programId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Delete related data first
            var deleteRelatedQuery = @"
            DELETE FROM CoverageEligibilities WHERE ProgramId = @ProgramId;
            DELETE FROM Requirements WHERE ProgramId = @ProgramId;
            DELETE FROM Benefits WHERE ProgramId = @ProgramId;
            DELETE FROM Forms WHERE ProgramId = @ProgramId;
            DELETE FROM Funding WHERE ProgramId = @ProgramId;
            DELETE FROM ProgramDetails WHERE ProgramId = @ProgramId;";

            using (var command = new SqlCommand(deleteRelatedQuery, connection))
            {
                command.Parameters.AddWithValue("@ProgramId", programId);
                await command.ExecuteNonQueryAsync();
            }

            // Then delete the program
            var deleteProgramQuery = "DELETE FROM CopayPrograms WHERE ProgramId = @ProgramId";
            using (var command = new SqlCommand(deleteProgramQuery, connection))
            {
                command.Parameters.AddWithValue("@ProgramId", programId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}