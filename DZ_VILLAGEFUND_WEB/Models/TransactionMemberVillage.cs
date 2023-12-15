using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{
    public partial class TransactionMemberVillage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MemberId { get; set; }
        public Int64 TransactionVillageId { get; set; }
        public Int64 VillageId { get; set; }
        public string MemberCode { get; set; }
        public string CitizenId { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public int GenderId { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
        public int MemberOccupation { get; set; }
        public string MemberAddress { get; set; }
        public int MemberPositionId { get; set; }
        public int MemberStatusId { get; set; }
        public DateTime MemberDate { get; set; } = new DateTime(1900, 1, 1);
        public DateTime UpdateDate { get; set; } = new DateTime(1900, 1, 1);
        public string UpdateBy { get; set; }
        public DateTime MemberBirthDate { get; set; } = new DateTime(1900, 1, 1);
        public string KickOffComment { get; set; }
        public bool MemberRenewal { get; set; }
        public DateTime MemberEndDate { get; set; } = new DateTime(1900, 1, 1);
        public string Connection { get; set; }
        public string MemberOccupationOther { get; set; }
        public bool NoCitizenId { get; set; }
        public bool NoBirthDate { get; set; }
        public string UserId { get; set; }
    }
}
