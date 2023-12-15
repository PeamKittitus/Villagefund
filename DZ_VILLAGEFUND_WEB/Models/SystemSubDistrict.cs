using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public partial class SystemSubDistrict
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubdistrictId { get; set; }
        public string SubdistrictCode { get; set; }
        public string SubdistrictName { get; set; }
        public int? AmphurId { get; set; }
        public int? ProvinceId { get; set; }
        public int? GeoId { get; set; }
        public string PostCode { get; set; }
    }
}
