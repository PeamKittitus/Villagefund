using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class AccountBookBank
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ProjectBbId { get; set; }
        public int OrgId { get; set; }
        public string BookBankId { get; set; }
        public string BookBankName { get; set; }
        public int BankCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public bool IsBrance { get; set; }
        public string WithdrawOfficeName { get; set; }
    }
}
