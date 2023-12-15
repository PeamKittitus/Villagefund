using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EOffice
{
    public class ArchiveViewDetailViewModel
    {
        public string CreateBy { get; set; }
        public string CreateDate { get; set; }
        public string DateOfdoc { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public string ArchiveNumber { get; set; }
        public string ExternalArchiveNumber { get; set; }
        public string AssesLevelName { get; set; }
        public string ExpedotionName { get; set; }
        public int DocumentYear { get; set; }
        public string Dear { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int ReceiveNumber { get; set; }
    }

    public class Transaction
    {
        public string StatusId { get; set; }
        public string Status { get; set; }
        public string OrgShortName { get; set; }
        public string OrgtName { get; set; }
    }

    public class TransactionDetails
    {
        public int ReceiveNumber { get; set; }
        public string ToOrg { get; set; }
        public string ReceiveUserName { get; set; }
        public string Comment { get; set; }
        public string SendDate { get; set; }
        public string ReceiveDate { get; set; }
        public string StatusName { get; set; }
        public string StatusCode { get; set; }
    }

    public class ArchiveFiles
    {
        public Int64 Id { get; set; }
        public string FileName { get; set; }
        public string TitleFileName { get; set; }
    }
}
