using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string OTP { get; set; }
        public DateTime OTPExpiryDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CitizenId { get; set; }
        public int OrgId { get; set; }
        public Int64 VillageId { get; set; }
        public bool Status { get; set; }
        public int LookupValueDepartment { get; set; }
        public int LookupValueDivision { get; set; }
        public int MemberId { get; set; }
    }
}
