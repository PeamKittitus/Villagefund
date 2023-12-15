using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransacionAccountProject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 TransactionAccProjectId { get; set; }
        public Int64 AccountBudgetd { get; set; }
        public Int64 ProjectId { get; set; }
        public int ProjectRefId { get; set; }
        public Int64 VillageId { get; set; }
        public int TransactionPeriod { get; set; }
        public int TransactionYear { get; set; }
        public bool TransactionType { get; set; }
        public string Sender { get; set; }
        public int SenderOrgId { get; set; }
        public string Receiver { get; set; }
        public decimal Amount { get; set; }
        public DateTime SenderDate { get; set; }
        public DateTime ReceiverDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
    }
}
