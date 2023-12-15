using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class TransactionFileVillage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionFileId { get; set; }
        public Int64 TransactionVillageId { get; set; }
        public int VillageId { get; set; }
        public int VillageCode { get; set; }
        public int TransactionYear { get; set; }
        public string FileName { get; set; }
        public string GencodeFileName { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public bool Approvemark { get; set; }
    }
}
