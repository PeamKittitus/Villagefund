using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public partial class SystemTransactionType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionTypeId { get; set; }
        public string TransactionTypeNameTH { get; set; }
        public string TransactionTypeNameEN { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public int SystemGroup { get; set; }
    }
}
