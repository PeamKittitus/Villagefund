using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ProjectAsset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ProjectAssetId { get; set; }
        public Int64 ProjectId { get; set; }
        public Int64 VillageId { get; set; }
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public int StatusId { get; set; }
        public int AssetAge { get; set; }
        public decimal Amount { get; set; }
        public int AmountUnit { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
    }
}
