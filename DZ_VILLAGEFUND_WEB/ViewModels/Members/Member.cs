using System;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Members
{
    public class Member
    {
        public int MemberId { get; set; }
        public Int64 VillageId { get; set; }
        public string MemberCode { get; set; }
        public string CitizenId { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
        public string MemberOccupation { get; set; }
        public string MemberAddress { get; set; }
        public string MemberPosition { get; set; }
        public string MemberStatus { get; set; }
        public string MemberStartDate { get; set; }
        public string MemberEndDate { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public string FullName { get; set; }
        public string MemberBirthDate { get; set; }
        public string Remark { get; set; }
        public string Connection { get; set; }
    }
}
