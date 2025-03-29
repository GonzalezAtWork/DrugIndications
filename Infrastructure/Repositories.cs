using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DrugIndications.Infrastructure.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly string ConnectionString;

        protected BaseRepository(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        protected async Task<SqlConnection> CreateAndOpenConnectionAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
    public class DrugRepository : BaseRepository, IDrugRepository
    {
        public DrugRepository(string connectionString) : base(connectionString) { }

        public async Task<Drug> GetByIdAsync(int id)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "SELECT * FROM Drugs WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Drug
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                };
            }

            return null;
        }

        public async Task<IEnumerable<Drug>> GetAllAsync()
        {
            var drugs = new List<Drug>();
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "SELECT * FROM Drugs";
            using var command = new SqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                drugs.Add(new Drug
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                });
            }

            return drugs;
        }

        public async Task<int> AddAsync(Drug drug)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "INSERT INTO Drugs (Name, Manufacturer) VALUES (@Name, @Manufacturer); SELECT SCOPE_IDENTITY();";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", drug.Name);
            command.Parameters.AddWithValue("@Manufacturer", drug.Manufacturer ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(Drug drug)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "UPDATE Drugs SET Name = @Name, Manufacturer = @Manufacturer WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", drug.Id);
            command.Parameters.AddWithValue("@Name", drug.Name);
            command.Parameters.AddWithValue("@Manufacturer", drug.Manufacturer ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "DELETE FROM Drugs WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
    public class IndicationRepository : BaseRepository, IIndicationRepository
    {
        public IndicationRepository(string connectionString) : base(connectionString) { }

        public async Task<Indication> GetByIdAsync(int id)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "SELECT * FROM Indications WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Indication
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    ICD10Code = reader.GetString(reader.GetOrdinal("ICD10Code")),
                    DrugId = reader.GetInt32(reader.GetOrdinal("DrugId"))
                };
            }

            return null;
        }

        public async Task<IEnumerable<Indication>> GetByDrugIdAsync(int drugId)
        {
            var indications = new List<Indication>();
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "SELECT * FROM Indications WHERE DrugId = @DrugId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DrugId", drugId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indications.Add(new Indication
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    ICD10Code = reader.GetString(reader.GetOrdinal("ICD10Code")),
                    DrugId = reader.GetInt32(reader.GetOrdinal("DrugId"))
                });
            }

            return indications;
        }

        public async Task<int> AddAsync(Indication indication)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "INSERT INTO Indications (Description, ICD10Code, DrugId) VALUES (@Description, @ICD10Code, @DrugId); SELECT SCOPE_IDENTITY();";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Description", indication.Description);
            command.Parameters.AddWithValue("@ICD10Code", indication.ICD10Code ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DrugId", indication.DrugId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(Indication indication)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "UPDATE Indications SET Description = @Description, ICD10Code = @ICD10Code, DrugId = @DrugId WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", indication.Id);
            command.Parameters.AddWithValue("@Description", indication.Description);
            command.Parameters.AddWithValue("@ICD10Code", indication.ICD10Code ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DrugId", indication.DrugId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "DELETE FROM Indications WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
    }

    public class CopayProgramRepository : BaseRepository, ICopayProgramRepository
    {
        public CopayProgramRepository(string connectionString) : base(connectionString) { }

        public async Task<CopayProgram> GetByIdAsync(int programId)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var query = "SELECT * FROM CopayPrograms WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var program = new CopayProgram
                {
                    ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                    ProgramName = reader.GetString(reader.GetOrdinal("ProgramName")),
                    ProgramType = reader.GetString(reader.GetOrdinal("ProgramType"))
                };

                // Close the first reader before opening new ones
                reader.Close();

                // Load related data
                program.CoverageEligibilities = await GetCoverageEligibilitiesAsync(connection, programId);
                program.Requirements = await GetRequirementsAsync(connection, programId);
                program.Benefits = await GetBenefitsAsync(connection, programId);
                program.Forms = await GetFormsAsync(connection, programId);
                program.Funding = await GetFundingAsync(connection, programId);
                program.Details = await GetDetailsAsync(connection, programId);

                return program;
            }

            return null;
        }

        public async Task<int> AddAsync(CopayProgram program)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var query = @"INSERT INTO CopayPrograms (ProgramId, ProgramName, ProgramType)
                          VALUES (@ProgramId, @ProgramName, @ProgramType);";

                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@ProgramName", program.ProgramName);
                command.Parameters.AddWithValue("@ProgramType", program.ProgramType);

                await command.ExecuteNonQueryAsync();

                await InsertCoverageEligibilitiesAsync(connection, transaction, program);
                await InsertRequirementsAsync(connection, transaction, program);
                await InsertBenefitsAsync(connection, transaction, program);
                await InsertFormsAsync(connection, transaction, program);
                await InsertFundingAsync(connection, transaction, program);
                await InsertDetailsAsync(connection, transaction, program);

                transaction.Commit();
                return program.ProgramId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task UpdateAsync(CopayProgram program)
        {
            using var connection = await CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var query = @"UPDATE CopayPrograms 
                          SET ProgramName = @ProgramName, ProgramType = @ProgramType
                          WHERE ProgramId = @ProgramId;";

                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@ProgramName", program.ProgramName);
                command.Parameters.AddWithValue("@ProgramType", program.ProgramType);

                await command.ExecuteNonQueryAsync();

                await DeleteRelatedDataAsync(connection, transaction, program.ProgramId);

                await InsertCoverageEligibilitiesAsync(connection, transaction, program);
                await InsertRequirementsAsync(connection, transaction, program);
                await InsertBenefitsAsync(connection, transaction, program);
                await InsertFormsAsync(connection, transaction, program);
                await InsertFundingAsync(connection, transaction, program);
                await InsertDetailsAsync(connection, transaction, program);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task<List<string>> GetCoverageEligibilitiesAsync(SqlConnection connection, int programId)
        {
            var eligibilities = new List<string>();
            var query = "SELECT Eligibility FROM CoverageEligibilities WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                eligibilities.Add(reader.GetString(0));
            }

            return eligibilities;
        }

        private async Task<List<Requirement>> GetRequirementsAsync(SqlConnection connection, int programId)
        {
            var requirements = new List<Requirement>();
            var query = "SELECT Name, Value FROM Requirements WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                requirements.Add(new Requirement
                {
                    Name = reader.GetString(0),
                    Value = reader.GetString(1)
                });
            }

            return requirements;
        }

        private async Task<List<Benefit>> GetBenefitsAsync(SqlConnection connection, int programId)
        {
            var benefits = new List<Benefit>();
            var query = "SELECT Name, Value FROM Benefits WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                benefits.Add(new Benefit
                {
                    Name = reader.GetString(0),
                    Value = reader.GetString(1)
                });
            }

            return benefits;
        }

        private async Task<List<Form>> GetFormsAsync(SqlConnection connection, int programId)
        {
            var forms = new List<Form>();
            var query = "SELECT Name, Link FROM Forms WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                forms.Add(new Form
                {
                    Name = reader.GetString(0),
                    Link = reader.GetString(1)
                });
            }

            return forms;
        }

        private async Task<Funding> GetFundingAsync(SqlConnection connection, int programId)
        {
            var query = "SELECT Evergreen, CurrentFundingLevel FROM Funding WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Funding
                {
                    Evergreen = reader.GetString(0),
                    CurrentFundingLevel = reader.GetString(1)
                };
            }

            return null;
        }

        private async Task<List<ProgramDetail>> GetDetailsAsync(SqlConnection connection, int programId)
        {
            var details = new List<ProgramDetail>();
            var query = "SELECT Eligibility, Program, Renewal, Income FROM ProgramDetails WHERE ProgramId = @ProgramId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProgramId", programId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                details.Add(new ProgramDetail
                {
                    Eligibility = reader.GetString(0),
                    Program = reader.GetString(1),
                    Renewal = reader.GetString(2),
                    Income = reader.GetString(3)
                });
            }

            return details;
        }

        private async Task InsertCoverageEligibilitiesAsync(SqlConnection connection, SqlTransaction transaction, CopayProgram program)
        {
            foreach (var eligibility in program.CoverageEligibilities)
            {
                var query = "INSERT INTO CoverageEligibilities (ProgramId, Eligibility) VALUES (@ProgramId, @Eligibility)";
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@Eligibility", eligibility);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertRequirementsAsync(SqlConnection connection, SqlTransaction transaction, CopayProgram program)
        {
            foreach (var requirement in program.Requirements)
            {
                var query = "INSERT INTO Requirements (ProgramId, Name, Value) VALUES (@ProgramId, @Name, @Value)";
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@Name", requirement.Name);
                command.Parameters.AddWithValue("@Value", requirement.Value);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertBenefitsAsync(SqlConnection connection, SqlTransaction transaction, CopayProgram program)
        {
            foreach (var benefit in program.Benefits)
            {
                var query = "INSERT INTO Benefits (ProgramId, Name, Value) VALUES (@ProgramId, @Name, @Value)";
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@Name", benefit.Name);
                command.Parameters.AddWithValue("@Value", benefit.Value);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertFormsAsync(SqlConnection connection, SqlTransaction transaction, CopayProgram program)
        {
            foreach (var form in program.Forms)
            {
                var query = "INSERT INTO Forms (ProgramId, Name, Link) VALUES (@ProgramId, @Name, @Link)";
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@Name", form.Name);
                command.Parameters.AddWithValue("@Link", form.Link);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertFundingAsync(SqlConnection connection, SqlTransaction transaction, CopayProgram program)
        {
            if (program.Funding != null)
            {
                var query = "INSERT INTO Funding (ProgramId, Evergreen, CurrentFundingLevel) VALUES (@ProgramId, @Evergreen, @CurrentFundingLevel)";
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@Evergreen", program.Funding.Evergreen);
                command.Parameters.AddWithValue("@CurrentFundingLevel", program.Funding.CurrentFundingLevel);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertDetailsAsync(SqlConnection connection, SqlTransaction transaction, CopayProgram program)
        {
            foreach (var detail in program.Details)
            {
                var query = "INSERT INTO ProgramDetails (ProgramId, Eligibility, Program, Renewal, Income) VALUES (@ProgramId, @Eligibility, @Program, @Renewal, @Income)";
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ProgramId", program.ProgramId);
                command.Parameters.AddWithValue("@Eligibility", detail.Eligibility);
                command.Parameters.AddWithValue("@Program", detail.Program);
                command.Parameters.AddWithValue("@Renewal", detail.Renewal);
                command.Parameters.AddWithValue("@Income", detail.Income);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task DeleteRelatedDataAsync(SqlConnection connection, SqlTransaction transaction, int programId)
        {
            var query = @"
            DELETE FROM CoverageEligibilities WHERE ProgramId = @ProgramId;
            DELETE FROM Requirements WHERE ProgramId = @ProgramId;
            DELETE FROM Benefits WHERE ProgramId = @ProgramId;
            DELETE FROM Forms WHERE ProgramId = @ProgramId;
            DELETE FROM Funding WHERE ProgramId = @ProgramId;
            DELETE FROM ProgramDetails WHERE ProgramId = @ProgramId;";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@ProgramId", programId);
            await command.ExecuteNonQueryAsync();
        }
    }

}