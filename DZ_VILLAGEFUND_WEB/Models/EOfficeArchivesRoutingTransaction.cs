using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeArchivesRoutingTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 TransactionId { get; set; }
        [ForeignKey("EOfficeArchives")]
        public Int64 ArchiveId { get; set; }
        public Int64 FromOrgCode { get; set; }
        public Int64 ToOrgCode { get; set; }
        public DateTime SendDate { get; set; }
        public string SendByPCode { get; set; }
        public int BudgetYear { get; set; }
        public int? ReceiveNumber { get; set; }
        public DateTime? ReceiveDate { get; set; }
        public string ReceiveByPCode { get; set; }
        public string Comment { get; set; }
        public bool IsApprove { get; set; }
        public string CmdCode { get; set; }
        public string StatusCode { get; set; }
        public int IOrder { get; set; }
        public bool IsExternal { get; set; }
        public int Flag { get; set; }


        public virtual EOfficeArchives EOfficeArchives { get; set; }
    }
}
