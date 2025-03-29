using Xunit;
using Moq;
using DrugIndications.Infrastructure.Repositories;
using DrugIndications.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DrugIndications.Application.Interfaces;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace DrugIndications.Tests.Repositories
{
    public class DrugRepositoryTests
    {
        private readonly string _connectionString;
        private readonly DrugRepository _repository;

        public DrugRepositoryTests()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Get connection string from configuration
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _repository = new DrugRepository(_connectionString);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDrug_WhenDrugExists()
        {
            // Arrange
            var testDrug = await CreateTestDrugAsync();

            try
            {
                // Act
                var result = await _repository.GetByIdAsync(testDrug.Id);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(testDrug.Id, result.Id);
                Assert.Equal(testDrug.Name, result.Name);
                Assert.Equal(testDrug.Manufacturer, result.Manufacturer);
            }
            finally
            {
                // Cleanup
                await DeleteTestDrugAsync(testDrug.Id);
            }
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDrugDoesNotExist()
        {
            // Arrange
            var nonExistentDrugId = 99999;

            // Act
            var result = await _repository.GetByIdAsync(nonExistentDrugId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllDrugs()
        {
            // Arrange
            var testDrug1 = await CreateTestDrugAsync();
            var testDrug2 = await CreateTestDrugAsync();

            try
            {
                // Act
                var result = await _repository.GetAllAsync();

                // Assert
                Assert.NotNull(result);
                Assert.Contains(result, d => d.Id == testDrug1.Id);
                Assert.Contains(result, d => d.Id == testDrug2.Id);
            }
            finally
            {
                // Cleanup
                await DeleteTestDrugAsync(testDrug1.Id);
                await DeleteTestDrugAsync(testDrug2.Id);
            }
        }

        [Fact]
        public async Task AddAsync_ShouldAddNewDrug_AndReturnId()
        {
            // Arrange
            var newDrug = new Drug
            {
                Name = "Test Drug",
                Manufacturer = "Test Manufacturer"
            };

            try
            {
                // Act
                var result = await _repository.AddAsync(newDrug);

                // Assert
                Assert.True(result > 0);

                var addedDrug = await _repository.GetByIdAsync(result);
                Assert.NotNull(addedDrug);
                Assert.Equal(newDrug.Name, addedDrug.Name);
                Assert.Equal(newDrug.Manufacturer, addedDrug.Manufacturer);
            }
            finally
            {
                // Cleanup
                if (newDrug.Id > 0)
                {
                    await DeleteTestDrugAsync(newDrug.Id);
                }
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExistingDrug()
        {
            // Arrange
            var testDrug = await CreateTestDrugAsync();
            testDrug.Name = "Updated Drug Name";
            testDrug.Manufacturer = "Updated Manufacturer";

            try
            {
                // Act
                await _repository.UpdateAsync(testDrug);

                // Assert
                var updatedDrug = await _repository.GetByIdAsync(testDrug.Id);
                Assert.NotNull(updatedDrug);
                Assert.Equal(testDrug.Name, updatedDrug.Name);
                Assert.Equal(testDrug.Manufacturer, updatedDrug.Manufacturer);
            }
            finally
            {
                // Cleanup
                await DeleteTestDrugAsync(testDrug.Id);
            }
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteExistingDrug()
        {
            // Arrange
            var testDrug = await CreateTestDrugAsync();

            // Act
            await _repository.DeleteAsync(testDrug.Id);

            // Assert
            var deletedDrug = await _repository.GetByIdAsync(testDrug.Id);
            Assert.Null(deletedDrug);
        }

        private async Task<Drug> CreateTestDrugAsync()
        {
            var drug = new Drug
            {
                Name = $"Test Drug {Guid.NewGuid()}",
                Manufacturer = $"Test Manufacturer {Guid.NewGuid()}"
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO Drugs (Name, Manufacturer) VALUES (@Name, @Manufacturer); SELECT SCOPE_IDENTITY();";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", drug.Name);
            command.Parameters.AddWithValue("@Manufacturer", drug.Manufacturer);

            drug.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

            return drug;
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