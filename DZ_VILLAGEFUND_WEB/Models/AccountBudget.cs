using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class AccountBudget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 AccountBudgetd { get; set; }
        public string AccName { get; set; }
        public int ParentId { get; set; }
        public int BudgetYear { get; set; }
        public string AccCode { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public string Qualification { get; set; }
        public DateTime AccStart { get; set; }
        public DateTime AccEndDate { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public decimal SubAmount { get; set; }
        public string DocumentFile { get; set; }
        public bool IsApproveProvince { get; set; }
        public bool IsApproveBranch { get; set; }
        public bool IsApproveCenter { get; set; }
    }
}
