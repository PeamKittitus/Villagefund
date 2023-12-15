using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Settings
{
    public class ListMenusViewModel
    {
        public int id { get; set; }
        public List<SubItem> children { get; set; }
    }


    [Serializable]
    public class SubItem
    {
        public int id { get; set; }
        public List<SubItem2> children { get; set; }
    }

    [Serializable]
    public class SubItem2
    {
        public int id { get; set; }
        public List<SubItem3> children { get; set; }
    }

    [Serializable]
    public class SubItem3
    {
        public int id { get; set; }
        public List<SubItem4> children { get; set; }
    }

    [Serializable]
    public class SubItem4
    {
        public int id { get; set; }
    }

    [Serializable]
    public class SubItem5
    {
        public int id { get; set; }
    }

}
