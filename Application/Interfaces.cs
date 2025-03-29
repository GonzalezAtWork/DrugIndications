using DrugIndications.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DrugIndications.Application.Interfaces
{
    public interface IAppDbConnection : IDisposable
    {
        Task OpenAsync(CancellationToken cancellationToken = default);
        IAppDbCommand CreateCommand();
    }

    public interface IAppDbCommand : IDisposable
    {
        string CommandText { get; set; }
        IAppDbConnection Connection { get; set; }
        Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default);
        Task<IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default);
        Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default);
        void AddParameter(string name, object value);
    }

    public interface IDbConnectionFactory
    {
        IAppDbConnection CreateConnection();
    }

    public interface IDrugRepository
    {
        Task<Drug> GetByIdAsync(int id);
        Task<IEnumerable<Drug>> GetAllAsync();
        Task<int> AddAsync(Drug drug);
        Task UpdateAsync(Drug drug);
        Task DeleteAsync(int id);
    }

    public interface IIndicationRepository
    {
        Task<Indication> GetByIdAsync(int id);
        Task<IEnumerable<Indication>> GetByDrugIdAsync(int drugId);
        Task<int> AddAsync(Indication indication);
        Task UpdateAsync(Indication indication);
        Task DeleteAsync(int id);
    }

    public interface ICopayProgramRepository
    {
        Task<CopayProgram> GetByIdAsync(int programId);
        Task<int> AddAsync(CopayProgram program);
        Task UpdateAsync(CopayProgram program);
    }

    public interface IDailyMedService
    {
        Task<string> ExtractDrugLabelAsync(string drugName);
    }

    public interface IICD10MappingService
    {
        Task<string> MapToICD10CodeAsync(string indication);
    }

    public interface IEligibilityParser
    {
        Task<List<Requirement>> ParseEligibilityDetailsAsync(string eligibilityText);
    }
    
    public interface IAuthService
    {
        Task<bool> RegisterUserAsync(string username, string password, string role);
        Task<string> AuthenticateAsync(string username, string password);
    }
}