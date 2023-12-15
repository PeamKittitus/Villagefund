using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EProjects
{
    public class ProjectListViewModel
    {
        public Int64 ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectTime { get; set; }
        public decimal Progress { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int StatusDraft { get; set; }
        public int StatusWait { get; set; }
        public int StatusEdit { get; set; }
        public int StatusPendingApprove { get; set; }
        public int StatusNotApprove { get; set; }
        public int StatusApprove { get; set; }
        public int StatusAll { get; set; }
        public decimal Amount { get; set; }
        public string VillageName { get; set; }
        public string VillageCodeText { get; set; }
        public string ApproveBy { get; set; }
        public string ApproveDate { get; set; }
        public string FullName { get; set; }
        public string RiskTime { get; set; }
        public string RiskActiivity { get; set; }
        public string AccCode { get; set; }
    }

    public class PeriodViewModel
    {
        public Int64 ProjectPeriodId { get; set; }
        public string ProjectPeriodDetail { get; set; }
        public string ProjectName { get; set; }
        public string StartToEnd { get; set; }
        public int Period { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public string VillageName { get; set; }
        public string ProjectCode { get; set; }
        public Int64 ProjectId { get; set; }
    }

    public class ProjectDetail
    {
        public Int64 ProjecId { get; set; }
        public string ProjectName { get; set; }
        public string VillageName { get; set; }
        public decimal Amount { get; set; }
        public int BudgetYear { get; set; }
        public string Address { get; set; }
        public string ProJectCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Comment { get; set; }
        public int StatusId { get; set; }
    }

    public class PeriodViewModelDetail
    {
        public Int64 PeriodId { get; set; }
        public Int64 ProjectActivityId { get; set; }
        public int Period { get; set; }
        public int Status { get; set; }
        public string PeriodName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Comment { get; set; }
        public string StatusName { get; set; }
        public string ActivityBudget { get; set; }
    }

    public class PeriodFileList
    {
        public Int64 FilePeriodId { get; set; }
        public Int64 ProjectPeriodId { get; set; }
        public int TransactionYear { get; set; }
        public string FileName { get; set; }
        public string GencodeFileName { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public bool ApproverMark { get; set; }
    }

    public class MileStoneViewModel
    {
        public Int64 Id { get; set; }
        public string ProjectName { get; set; }
        public string ActivityName { get; set; }
        public string AccCode { get; set; }
    }

    public class MileStoneDetail
    {
        public string AcName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string StatusName { get; set; }
        public string VillageName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Comment { get; set; }
    }

    public class EprojectStructures
    {
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string StartEndDate { get; set; }
        public decimal Amount { get; set; }
        public decimal SubAmount { get; set; }
        public bool Status { get; set; }
        public Int64 AccountBudgetd { get; set; }
        public Int64 ParentId { get; set; }
    }
}
