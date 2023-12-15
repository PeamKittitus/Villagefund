using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeArchivesStatus
    {
        public Int64 Id { get; set; }
        public string StatusCode { get; set; }
        public string Name { get; set; }
        public int Flag1 { get; set; }
        public int Flag2 { get; set; }
        public bool IsFinnish { get; set; }
        public bool Enable { get; set; }

    }
}
