using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransacionAccountBudget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 TransactionAccBudgeId { get; set; }
        public Int64 AccountBudgetd { get; set; }
        public string Title { get; set; }
        public int TransactionYear { get; set; }
        public bool TransactionType { get; set; }
        public Int64 SenderBookBankId { get; set; }
        public int SenderOrgId { get; set; }
        public Int64 ReceiverBookBankId { get; set; }
        public int ReceiverOrgId { get; set; }
        public decimal Amount { get; set; }
        public DateTime SenderDate { get; set; }
        public DateTime ReceiverDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
    }
}
