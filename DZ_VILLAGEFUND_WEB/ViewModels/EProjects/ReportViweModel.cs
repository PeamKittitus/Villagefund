namespace DZ_VILLAGEFUND_WEB.ViewModels.EProjects
{
    public class ReportViweModel
    {
    }
    public class ReportSummarizeProject
    {
        public string FundCode { get; set; }
        public string Village { get; set; }
        public string Moo { get; set; }
        public string SubDistrict { get; set; }
        public string District { get; set; }
        public string Province { get; set; }
        public int ProjectCount { get; set; }
        public string Budget { get; set; }
        public string Draft { get; set; }
        public string WaitingForCheck { get; set; }
        public string SendBackEdit { get; set; }
        public string PendingApproval { get; set; }
        public string Disapproved { get; set; }
        public string Approve { get; set; }
    }
}
