using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransactionNews
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionNewsId { get; set; }
        public string UserId { get; set; }
        public bool TransactionType { get; set; }
        public int TransactionYear { get; set; }
        public int LookupId { get; set; }
        public string TransactionBy { get; set; }
        public string TransactionTitle { get; set; }
        public string TransactionDetail { get; set; }
        public DateTime NewsStartDate { get; set; }
        public DateTime NewsEndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsApprove { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }

    }
}
