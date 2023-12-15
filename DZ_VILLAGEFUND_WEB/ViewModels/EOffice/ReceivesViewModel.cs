using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EOffice
{
    public class ReceivesViewModel
    {
        public Int64 ArchiveId { get; set; }
        public int ReceiveNumber { get; set; }
        public string ArchiveNumber { get; set; }
        public string CreateDate { get; set; }
        public string FromOrg { get; set; }
        public string ToOrg { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string StatusCode { get; set; }
    }
}
