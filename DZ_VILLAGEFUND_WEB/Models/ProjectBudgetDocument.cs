using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ProjectBudgetDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 TransactionDocId { get; set; }
        public Int64 ProjectId { get; set; }
        public Int64 ProjectPeriodId { get; set; }
        public int TransactionYear { get; set; }
        public string FileName { get; set; }
        public string GencodeFileName { get; set; }
        public int FileType { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public bool Approvemark { get; set; }
    }
}
