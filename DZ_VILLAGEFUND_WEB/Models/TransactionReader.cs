using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransactionReader
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionFileId { get; set; }
        public int TransactionNewsId { get; set; }
        public string UserId { get; set; }
        public int TransactionYear { get; set; }
        public bool IsRead { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
    }
}
