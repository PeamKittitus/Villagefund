using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
   
    public partial class SystemMemberStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StatusId { get; set; }
        public string StatusNameTH { get; set; }
        public string StatusNameEN { get; set; }

    }
}
