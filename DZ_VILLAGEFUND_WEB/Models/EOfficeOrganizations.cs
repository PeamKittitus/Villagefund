using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeOrganizations
    {
       [Key]
       [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 OrgId { get; set; }
        public Int64 OrgMomId { get; set; }
        public Int64 ParentId { get; set; }
        public int Position { get; set; }
        public string OrgName { get; set; }
        public string OrgShortName { get; set; }
        public string OrgNumber { get; set; }
    }
}
