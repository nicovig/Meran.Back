namespace Meran.Back.DTO
{
    public class ApplicationFeaturePlanValueDto
    {
        public Guid ApplicationPlanId { get; set; }
        public string Value { get; set; } = null!;
    }

    public class ApplicationFeatureDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = null!;
        public string Type { get; set; } = null!;
        public List<ApplicationFeaturePlanValueDto> PlanValues { get; set; } = new();
    }

    public class UpsertApplicationFeatureRequestDto
    {
        public string Key { get; set; } = null!;
        public string Type { get; set; } = null!;
        public List<ApplicationFeaturePlanValueDto> PlanValues { get; set; } = new();
    }

    public class UpsertApplicationFeaturesRequestDto
    {
        public List<UpsertApplicationFeatureRequestDto> Features { get; set; } = new();
    }
}
