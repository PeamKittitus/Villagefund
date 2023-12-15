using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class EOfficeArchivesFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 FileID { get; set; }
        public string FileName { get; set; }
        public Int64 ArchiveId { get; set; }
        public string FileTitle { get; set; }

    }
}
