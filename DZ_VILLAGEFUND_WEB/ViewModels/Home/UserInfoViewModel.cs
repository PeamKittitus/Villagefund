using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Home
{
    public class UserInfoViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string OrgName { get; set; }
        public string RoleName { get; set; }
    }

    public class ListMembers
    {
        public string  FullName { get; set; }
    }

    public class UsedLogs
    {
        public string FullName { get; set; }
        public string ActiveDate { get; set; }
        public string EventName { get; set; }
    }
}
