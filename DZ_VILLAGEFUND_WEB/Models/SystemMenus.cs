using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class SystemMenus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string MenuName { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public int ParentId { get; set; }
        public int Position { get; set; }
        public string Icon { get; set; }
        public string ImageMenu { get; set; }
        public bool IsLink { get; set; }
        public string PathLink { get; set; }


    }
}
