using DZ_VILLAGEFUND_WEB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Settings
{
    public class ListRoleViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public List<SystemPermission> Permisstion { get; set; }
    }

    public class GetRoleMenuViewModel
    {
        public int MenuId { get; set; }
        public int ParentId { get; set; }
        public string MenuName { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public int Position { get; set; }
        public string Icon { get; set; }
        public string OrgCode { get; set; }
        public bool IsFront { get; set; }
        public List<SystemRolemenus> SystemRolemenus { get; set; }
    }
}
