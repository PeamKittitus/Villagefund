using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public partial class TransactionReqVillage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 TransactionVillageId { get; set; }
        public string UserId { get; set; }
        public int TransactionType { get; set; }
        public int TransactionYear { get; set; }
        public string TransactionDetail { get; set; }
        public string TransactionEdit { get; set; }
        public int StatusId { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime ApproveDate { get; set; }
        public string ApproveBy { get; set; }
        public int? OrgId { get; set; }
        public bool IsAlreadyCheck { get; set; }
    }
}
