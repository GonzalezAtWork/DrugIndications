namespace DrugIndications.Domain.Entities
{
    public class Drug
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public List<Indication> Indications { get; set; } = new List<Indication>();
    }

    public class Indication
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string ICD10Code { get; set; }
        public int DrugId { get; set; }
        public Drug Drug { get; set; }
    }

    public class CopayProgram
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public List<string> CoverageEligibilities { get; set; } = new List<string>();
        public string ProgramType { get; set; }
        public List<Requirement> Requirements { get; set; } = new List<Requirement>();
        public List<Benefit> Benefits { get; set; } = new List<Benefit>();
        public List<Form> Forms { get; set; } = new List<Form>();
        public Funding Funding { get; set; }
        public List<ProgramDetail> Details { get; set; } = new List<ProgramDetail>();
    }

    public class Requirement
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Benefit
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Form
    {
        public string Name { get; set; }
        public string Link { get; set; }
    }

    public class Funding
    {
        public string Evergreen { get; set; }
        public string CurrentFundingLevel { get; set; }
    }

    public class ProgramDetail
    {
        public string Eligibility { get; set; }
        public string Program { get; set; }
        public string Renewal { get; set; }
        public string Income { get; set; }
    }
}