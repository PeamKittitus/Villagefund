using Newtonsoft.Json;
using System;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EAccount
{
    public class ReportViewModel
    {
    }
    public class ReportActTransactionProvince
    {
        public string ProvinceName { get; set; }
        public string BookbankName { get; set; }
        public decimal VillageNumber { get; set;}
        public decimal Amount { get; set; }
    }
    public class ReportApproveActTransactionProvince
    {
        public int ProvinceId { get; set; }
        public int OrgId { get; set; }
        public string ProvinceName { get; set; }
        public int VillageNumberAll { get;set; }
        public int VillageNumber { get; set;}
        public int ProjectNumber { get; set;}
        public decimal AmountProject { get; set; }
        public int _VillageNumber { get; set; }
        public int _ProjectNumber { get; set; }
        public decimal _AmountProject { get; set; }
    }
    public class ReportApproveAndPendingActTransactionProvince
    {
        public int ProvinceId { get; set; }
        public int OrgId { get; set; }
        public string ProvinceName { get; set; }
        public string VillageName { get; set; }
        public int VillageNumberAll { get; set; }
        public int VillageNumber { get; set; }
        public int ProjectNumber { get; set; }
        public decimal AmountProject { get; set; }
        public int _VillageNumber { get; set; }
        public int _ProjectNumber { get; set; }
        public decimal _AmountProject { get; set; }
        public int PendingVillageNumber { get; set; }
        public int PendingProjectNumber { get; set; }
        public decimal PendingAmountProject { get; set; }
        public string DistrictName { get; set; }
        public string VillageCode { get; set; }

    }
    public class ReportProjectType
    {
        public string ProjectTypeName { get; set; }
        public int ProjectNumber { get; set; }
        public decimal Amount { get; set; }
        public string AccCode { get; set; }
    }

    public class ReportRequestSummaryByProvince
    {
        public int ProvinceId { get; set; }
        public int OrgId { get; set; }
        public string ProvinceName { get; set; }
        public int VillageNumberAll { get; set; }
        public int VillageNumber { get; set; }
        public int ProjectNumber { get; set; }
        public decimal AmountProject { get; set; }
        public int _VillageNumber { get; set; }
        public int _ProjectNumber { get; set; }
        public decimal _AmountProject { get; set; }
        public int PendingVillageNumber { get; set; }
        public int PendingProjectNumber { get; set; }
        public decimal PendingAmountProject { get; set; }
        public int DraftProjectNumber { get; set; }
    }

    public class ReportChildAccountBudgetByMonth
    {
        public string Title { get; set; }
        public string AccName { get; set; }
        public string OrgName { get; set; }
        public string Amount { get; set; }
        public string SenderDate { get; set; }
    }

    public class ReportTransAccountBudgetByVillageIndex
    {
        public string Title { get; set; }
        public string AccName { get; set; }
        public string VillageName { get; set; }
        public string Amount { get; set; }
        public string SenderDate { get; set; }
    }   
    
    public class ReportTransAccountBudgetByOrgIndex
    {
        public string Title { get; set; }
        public string AccName { get; set; }
        public string OrgName { get; set; }
        public string Amount { get; set; }
        public string SenderDate { get; set; }
    }

    public class RootReportTransAccountBudgetActivityByVillageIndex
    {
        public string ProjectName { get; set; }
        public int ActivityCount { get; set; }
        public string StatusName { get; set; }
        public string Amount { get; set; }
        public string VillageName { get; set; }
    }

    public class ReportTransBudgetByTypeAndProject
    {
        public string Project { get; set; }
        public string Activities { get; set; }
        public string Amount { get; set; }
        public string Transfer { get; set; }
        public string Type { get; set; }
        public string PaymentDate { get; set; }
    }

    public class ReportTransBudgetActivityByChard
    {
        public string Project { get; set; }
        public string Activities { get; set; }
        public string Amount { get; set; }
        public string Transferee { get; set; }
        public string Type { get; set; }
        public string PaymentDate { get; set; }
        public string Categories { get; set; }
    }

    public class ReportTransBudgetActivityByChardAndType 
    {
        public string Project { get; set; }
        public string Activities { get; set; }
        public string AmountBaht { get; set; }
        public string Transferee { get; set; }
        public string Type { get; set; }
        public string PaymentDate { get; set; }
        public string Categories { get; set; }
    }


}
