using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EAccount
{
    public class TransactionAccBudgetViewModel
    {
        public Int64 TransactionAccBudgetId { get; set; }
        public string StructureAccName { get; set; }
        public string FromOrgAccount { get; set; }
        public string ToOrgAccount { get; set; }
        public decimal Amount { get; set; }
        public string SendDate { get; set; }
        public bool TransactionType { get; set; }
        public string Title { get; set; }
    }

    public class LedgerReportViewModel
    {
        public Int64 Id { get; set; }
        public string Receiver { get; set; }
        public decimal Amount { get; set; }
        public bool TransactionType { get; set; }
        public string ReceiverDate { get; set; }
    }

    public class ViewStructures
    {
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string StartEndDate { get; set; }
        public decimal Amount { get; set; }
        public decimal SubAmount { get; set; }
        public bool Status { get; set; }
        public Int64 AccountBudgetd { get; set; }
        public Int64 ParentId { get; set; }
        public string LookupValueDivision { get; set; }
        public string LookupValueDepartment { get; set; }
    }
    public class TransacionAccountBudgetViewModel
    {
        public Int64 TransactionAccBudgeId { get; set; }
        public string Title { get; set; }
        public int TransactionYear { get; set; }
        public bool TransactionType { get; set; }
        public string SenderBookBankId { get; set; }
        public string SenderOrgId { get; set; }
        public string ReceiverBookBankId { get; set; }
        public string ReceiverOrgId { get; set; }
        public decimal Amount { get; set; }
        public string SenderDate { get; set; }
        public string ReceiverDate { get; set; }
    }

    public class ReportTypeNewsViewModel
    {
        public Int64 Id { get; set; }
        public string TransactionYear { get; set; }
        public string Month { get; set; }
        public string fullName { get; set; }
        public int AmountMember { get; set; }
        public int AmountPublic { get; set; }
        public int AmountNews { get; set; }
        public int AmountWait { get; set; }
        public int AmountApprove { get; set; }
        public int AmountCreate { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
