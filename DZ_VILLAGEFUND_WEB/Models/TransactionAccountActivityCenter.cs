using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransactionAccountActivityCenter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 TransactionAccActivityCenterId { get; set; }
        public Int64 ProjectId { get; set; }
        public Int64 ActivityId { get; set; }
        public Int64 VillageId { get; set; }
        public Int64 AccChartId { get; set; }
        public Int64 BookBankId { get; set; }
        public int TransactionYear { get; set; }
        public int TransactionType { get; set; }
        public string Receiver { get; set; }
        public decimal Amount { get; set; }
        public DateTime ReceiverDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public string Detail { get; set; }
		public int LookupValueDivision { get; set; }
        public int LookupValueDepartment { get; set; }
        public bool IsFinish { get; set; }
        public decimal RealAmount { get; set; }
        public decimal TotalAmout { get; set; }
    }
}
