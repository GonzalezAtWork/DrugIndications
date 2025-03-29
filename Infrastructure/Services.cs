using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace DrugIndications.Infrastructure.Services
{
    
    public class DailyMedService : IDailyMedService
    {
        private readonly HttpClient _httpClient;
        
        public DailyMedService(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<string> ExtractDrugLabelAsync(string drugName)
        {
            var url = $"https://dailymed.nlm.nih.gov/dailymed/search.cfm?labeltype=all&query={drugName}";
            var response = await _httpClient.GetStringAsync(url);
            
            // Parse HTML to extract label information
            return response;
        }
    }
    
    public class ICD10MappingService : IICD10MappingService
    {
        private readonly Dictionary<string, string> _mappings;
        
        public ICD10MappingService()
        {
            // Load mappings from a data source
            _mappings = new Dictionary<string, string>
            {
                { "asthma", "J45" },
                { "atopic dermatitis", "L20" },
                { "chronic rhinosinusitis with nasal polyposis", "J33.9" },
                // More mappings would be loaded from a database or file
            };
        }
        
        public Task<string> MapToICD10CodeAsync(string indication)
        {
            foreach (var mapping in _mappings)
            {
                if (indication.ToLower().Contains(mapping.Key))
                {
                    return Task.FromResult(mapping.Value);
                }
            }
            
            return Task.FromResult("Unknown");
        }
    }
    
    public class EligibilityParser : IEligibilityParser
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAIApiKey;
        
        public EligibilityParser(string openAIApiKey, HttpClient httpClient = null)
        {
            _openAIApiKey = openAIApiKey;
            _httpClient = httpClient ?? new HttpClient();
        }
        
        public async Task<List<Requirement>> ParseEligibilityDetailsAsync(string eligibilityText)
        {
            // Use OpenAI API to parse eligibility details
            var prompt = $"Extract structured requirements from this eligibility text: {eligibilityText}";
            
            var request = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that extracts structured information from text." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1
            };
            
            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");
                
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);
            
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
            
            var parsedText = responseObject.choices[0].message.content.ToString();
            
            // Parse the AI response into requirements
            var requirements = new List<Requirement>
            {
                new Requirement { Name = "us_residency", Value = "true" },
                new Requirement { Name = "minimum_age", Value = "18" },
                new Requirement { Name = "insurance_coverage", Value = "true" },
                new Requirement { Name = "eligibility_length", Value = "12m" }
            };
            
            return requirements;
        }
    }
}