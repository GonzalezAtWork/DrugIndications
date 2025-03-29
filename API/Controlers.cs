using DrugIndications.API.Models;
using DrugIndications.Application.Interfaces;
using DrugIndications.Application.UseCases;
using DrugIndications.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DrugIndications.API.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DrugsController : ControllerBase
    {
        private readonly IDrugRepository _drugRepository;
        private readonly ExtractDrugIndicationsUseCase _extractDrugIndicationsUseCase;

        public DrugsController(IDrugRepository drugRepository, ExtractDrugIndicationsUseCase extractDrugIndicationsUseCase)
        {
            _drugRepository = drugRepository;
            _extractDrugIndicationsUseCase = extractDrugIndicationsUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DrugResponseModel>>> GetAll()
        {
            var drugs = await _drugRepository.GetAllAsync();
            var response = new List<DrugResponseModel>();
            foreach (var drug in drugs)
            {
                response.Add(new DrugResponseModel
                {
                    Id = drug.Id,
                    Name = drug.Name,
                    Manufacturer = drug.Manufacturer,
                    Indications = drug.Indications.Select(i => new IndicationResponseModel
                    {
                        Id = i.Id,
                        Description = i.Description,
                        ICD10Code = i.ICD10Code
                    }).ToList()
                });
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DrugResponseModel>> GetById(int id)
        {
            var drug = await _drugRepository.GetByIdAsync(id);
            if (drug == null)
            {
                return NotFound();
            }

            var response = new DrugResponseModel
            {
                Id = drug.Id,
                Name = drug.Name,
                Manufacturer = drug.Manufacturer,
                Indications = drug.Indications.Select(i => new IndicationResponseModel
                {
                    Id = i.Id,
                    Description = i.Description,
                    ICD10Code = i.ICD10Code
                }).ToList()
            };
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<DrugResponseModel>> Create([FromBody] DrugRequestModel model)
        {
            var drug = await _extractDrugIndicationsUseCase.ExecuteAsync(model.Name);
            drug.Manufacturer = model.Manufacturer;

            var response = new DrugResponseModel
            {
                Id = drug.Id,
                Name = drug.Name,
                Manufacturer = drug.Manufacturer,
                Indications = drug.Indications.Select(i => new IndicationResponseModel
                {
                    Id = i.Id,
                    Description = i.Description,
                    ICD10Code = i.ICD10Code
                }).ToList()
            };
            return CreatedAtAction(nameof(GetById), new { id = drug.Id }, response);
        }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramsController : ControllerBase
    {
        private readonly ICopayProgramRepository _programRepository;
        private readonly ProcessCopayCardUseCase _processCopayCardUseCase;

        public ProgramsController(ICopayProgramRepository programRepository, ProcessCopayCardUseCase processCopayCardUseCase)
        {
            _programRepository = programRepository;
            _processCopayCardUseCase = processCopayCardUseCase;
        }

        [HttpGet("{programId}")]
        public async Task<ActionResult<CopayProgramResponseModel>> GetById(int programId)
        {
            var program = await _programRepository.GetByIdAsync(programId);
            if (program == null)
            {
                return NotFound();
            }

            var response = new CopayProgramResponseModel
            {
                program_name = program.ProgramName,
                coverage_eligibilities = program.CoverageEligibilities,
                program_type = program.ProgramType,
                requirements = program.Requirements.Select(r => new RequirementModel { name = r.Name, value = r.Value }).ToList(),
                benefits = program.Benefits.Select(b => new BenefitModel { name = b.Name, value = b.Value }).ToList(),
                forms = program.Forms.Select(f => new FormModel { name = f.Name, link = f.Link }).ToList(),
                funding = new FundingModel { evergreen = program.Funding.Evergreen, current_funding_level = program.Funding.CurrentFundingLevel },
                details = program.Details.Select(d => new ProgramDetailModel
                {
                    eligibility = d.Eligibility,
                    program = d.Program,
                    renewal = d.Renewal,
                    income = d.Income
                }).ToList()
            };
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<CopayProgramResponseModel>> Create([FromBody] dynamic rawProgramData)
        {
            var program = await _processCopayCardUseCase.ExecuteAsync(rawProgramData);

            


            var response = new CopayProgramResponseModel
            {
                program_name = program.ProgramName,
                coverage_eligibilities = program.CoverageEligibilities,
                program_type = program.ProgramType,
                requirements = (program.Requirements as List<Requirement>).Select((Func<Requirement, RequirementModel>)(r => new RequirementModel { name = r.Name, value = r.Value })).ToList(),
                benefits = (program.Benefits as List<Benefit>).Select((Func<Benefit, BenefitModel>)(b => new BenefitModel { name = b.Name, value = b.Value })).ToList(),
                forms = (program.Forms as List<Form>).Select((Func<Form, FormModel>)(f => new FormModel { name = f.Name, link = f.Link })).ToList(),
                funding = new FundingModel { evergreen = program.Funding.Evergreen, current_funding_level = program.Funding.CurrentFundingLevel },
                details = (program.Details as List<ProgramDetail>).Select((Func<ProgramDetail, ProgramDetailModel>)(d => new ProgramDetailModel
                {
                    eligibility = d.Eligibility,
                    program = d.Program,
                    renewal = d.Renewal,
                    income = d.Income
                })).ToList()
            };
            return CreatedAtAction(nameof(GetById), new { programId = program.ProgramId }, response);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var result = await _authService.RegisterUserAsync(model.Username, model.Password, model.Role);
            if (result)
            {
                return Ok();
            }
            return BadRequest(new { message = "User registration failed. Username may already exist." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var token = await _authService.AuthenticateAsync(model.Username, model.Password);
            if (token != null)
            {
                return Ok(new TokenResponse { Token = token });
            }
            return Unauthorized();
        }
    }
}