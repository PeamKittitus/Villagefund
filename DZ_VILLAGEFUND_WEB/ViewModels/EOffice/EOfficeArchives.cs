using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EOffice
{
    public class EOfficeArchives
    {
        public Int64 ArchiveId { get; set; }
        public Int64 ArchiveOrgCode { get; set; }
        public string ArchiveNumber { get; set; }
        public int BudgetYear { get; set; }
        public string Dear { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string AttachFiles { get; set; }
        public Int64 OriginalOrgCode { get; set; }
        public string DestinationOrgCode { get; set; }
        public string CreateDate { get; set; }
        public string CreateByPCode { get; set; }
        public string EditDate { get; set; }
        public string EditByPCode { get; set; }
        public bool IsDelete { get; set; }
        public string DeleteDate { get; set; }
        public string DeleteByPcode { get; set; }
        public string StatusCode { get; set; }
        public string FinalStatus { get; set; }
        public string TypeCode { get; set; }
        public string RegisterDate { get; set; }
        public string AccessLevel { get; set; }
        public string Expedition { get; set; }
        public bool IsCirculation { get; set; }
        public string ExternalArchiveNumber { get; set; }
        public string ExternalOrgName { get; set; }
        public string ExternalCmdCode { get; set; }
        public string Prefix { get; set; }
        public string DateOfDoc { get; set; }
        public string FromOrg { get; set; }
        public string ToOrg { get; set; }
        public int Record { get; set; }

        //Transaction
        public Int64 FromOrgCode { get; set; }
        public Int64 ToOrgCode { get; set; }
        public DateTime SendDate { get; set; }
        public string SendByPCode { get; set; }
        public int ReceiveNumber { get; set; }
        public DateTime ReceiveDate { get; set; }
        public string ReceiveByPCode { get; set; }
        public string Comment { get; set; }
        public bool IsApprove { get; set; }
        public string CmdCode { get; set; }
        public int IOrder { get; set; }
        public bool IsExternal { get; set; }
        public int Flag { get; set; }
    }

    public class ForwardViewModel
    {
        [Required]
        [Display(Name = "OrgName")]
        public string OrgName { get; set; }
    }
}
