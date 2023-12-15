using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransactionFilePeriod
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 FilePeriodId { get; set; }
        public Int64 ProjectPeriodId { get; set; }
        public int TransactionYear { get; set; }
        public string FileName { get; set; }
        public string GencodeFileName { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public bool Approvemark { get; set; }
    }
}
