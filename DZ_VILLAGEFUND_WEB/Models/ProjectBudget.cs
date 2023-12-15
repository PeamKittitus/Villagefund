using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ProjectBudget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ProjectId { get; set; }
        public Int64 ParentId { get; set; }
        public Int64 VillageId { get; set; }
        public Int64 AccountBudgetd { get; set; }
        public int OrgId { get; set; }
        public string ProjectCode { get; set; }
        public int TransactionYear { get; set; }
        public string ProjectName { get; set; }
        public string ProjectComment { get; set; }
        public decimal Amount { get; set; }
        public int Period { get; set; }
        public int StatusId { get; set; }
        public DateTime SignProjectDate { get; set; }
        public DateTime StartProjectDate { get; set; }
        public DateTime EndProjectDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime ApproveDate { get; set; }
        public string ApproveBy { get; set; }

    }
}
