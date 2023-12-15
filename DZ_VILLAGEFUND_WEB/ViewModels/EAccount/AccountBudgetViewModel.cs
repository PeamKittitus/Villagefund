using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class AccountBudgetViewModel
    {
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
        public string AccStart { get; set; }
        public string AccEndDate { get; set; }
        public decimal SubAmount { get; set;}
        public DateTime _AccStart { get; set; }
        public DateTime _AccEndDate { get; set; }
        public string DocumentFile { get; set; }

        public int LowActivity { get; set; }
        public int MidActivity { get; set; }
        public int HiActivity { get; set; }
        public int LowTiming { get; set; }
        public int MidTiming { get; set; }
        public int HiTiming { get; set; }

        public bool IsApproveProvince { get; set; }
        public bool IsApproveBranch { get; set; }
        public bool IsApproveCenter { get; set; }
    }

    
}
