using System;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Villages
{
    public class AdminVillageView
    {
    }
    public class AdminVillageViewIndex
    {
        public Int64 TransactionVillageId { get; set; }
        public Int64 VillageId { get; set; }
        public int VillageCode { get; set; }
        public string VillageName { get; set; }
        public string VillageBbdId { get; set; }
        public int TransactionType { get; set; }
        public int StatusId { get; set; }
        public string VillageCodeText { get; set; }

        //Member
        public string President { get; set; }
    }
}
