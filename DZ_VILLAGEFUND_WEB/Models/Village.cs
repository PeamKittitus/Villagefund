using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;



namespace DZ_VILLAGEFUND_WEB.Models
{
    public class Village
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 VillageId { get; set; }
        public string UserId { get; set; }
        public int VillageCode { get; set; }
        public string VillageBbdId { get; set; }
        public string VillageName { get; set; }
        public string VillageAddress { get; set; }
        public int? VillageMoo { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int SubDistrictId { get; set; }
        public int PostCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime BbdDate { get; set; }
        public DateTime VillageStartDate { get; set; }
        public DateTime VillageEndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public int OrgId { get; set; }
        public string VillageCodeText { get; set; }
        public string VillageBbdCode { get; set; }
    }
}
