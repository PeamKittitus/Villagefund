using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace DZ_VILLAGEFUND_WEB.Models
{

    public partial class MemberVillage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MemberId { get; set; }
        public long VillageId { get; set; }
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
        public DateTime MemberStartDate { get; set; }
        public DateTime MemberEndDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime MemberBirthDate { get; set; }
        public string KickOffComment { get; set; }
        public bool MemberRenewal { get; set; }
        public string Connection { get; set; }
        public string MemberOccupationOther { get; set; }
        public bool NoCitizenId { get; set; }
        public bool NoBirthDate { get; set; }
        public string UserId { get; set; }
        public virtual Village Village { get; set; }
    }
}
