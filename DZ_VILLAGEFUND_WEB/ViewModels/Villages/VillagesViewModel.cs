using Microsoft.AspNetCore.Http;
using System;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Villages
{
    public class VillagesViewModel
    {
        public Int64 VillageId { get; set; }
        public Int64 TransactionVillageId { get; set; }
        public string UserId { get; set; }
        public int VillageCode { get; set; }
        public string VillageBbdId { get; set; }
        public string VillageName { get; set; }
        public string VillageAddress { get; set; }
        public int? VillageMoo { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int SubDistrictId { get; set; }
        public int PostCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string BbdDate { get; set; }
        public string ExpirydDate { get; set; }
        public string VillageStartDate { get; set; }
        public string VillageEndDate { get; set; }
        public bool? IsActive { get; set; }
        public string UpdateDate { get; set; }
        public string ApproveDate { get; set; }
        public DateTime _ApproveDate { get; set; }
        public string Address { get; set; }
        public string TransactionType { get; set; }
        public int TransactionTypeId { get; set; }
        public string StatusName { get; set; }
        public int StatusId { get; set; }
        public string VillageCodeText { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string SubDistrict { get; set; }
    }

    public class FormUpdateViewModel
    {
        public Int64 VillageId { get; set; }
        public Int64 TransactionVillageId { get; set; }
        public string UserId { get; set; }
        public int VillageCode { get; set; }
        public string VillageBbdId { get; set; }
        public string VillageName { get; set; }
        public string VillageAddress { get; set; }
        public int? VillageMoo { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int SubDistrictId { get; set; }
        public int PostCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime BbdDate { get; set; }
        public DateTime VillageStartDate { get; set; }
        public DateTime VillageEndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public int OrgId { get; set; }
        public string VillageCodeText { get; set; }
        public string VillageBbdCode { get; set; }

    }

    public class ReportViewModel
    {
        public string Year { get; set; }
        public string Month { get; set; }
        public string Province { get; set; }
        public string Name { get; set; }
        public string SubName { get; set; }
        public string Status { get; set; }
        public int Amount { get; set; }
        public int AmountReq { get; set; }
        public int AmountEdit { get; set; }
        public int AmountCancle { get; set; }
        public int AmountExp { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    public class ImpoertVillageViewModel
    {
        public string Password { get; set; }
        public IFormFile FileImport { get; set; }
    }
}
