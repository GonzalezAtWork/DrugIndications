using DrugIndications.Application.Interfaces;
using DrugIndications.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace DrugIndications.Application.UseCases
{
    public class ExtractDrugIndicationsUseCase
    {
        private readonly IDailyMedService _dailyMedService;
        private readonly IICD10MappingService _icd10MappingService;
        private readonly IDrugRepository _drugRepository;
        private readonly IIndicationRepository _indicationRepository;
        public ExtractDrugIndicationsUseCase()
        {
            _dailyMedService = null;
            _icd10MappingService = null;
            _drugRepository = null;
            _indicationRepository = null;
        }
        public ExtractDrugIndicationsUseCase(
            IDailyMedService dailyMedService,
            IICD10MappingService icd10MappingService,
            IDrugRepository drugRepository,
            IIndicationRepository indicationRepository)
        {
            _dailyMedService = dailyMedService;
            _icd10MappingService = icd10MappingService;
            _drugRepository = drugRepository;
            _indicationRepository = indicationRepository;
        }

        public virtual async Task<Drug> ExecuteAsync(string drugName)
        {
            // Extract drug label from DailyMed
            var labelText = await _dailyMedService.ExtractDrugLabelAsync(drugName);
            
            // Parse indications from label text
            var indicationTexts = ParseIndicationsFromLabel(labelText);
            
            // Create drug entity
            var drug = new Drug { Name = drugName };
            var drugId = await _drugRepository.AddAsync(drug);
            drug.Id = drugId;
            
            // Map indications to ICD-10 codes and save
            foreach (var indicationText in indicationTexts)
            {
                var icd10Code = await _icd10MappingService.MapToICD10CodeAsync(indicationText);
                
                var indication = new Indication
                {
                    Description = indicationText,
                    ICD10Code = icd10Code,
                    DrugId = drugId
                };
                
                await _indicationRepository.AddAsync(indication);
                drug.Indications.Add(indication);
            }
            
            return drug;
        }
        
        private List<string> ParseIndicationsFromLabel(string labelText)
        {
            var indications = new List<string>();
            
            // Find the "INDICATIONS AND USAGE" section
            var startIndex = labelText.IndexOf("INDICATIONS AND USAGE:", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return indications;
            
            startIndex += "INDICATIONS AND USAGE:".Length;
            
            // Extract the section content
            var sectionContent = labelText.Substring(startIndex).Trim();
            
            // Split by common delimiters
            var sentences = sectionContent.Split(new[] { '.', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (string.IsNullOrWhiteSpace(trimmedSentence)) continue;
                
                // Remove common prefixes
                var indication = trimmedSentence
                    .Replace("Treatment of", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("For the treatment of", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("For treatment of", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Indicated for", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
                
                if (!string.IsNullOrWhiteSpace(indication))
                {
                    indications.Add(indication);
                }
            }
            
            return indications;
        }
    }

    public class ProcessCopayCardUseCase
    {
        private readonly ICopayProgramRepository _copayProgramRepository;
        private readonly IEligibilityParser _eligibilityParser;
        public ProcessCopayCardUseCase()
        {
            _copayProgramRepository = null;
            _eligibilityParser = null;
        }
        public ProcessCopayCardUseCase(
            ICopayProgramRepository copayProgramRepository,
            IEligibilityParser eligibilityParser)
        {
            _copayProgramRepository = copayProgramRepository;
            _eligibilityParser = eligibilityParser;
        }

        public virtual async Task<CopayProgram> ExecuteAsync(JObject rawProgramData)
        {
            var program = new CopayProgram
            {
                ProgramId = rawProgramData["ProgramID"].Value<int>(),
                ProgramName = rawProgramData["ProgramName"].Value<string>(),
                ProgramType = "Coupon",
                CoverageEligibilities = rawProgramData["CoverageEligibilities"].ToObject<List<string>>()
            };

            // Parse eligibility details using AI
            var eligibilityText = rawProgramData["EligibilityDetails"].Value<string>();
            program.Requirements = await _eligibilityParser.ParseEligibilityDetailsAsync(eligibilityText);

            // Extract benefits
            program.Benefits = new List<Benefit>
            {
                new Benefit { Name = "max_annual_savings", Value = rawProgramData["AnnualMax"].Value<string>().Replace("$", "") },
                new Benefit { Name = "min_out_of_pocket", Value = "0.00" }
            };

            // Add forms
            program.Forms = new List<Form>
            {
                new Form { 
                    Name = "Enrollment Form", 
                    Link = rawProgramData["ProgramURL"].Value<string>() 
                }
            };

            // Set funding information
            program.Funding = new Funding
            {
                Evergreen = "true",
                CurrentFundingLevel = "Data Not Available"
            };

            // Add program details
            program.Details = new List<ProgramDetail>
            {
                new ProgramDetail
                {
                    Eligibility = eligibilityText,
                    Program = rawProgramData["ProgramDetails"].Value<string>(),
                    Renewal = rawProgramData["AddRenewalDetails"].Value<string>(),
                    Income = rawProgramData["IncomeReq"].Value<bool>() ? 
                        rawProgramData["IncomeDetails"].Value<string>() : 
                        "Not required"
                }
            };

            await _copayProgramRepository.AddAsync(program);
            return program;
        }
    }
}