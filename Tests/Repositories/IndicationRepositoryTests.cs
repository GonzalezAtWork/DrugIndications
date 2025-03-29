using Xunit;
using Moq;
using DrugIndications.Infrastructure.Repositories;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using DrugIndications.Application.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DrugIndications.Tests.Repositories
{
    public class IndicationRepositoryTests
    {
        private readonly string _connectionString;
        private readonly IndicationRepository _repository;

        public IndicationRepositoryTests()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Get connection string from configuration
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _repository = new IndicationRepository(_connectionString);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnIndication_WhenIndicationExists()
        {
            // Arrange
            var testIndication = await CreateTestIndicationAsync();

            try
            {
                // Act
                var result = await _repository.GetByIdAsync(testIndication.Id);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(testIndication.Id, result.Id);
                Assert.Equal(testIndication.Description, result.Description);
                Assert.Equal(testIndication.ICD10Code, result.ICD10Code);
                Assert.Equal(testIndication.DrugId, result.DrugId);
            }
            finally
            {
                // Cleanup
                await DeleteTestIndicationAsync(testIndication.Id);
            }
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenIndicationDoesNotExist()
        {
            // Arrange
            var nonExistentIndicationId = 99999;

            // Act
            var result = await _repository.GetByIdAsync(nonExistentIndicationId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByDrugIdAsync_ShouldReturnIndications_WhenDrugExists()
        {
            // Arrange
            var testDrugId = await CreateTestDrugAsync();
            var testIndication1 = await CreateTestIndicationAsync(testDrugId);
            var testIndication2 = await CreateTestIndicationAsync(testDrugId);

            try
            {
                // Act
                var result = await _repository.GetByDrugIdAsync(testDrugId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                Assert.Contains(result, i => i.Id == testIndication1.Id);
                Assert.Contains(result, i => i.Id == testIndication2.Id);
            }
            finally
            {
                // Cleanup
                await DeleteTestIndicationAsync(testIndication1.Id);
                await DeleteTestIndicationAsync(testIndication2.Id);
                await DeleteTestDrugAsync(testDrugId);
            }
        }

        [Fact]
        public async Task AddAsync_ShouldAddNewIndication_AndReturnId()
        {
            // Arrange
            var testDrugId = await CreateTestDrugAsync();
            var newIndication = new Indication
            {
                Description = "Test Indication",
                ICD10Code = "T99",
                DrugId = testDrugId
            };

            try
            {
                // Act
                var result = await _repository.AddAsync(newIndication);

                // Assert
                Assert.True(result > 0);

                var addedIndication = await _repository.GetByIdAsync(result);
                Assert.NotNull(addedIndication);
                Assert.Equal(newIndication.Description, addedIndication.Description);
                Assert.Equal(newIndication.ICD10Code, addedIndication.ICD10Code);
                Assert.Equal(newIndication.DrugId, addedIndication.DrugId);
                newIndication.Id = addedIndication.Id;
            }
            finally
            {
                // Cleanup
                if (newIndication.Id > 0)
                {
                    await DeleteTestIndicationAsync(newIndication.Id);
                }
                await DeleteTestDrugAsync(testDrugId);
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExistingIndication()
        {
            // Arrange
            var testIndication = await CreateTestIndicationAsync();
            testIndication.Description = "Updated Indication Description";
            testIndication.ICD10Code = "U99";

            try
            {
                // Act
                await _repository.UpdateAsync(testIndication);

                // Assert
                var updatedIndication = await _repository.GetByIdAsync(testIndication.Id);
                Assert.NotNull(updatedIndication);
                Assert.Equal(testIndication.Description, updatedIndication.Description);
                Assert.Equal(testIndication.ICD10Code, updatedIndication.ICD10Code);
                Assert.Equal(testIndication.DrugId, updatedIndication.DrugId);
            }
            finally
            {
                // Cleanup
                await DeleteTestIndicationAsync(testIndication.Id);
            }
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteExistingIndication()
        {
            // Arrange
            var testIndication = await CreateTestIndicationAsync();

            try
            {
                // Act
                await _repository.DeleteAsync(testIndication.Id);

                // Assert
                var deletedIndication = await _repository.GetByIdAsync(testIndication.Id);
                Assert.Null(deletedIndication);
            }
            finally
            {
                // Cleanup (not needed for delete test, but included for consistency)
                await DeleteTestIndicationAsync(testIndication.Id);
            }
        }

        private async Task<int> CreateTestDrugAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO Drugs (Name, Manufacturer) VALUES (@Name, @Manufacturer); SELECT SCOPE_IDENTITY();";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", $"Test Drug {Guid.NewGuid()}");
            command.Parameters.AddWithValue("@Manufacturer", $"Test Manufacturer {Guid.NewGuid()}");

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private async Task<Indication> CreateTestIndicationAsync()
        {
            var testDrugId = await CreateTestDrugAsync();
            return await CreateTestIndicationAsync(testDrugId);
        }

        private async Task<Indication> CreateTestIndicationAsync(int drugId)
        {
            var indication = new Indication
            {
                Description = $"Test Indication {Guid.NewGuid()}",
                ICD10Code = $"T{new Random().Next(10, 99)}",
                DrugId = drugId
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO Indications (Description, ICD10Code, DrugId) VALUES (@Description, @ICD10Code, @DrugId); SELECT SCOPE_IDENTITY();";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Description", indication.Description);
            command.Parameters.AddWithValue("@ICD10Code", indication.ICD10Code);
            command.Parameters.AddWithValue("@DrugId", indication.DrugId);

            indication.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

            return indication;
        }

        private async Task DeleteTestIndicationAsync(int indicationId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM Indications WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", indicationId);

            await command.ExecuteNonQueryAsync();
        }

        private async Task DeleteTestDrugAsync(int drugId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM Drugs WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", drugId);

            await command.ExecuteNonQueryAsync();
        }
    }
}