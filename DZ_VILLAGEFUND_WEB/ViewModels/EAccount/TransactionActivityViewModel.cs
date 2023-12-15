using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EAccount
{
    public class TransactionActivityViewModel
    {
        public Int64 TransactionAccActivityId { get; set; }
        public Int64 ProjectId { get; set; }
        public Int64 ActivityId { get; set; }
        public string VillageId { get; set; }
        public string AccChartName { get; set; }
        public int TransactionYear { get; set; }
        public bool AccountType { get; set; }
        public string TransactionType { get; set; }
        public string Receiver { get; set; }
        public decimal Amount { get; set; }
        public string ReceiverDate { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public string Detail { get; set; }
        public string IsFinish { get; set; }
        public decimal RealAmount { get; set; }
        public decimal TotalAmout { get; set; }
    }

    public class TransactionActivityFile
    {
        public Int64 TransactionFileId { get; set; }
        public Int64 TransactionAccAcsId { get; set; }
        public int TransactionYear { get; set; }
        public string FileName { get; set; }
        public string GencodeFileName { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public bool ApproverMark { get; set; }
    }
}
