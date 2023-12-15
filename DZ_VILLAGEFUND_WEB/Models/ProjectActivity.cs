using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ProjectActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ProjectActivityId { get; set; }

        [ForeignKey("ProjectBudget")]
        public Int64 ProjectId { get; set; }
        public Int64 VillageId { get; set; }
        public int TransactionYear { get; set; }
        public string ActivityDetail { get; set; }
        public int Period { get; set; }
        public int StatusId { get; set; }
        public DateTime StartActivityDate { get; set; }
        public DateTime EndActivityDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime UpdateDate { get; set; }
        public string ActivityComment { get; set; }
        public DateTime ApproveDate { get; set; }
        public string ApproveBy { get; set; }
        public decimal ActivityBudget { get; set; }

    }
}
