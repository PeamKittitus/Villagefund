using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ProjectRisk
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ProjectRiskId { get; set; }
        public Int64 AccountBudgetId { get; set; }
        public int BudgetYear { get; set; }
        public int LowActivity { get; set; }
        public int MidActivity { get; set; }
        public int HiActivity { get; set; }
        public int LowTiming { get; set; }
        public int MidTiming { get; set; }
        public int HiTiming { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
    }
}
