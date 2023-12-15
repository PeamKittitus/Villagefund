using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public partial class SystemDistrict
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AmphurId { get; set; }
        public string AmphurCode { get; set; }
        public string AmphurName { get; set; }
        public int? GeoId { get; set; }
        public int? ProvinceId { get; set; }
    }
}
