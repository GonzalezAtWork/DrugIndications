namespace DrugIndications.API.Models
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
    public class TokenResponse
    {
        public string Token { get; set; }
    }
    public class DrugRequestModel
    {
        public string Name { get; set; }
        public string Manufacturer { get; set; }
    }
    public class DrugResponseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public List<IndicationResponseModel> Indications { get; set; } = new List<IndicationResponseModel>();
    }
    public class IndicationResponseModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string ICD10Code { get; set; }
    }
    public class CopayProgramResponseModel
    {
        public string program_name { get; set; }
        public List<string> coverage_eligibilities { get; set; } = new List<string>();
        public string program_type { get; set; }
        public List<RequirementModel> requirements { get; set; } = new List<RequirementModel>();
        public List<BenefitModel> benefits { get; set; } = new List<BenefitModel>();
        public List<FormModel> forms { get; set; } = new List<FormModel>();
        public FundingModel funding { get; set; }
        public List<ProgramDetailModel> details { get; set; } = new List<ProgramDetailModel>();
    }
    public class RequirementModel
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    public class BenefitModel
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    public class FormModel
    {
        public string name { get; set; }
        public string link { get; set; }
    }
    public class FundingModel
    {
        public string evergreen { get; set; }
        public string current_funding_level { get; set; }
    }
    public class ProgramDetailModel
    {
        public string eligibility { get; set; }
        public string program { get; set; }
        public string renewal { get; set; }
        public string income { get; set; }
    }
    public class ErrorResponseModel
    {
        public string Message { get; set; }
        public int StatusCode { get; set; }
    }
    public class ValidationErrorResponseModel
    {
        public string Message { get; set; }
        public Dictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }
}