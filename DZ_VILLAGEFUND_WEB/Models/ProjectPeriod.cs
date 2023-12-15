using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ProjectPeriod
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ProjectPeriodId { get; set; }
        public Int64 AccBudgetId { get; set; }
        public int TransactionYear { get; set; }
        public string PeriodName { get; set; }
        public decimal PeriodPercent { get; set; }
        public decimal PeriodAmount { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime PeriodDate { get; set; }
        public string PeriodComment { get; set; }
    }
}
