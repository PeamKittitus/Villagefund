using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeArchivesAccesslevel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 Id { get; set; }
        public int AccessLevel { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Comment { get; set; }
        public bool Enable { get; set; }
    }
}
