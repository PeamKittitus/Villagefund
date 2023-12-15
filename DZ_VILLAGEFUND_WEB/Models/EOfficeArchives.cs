using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeArchives
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ArchiveId { get; set; }
        public Int64 ArchiveOrgCode { get; set; }
        public int ArchiveNumber { get; set; }
        public int BudgetYear { get; set; }
        public string Dear { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string AttachFiles { get; set; }
        public Int64 OriginalOrgCode { get; set; }
        public string DestinationOrgCode { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateByPCode { get; set; }
        public DateTime? EditDate { get; set; }
        public string EditByPCode { get; set; }
        public bool IsDelete { get; set; }
        public DateTime? DeleteDate { get; set; }
        public string DeleteByPcode { get; set; }
        public string StatusCode { get; set; }
        public string FinalStatus { get; set; }
        public string TypeCode { get; set; }
        public DateTime RegisterDate { get; set; }
        public int AccessLevel { get; set; }
        public string Expedition { get; set; }
        public bool IsCirculation { get; set; }
        public string ExternalArchiveNumber { get; set; }
        public string ExternalOrgName { get; set; }
        public string ExternalCmdCode { get; set; }        
        public string Prefix { get; set; }
        public DateTime DateOfDoc { get; set; }



        public virtual ICollection<EOfficeArchivesRoutingTransaction> EOfficeArchivesRoutingTransaction { get; set; }
    }
}
