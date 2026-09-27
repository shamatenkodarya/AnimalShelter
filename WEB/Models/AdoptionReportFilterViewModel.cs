using System.ComponentModel.DataAnnotations;

public class AdoptionReportFilterViewModel
{
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }
}