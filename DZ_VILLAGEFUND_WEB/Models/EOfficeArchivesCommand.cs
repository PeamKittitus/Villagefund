using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeArchivesCommand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 Id { get; set; }
        public string CmdCode { get; set; }
        public string Name { get; set; }
        public int Flag1 { get; set; }
        public int Flag2 { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public bool Enable { get; set; }
    }
}
