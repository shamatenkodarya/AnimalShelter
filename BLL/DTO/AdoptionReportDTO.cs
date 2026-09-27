namespace BLL.DTO
{
    public class AdoptionReportDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalAdoptions { get; set; }
        public Dictionary<string, int> AdoptionsByType { get; set; }
    }
}
