using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public partial class SystemLookupMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LookupId { get; set; }
        public int LookupValue { get; set; }
        public string LookupText { get; set; }
        public bool LookupStatus { get; set; }
        public int LookupGroupId { get; set; }
        public string LookupGroupName { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime UpdateDate { get; set; }


    }
}
