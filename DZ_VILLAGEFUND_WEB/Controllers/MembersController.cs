using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.Members;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using DZ_VILLAGEFUND_WEB.ViewModels.Villages;
using System.Linq;
using DZ_VILLAGEFUND_WEB.ViewModels.DateCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AutoMapper.Execution;
using Microsoft.Extensions.Configuration;
using DZ_VILLAGEFUND_WEB.ViewModels.News;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using System.IO;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class MembersController : Controller
    {
        private readonly ILogger<VillagesController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;

        public MembersController(
        ILogger<VillagesController> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IHostingEnvironment environment,
        ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            DB = db;
            _configuration = configuration;
            _environment = environment;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }

        [HttpGet]
        public IActionResult Index(int VillageId)
        {
            ViewBag.VillageId = VillageId;
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> AddVillageMember(string VillageCode)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetReq = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.TransactionVillageId).FirstOrDefault();
            ViewBag.TransactionVillageId = GetReq.TransactionVillageId;
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.VillageCode = VillageCode;
            return View("AddVillageMember");
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers(Int64 TransactionVillageId, Int64 VillageId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            List<ViewModels.Members.Member> Members = new List<ViewModels.Members.Member>();
            var CheckRecord = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Count();
            if (CheckRecord == 0)
            {
                var RealMemberDatas = DB.MemberVillage.Where(w => w.VillageId == VillageId).ToList();
                foreach (var GetMember in RealMemberDatas)
                {
                    var TranMember = new TransactionMemberVillage();
                    TranMember.VillageId = GetMember.VillageId;
                    TranMember.CitizenId = GetMember.CitizenId;
                    TranMember.MemberFirstName = GetMember.MemberFirstName;
                    TranMember.MemberLastName = GetMember.MemberLastName;
                    TranMember.GenderId = GetMember.GenderId;
                    TranMember.Age = GetMember.Age;
                    TranMember.Phone = GetMember.Phone;
                    TranMember.MemberOccupation = GetMember.MemberOccupation;
                    TranMember.MemberAddress = GetMember.MemberAddress;
                    TranMember.MemberPositionId = GetMember.MemberPositionId;
                    TranMember.MemberStatusId = GetMember.MemberStatusId;
                    TranMember.MemberDate = GetMember.MemberStartDate;
                    TranMember.UpdateDate = DateTime.Now;
                    TranMember.UpdateBy = GetMember.UpdateBy;
                    TranMember.MemberBirthDate = GetMember.MemberBirthDate;
                    TranMember.TransactionVillageId = TransactionVillageId;
                    TranMember.MemberCode = GetMember.MemberCode;
                    TranMember.MemberEndDate = GetMember.MemberEndDate;
                    TranMember.KickOffComment = GetMember.KickOffComment;
                    TranMember.Connection = GetMember.Connection;
                    DB.TransactionMemberVillage.Add(TranMember);
                    DB.SaveChanges();
                }

                var GetMembers = await DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToListAsync();
                foreach (var item in GetMembers)
                {
                    var Member = new ViewModels.Members.Member();
                    Member.MemberId = item.MemberId;
                    Member.MemberCode = item.MemberCode;
                    Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                    Member.MemberPosition = DB.SystemMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                    Member.Remark = item.KickOffComment;
                    Member.Connection = item.Connection;

                    Members.Add(Member);
                }
            }
            else
            {
                var GetMembers = await DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToListAsync();
                foreach (var item in GetMembers)
                {
                    var Member = new ViewModels.Members.Member();
                    Member.MemberId = item.MemberId;
                    Member.MemberCode = item.MemberCode;
                    Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                    Member.MemberPosition = DB.SystemMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                    Member.MemberStatus = DB.SystemMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                    Member.Remark = item.KickOffComment;
                    Member.Connection = item.Connection;
                    Members.Add(Member);
                }
            }

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView(Members);
        }

        [HttpGet]
        public IActionResult FormAddMember(int MemberId, Int64 TransactionVillageId, Int64 VillageId)
        {
            //Make Gender Selection
            String[] ThaiGender = { "ชาย", "หญิง", "ทางเลือก" };
            var Gender = new List<Gender>();
            int GenderId = 0;
            foreach (var item in ThaiGender)
            {
                var GenderViewModel = new Gender();
                GenderViewModel.GenderId = GenderId;
                GenderViewModel.GenderName = item;
                GenderId++;
                Gender.Add(GenderViewModel);
            }
            ViewBag.Gender = new SelectList(Gender, "GenderId", "GenderName");

            //Make MemberStatusIds
            ViewBag.MemberStatusIds = new SelectList(DB.SystemMemberStatus, "StatusId", "StatusNameTH");

            ViewBag.MemberPosition = new SelectList(DB.SystemMemberPosition.ToList(), "PositionId", "PositionNameTH");

            //Load Member Data For Edit
            var GetTransMember = DB.TransactionMemberVillage.Where(w => w.MemberId == MemberId).FirstOrDefault();

            //Gen Day Selection            
            ViewBag.Day = new SelectList(new List<DateCollection>(), "Day", "Day");
            //Gen month and Month selection
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            var Months = new List<SelectListItem>();
            for (int m = 0; m < MonthName.Length; m++)
            {

                Months.Add(new SelectListItem
                {
                    Text = MonthName[m],
                    Value = m.ToString()
                });

            }
            ViewBag.Month = new SelectList(Months, "Value", "Text");
            //Gen Year and Year Selection
            var Years = new List<SelectListItem>();
            for (int y = DateTime.Now.Year; y > (DateTime.Now.Year - 100); y--)
            {
                Years.Add(new SelectListItem
                {
                    Text = (y + 543).ToString(),
                    Value = y.ToString()
                });
            }
            ViewBag.Year = new SelectList(Years, "Value", "Text");


            ViewBag.TransactionVillageId = TransactionVillageId;
            ViewBag.VillageId = VillageId;

            var OccuptionsDB = DB.SystemOccupationMaster.ToList();
            OccuptionsDB.Add(new SystemOccupationMaster { Id = 0, Name = "อาชีพอื่นๆ" });
            ViewBag.Occuption = new SelectList(OccuptionsDB, "Id", "Name");

            ViewBag.MemberCode = DB.Village.Where(w => w.VillageId == VillageId).Select(s => s.VillageCodeText + DB.MemberVillage.Where(w => w.VillageId == VillageId).Count().ToString("D3")).FirstOrDefault();

            return View("FormAddMember", GetTransMember);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(TransactionMemberVillage Member, DateCollection Date)
        {
            try
            {
                //Provide Object
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                var GetTransReq = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.TransactionVillageId).FirstOrDefault();

                //Check ประธาน 1 คน
                if (DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == GetTransReq.TransactionVillageId && w.MemberPositionId == 1).Count() == 1 && Member.MemberPositionId == 1)
                {
                    return Json(new { valid = false, message = "ประธานไม่เกิน 1 คน" });
                }

                //Check กรรมการ 9 ถึง 15 คน
                if (DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == GetTransReq.TransactionVillageId && w.MemberPositionId == 9 && Member.MemberPositionId == 9).Count() > 15)
                {
                    return Json(new { valid = false, message = "กรรมการหมู่บ้าน จะต้องมีสมาชิก 9 - 15 คน" });
                }

                //Add Member
                Member.TransactionVillageId = Member.TransactionVillageId;
                Member.MemberDate = DateTime.Now;
                Member.UpdateDate = DateTime.Now;
                Member.UpdateBy = CurrentUser.Id;
                Member.MemberBirthDate = new DateTime(Date.Year, Date.Month, Date.Day);
                Member.MemberEndDate = Member.MemberStatusId == 2 ? Member.MemberEndDate : DateTime.Now.AddYears(2);
                DB.TransactionMemberVillage.Add(Member);
                DB.SaveChanges();

                //Response OK
                string msg = "บันทึกข้อมูลสำเร็จ";
                return Json(new { valid = true, message = msg });

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMember(TransactionMemberVillage Member, DateCollection Date)
        {
            try
            {
                //Provide Object
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                //Edit
                var GetTransMember = DB.TransactionMemberVillage.Where(w => w.MemberId == Member.MemberId).FirstOrDefault();
                GetTransMember.CitizenId = Member.CitizenId;
                GetTransMember.MemberFirstName = Member.MemberFirstName;
                GetTransMember.MemberLastName = Member.MemberLastName;
                GetTransMember.GenderId = Member.GenderId;
                GetTransMember.Age = Member.Age;
                GetTransMember.Phone = Member.Phone;
                GetTransMember.MemberOccupation = Member.MemberOccupation;
                GetTransMember.MemberAddress = Member.MemberAddress;
                GetTransMember.MemberPositionId = Member.MemberPositionId;
                GetTransMember.MemberStatusId = Member.MemberStatusId;
                GetTransMember.MemberDate = DateTime.Now;
                GetTransMember.UpdateDate = DateTime.Now;
                GetTransMember.UpdateBy = CurrentUser.Id;
                GetTransMember.MemberBirthDate = new DateTime(Date.Year, Date.Month, Date.Day);
                GetTransMember.KickOffComment = Member.KickOffComment;
                GetTransMember.Connection = Member.Connection;
                GetTransMember.MemberEndDate = Member.MemberStatusId == 2 ? Member.MemberEndDate : GetTransMember.MemberEndDate;
                GetTransMember.MemberOccupation = Member.MemberOccupation;
                GetTransMember.MemberOccupationOther = Member.MemberOccupationOther;
                GetTransMember.NoCitizenId = Member.NoCitizenId;
                GetTransMember.NoBirthDate = Member.NoBirthDate;
                DB.TransactionMemberVillage.Update(GetTransMember);
                DB.SaveChanges();

                //Response OK
                string msg = "บันทึกข้อมูลสำเร็จ";
                return Json(new { valid = true, message = msg });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FormAddMemberVillage(Int64 TransactionVillageId, string VillageCode)
        {
            //User Data
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            //Make Gender Selection
            String[] ThaiGender = { "ชาย", "หญิง", "ทางเลือก" };
            var Gender = new List<Gender>();
            int GenderId = 0;
            foreach (var item in ThaiGender)
            {
                var GenderViewModel = new Gender();
                GenderViewModel.GenderId = GenderId;
                GenderViewModel.GenderName = item;
                GenderId++;
                Gender.Add(GenderViewModel);
            }
            ViewBag.Gender = new SelectList(Gender, "GenderId", "GenderName");

            //Make MemberStatusIds
            ViewBag.MemberStatusIds = new SelectList(DB.SystemMemberStatus, "StatusId", "StatusNameTH");

            ViewBag.MemberPosition = new SelectList(DB.SystemMemberPosition.ToList(), "PositionId", "PositionNameTH");


            //Gen Day Selection            
            ViewBag.Day = new SelectList(new List<DateCollection>(), "Day", "Day");
            //Gen month and Month selection
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            var Months = new List<SelectListItem>();
            for (int m = 0; m < MonthName.Length; m++)
            {

                Months.Add(new SelectListItem
                {
                    Text = MonthName[m],
                    Value = m.ToString()
                });

            }
            ViewBag.Month = new SelectList(Months, "Value", "Text");
            //Gen Year and Year Selection
            var Years = new List<SelectListItem>();
            for (int y = DateTime.Now.Year; y > (DateTime.Now.Year - 100); y--)
            {
                Years.Add(new SelectListItem
                {
                    Text = (y + 543).ToString(),
                    Value = y.ToString()
                });
            }
            ViewBag.Year = new SelectList(Years, "Value", "Text");
            ViewBag.TransactionVillageId = TransactionVillageId;

            var OccuptionsDB = DB.SystemOccupationMaster.ToList();
            OccuptionsDB.Add(new SystemOccupationMaster { Id = 0, Name = "อาชีพอื่นๆ" });
            ViewBag.Occuption = new SelectList(OccuptionsDB, "Id", "Name");

            if (TransactionVillageId == 0)
            {
                ViewBag.TransactionVillageId = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.TransactionVillageId).Select(s => s.TransactionVillageId).FirstOrDefault();
                TransactionVillageId = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.TransactionVillageId).Select(s => s.TransactionVillageId).FirstOrDefault();
            }

            ViewBag.MemberCode = VillageCode + (DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Count() + 1).ToString("D3");

            return View("FormAddMemberVillage");
        }

        [HttpGet]
        public IActionResult FormEditMember(int MemberId)
        {

            var GetMember = DB.TransactionMemberVillage.Where(w => w.MemberId == MemberId).FirstOrDefault();
            ViewBag.CheckVillageId = false;
            if (GetMember.VillageId != 0)
            {
                ViewBag.CheckVillageId = true;
            }

            //Make Gender Selection
            String[] ThaiGender = { "ชาย", "หญิง", "ทางเลือก" };
            var Gender = new List<Gender>();
            int GenderId = 0;
            foreach (var item in ThaiGender)
            {
                var GenderViewModel = new Gender();
                GenderViewModel.GenderId = GenderId;
                GenderViewModel.GenderName = item;
                GenderId++;
                Gender.Add(GenderViewModel);
            }
            ViewBag.Gender = new SelectList(Gender, "GenderId", "GenderName");

            //Make MemberStatusIds
            ViewBag.MemberStatusIds = new SelectList(DB.SystemMemberStatus, "StatusId", "StatusNameTH");

            ViewBag.MemberPosition = new SelectList(DB.SystemMemberPosition.ToList(), "PositionId", "PositionNameTH");

            //Provide Data
            var TransMemberModel = new TransactionMemberVillage();
            var GetTransMember = DB.TransactionMemberVillage.Where(w => w.MemberId == MemberId).FirstOrDefault();
            TransMemberModel = GetTransMember;

            //Gen Day Selection            
            ViewBag.Day = new SelectList(new List<DateCollection>(), "Day", "Day");
            //Gen month and Month selection
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            var Months = new List<SelectListItem>();
            for (int m = 0; m < MonthName.Length; m++)
            {

                Months.Add(new SelectListItem
                {
                    Text = MonthName[m],
                    Value = m.ToString()
                });

            }
            ViewBag.Month = new SelectList(Months, "Value", "Text");
            //Gen Year and Year Selection
            var Years = new List<SelectListItem>();
            for (int y = DateTime.Now.Year; y > (DateTime.Now.Year - 400); y--)
            {
                Years.Add(new SelectListItem
                {
                    Text = (y + 543).ToString(),
                    Value = y.ToString()
                });
            }
            ViewBag.Year = new SelectList(Years, "Value", "Text");

            ViewBag.KickOffComment = TransMemberModel.KickOffComment;

            var OccuptionsDB = DB.SystemOccupationMaster.ToList();
            OccuptionsDB.Add(new SystemOccupationMaster { Id = 0, Name = "อาชีพอื่นๆ" });
            ViewBag.Occuption = new SelectList(OccuptionsDB, "Id", "Name");

            return View(TransMemberModel);
        }

        [HttpGet]
        public IActionResult Delete(int MemberId)
        {
            try
            {
                var GetTransMember = DB.TransactionMemberVillage.Where(w => w.MemberId == MemberId).FirstOrDefault();
                DB.TransactionMemberVillage.Remove(GetTransMember);
                DB.SaveChanges();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult ViewsMember(int VillageId)
        {

            ViewBag.VillageId = VillageId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetViewsMember(int VillageId)
        {

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);

            var Gets = UserRole == "HeadQuarterAdmin" ? DB.MemberVillage.Where(w => w.VillageId == VillageId && w.MemberRenewal == true).OrderBy(o => o.MemberId).ToList() :
                DB.MemberVillage.Where(w => w.VillageId == VillageId && w.MemberRenewal == false).OrderBy(o => o.MemberId).ToList();

            ViewBag.Helper = Helper;
            ViewBag.PositionName = DB.SystemMemberPosition.ToList();

            return PartialView(Gets);
        }

        [HttpPut]
        public async Task<IActionResult> MemberRenewal(int MemberId)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                string UserRole = Helper.GetRoleUser(CurrentUser.Id);

                var Get = DB.MemberVillage.Where(w => w.MemberId == MemberId).FirstOrDefault();

                if (Get != null)
                {

                    if (UserRole == "HeadQuarterAdmin")
                    {
                        Get.MemberRenewal = false;
                        Get.MemberEndDate = Get.MemberEndDate.AddYears(2);
                        DB.MemberVillage.Update(Get);
                        DB.SaveChanges();

                        return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });

                    }

                    Get.MemberRenewal = true;
                    DB.MemberVillage.Update(Get);
                    DB.SaveChanges();

                    // add new TransactionReqVillage
                    var ReqModel = new TransactionReqVillage();
                    ReqModel.UserId = CurrentUser.Id;
                    ReqModel.TransactionType = 2;
                    ReqModel.TransactionYear = (DateTime.Now.Year + 543);
                    ReqModel.TransactionDetail = null;
                    ReqModel.TransactionEdit = null;
                    ReqModel.StatusId = 0;
                    ReqModel.UpdateBy = CurrentUser.Id;
                    ReqModel.UpdateDate = DateTime.Now;
                    ReqModel.ApproveBy = null;
                    ReqModel.ApproveDate = DateTime.Now;
                    ReqModel.OrgId = CurrentUser.OrgId;
                    DB.TransactionReqVillage.Add(ReqModel);
                    await DB.SaveChangesAsync();

                }

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        #region

        [HttpGet]
        public IActionResult FormImportMember(Int64 TransactionVillageId)
        {

            ViewBag.TransactionVillageId = TransactionVillageId;

            return PartialView("FormImportMember");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportMember(IFormFile FileUpload)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                if (FileUpload.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var Excel = new ExcelPackage(FileUpload.OpenReadStream()))
                    {
                        string[] fileNames = FileUpload.FileName.Split('_');
                        ExcelWorksheet worksheet = Excel.Workbook.Worksheets.First();
                        if (worksheet.Dimension.End.Row == 0)
                        {
                            return Json(new { valid = false, message = "ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                        }


                        for (int Row = 3; Row <= worksheet.Dimension.End.Row; Row++)
                        {

                            if (worksheet.Cells["A" + Row].Value == null)
                            {
                                break;
                            }

                            var Model = new TransactionMemberVillage();
                            Model.TransactionVillageId = Convert.ToInt64(worksheet.Cells["S3"].Value.ToString());
                            Model.MemberCode = worksheet.Cells["A" + Row].Value.ToString();
                            Model.CitizenId = worksheet.Cells["B" + Row].Value.ToString();
                            string[] Name = worksheet.Cells["C" + Row].Value.ToString().Split();
                            Model.MemberFirstName = Name[0];
                            Model.MemberLastName = Name[1];
                            Model.GenderId = Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString()));
                            Model.MemberBirthDate = DateTime.Parse(worksheet.Cells["E" + Row].Value.ToString()).AddYears(-543);
                            Model.Phone = worksheet.Cells["F" + Row].Value.ToString();
                            Model.MemberOccupation = worksheet.Cells["G" + Row].GetValue<int>();
                            Model.MemberAddress = worksheet.Cells["H" + Row].Value.ToString();
                            Model.MemberPositionId = Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["I" + Row].Value.ToString()));
                            Model.MemberStatusId = Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["J" + Row].Value.ToString()));
                            Model.KickOffComment = worksheet.Cells["K" + Row].Value == null ? null : worksheet.Cells["K" + Row].Value.ToString();
                            Model.UpdateDate = DateTime.Now;
                            Model.UpdateBy = CurrentUser.Id;
                            Model.MemberDate = DateTime.Now;
                            Model.MemberEndDate = DateTime.Now.AddYears(2);

                            Model.VillageId = CurrentUser.VillageId;

                            DB.TransactionMemberVillage.Add(Model);
                            DB.SaveChanges();

                        }
                    }
                }
                else
                {
                    return Json(new { valid = false, message = " ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult ExportFormImportMember(Int64 TransactionVillageId)
        {

            try
            {
                //Master

                //Export Excel
                var templateFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/member/template/FormImportMemberTemplate.xlsx");
                FileInfo templateFile = new FileInfo(templateFilePath);
                var newFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/member/export/FormImportMemberTemplate.xlsx");
                FileInfo newFile = new FileInfo(newFilePath);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage package = new ExcelPackage(newFile, templateFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    int RowStart = 3;

                    //

                    worksheet.Cells["S3"].Value = TransactionVillageId;

                    foreach (var item in DB.SystemMemberPosition.ToList())
                    {
                        worksheet.Cells["U" + RowStart].Value = "[" + item.PositionId + "]" + item.PositionNameTH;
                        worksheet.Cells["U" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }
                    RowStart = 3;

                    foreach (var item in DB.SystemMemberStatus.ToList())
                    {
                        worksheet.Cells["V" + RowStart].Value = "[" + item.StatusId + "]" + item.StatusNameTH;
                        worksheet.Cells["V" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }

                    package.Save();
                    var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return new FileStreamResult(new FileStream(newFilePath, FileMode.Open), mimeType);
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        #endregion

        #region สำหรับสามาชิกกองทุนหมู่บ้าน

        [HttpGet]
        public async Task<IActionResult> FormEditMemberByCurrentUser()
        {

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var GetMember = DB.TransactionMemberVillage.Where(w => w.MemberId == CurrentUser.MemberId).FirstOrDefault();
            ViewBag.CheckVillageId = false;
            if (GetMember.VillageId != 0)
            {
                ViewBag.CheckVillageId = true;
            }

            //Make Gender Selection
            String[] ThaiGender = { "ชาย", "หญิง", "ทางเลือก" };
            var Gender = new List<Gender>();
            int GenderId = 0;
            foreach (var item in ThaiGender)
            {
                var GenderViewModel = new Gender();
                GenderViewModel.GenderId = GenderId;
                GenderViewModel.GenderName = item;
                GenderId++;
                Gender.Add(GenderViewModel);
            }
            ViewBag.Gender = new SelectList(Gender, "GenderId", "GenderName");

            //Make MemberStatusIds
            ViewBag.MemberStatusIds = new SelectList(DB.SystemMemberStatus, "StatusId", "StatusNameTH");

            ViewBag.MemberPosition = new SelectList(DB.SystemMemberPosition.ToList(), "PositionId", "PositionNameTH");

            //Provide Data
            var TransMemberModel = new TransactionMemberVillage();
            TransMemberModel = GetMember;

            //Gen Day Selection            
            ViewBag.Day = new SelectList(new List<DateCollection>(), "Day", "Day");
            //Gen month and Month selection
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            var Months = new List<SelectListItem>();
            for (int m = 0; m < MonthName.Length; m++)
            {

                Months.Add(new SelectListItem
                {
                    Text = MonthName[m],
                    Value = m.ToString()
                });

            }
            ViewBag.Month = new SelectList(Months, "Value", "Text");
            //Gen Year and Year Selection
            var Years = new List<SelectListItem>();
            for (int y = DateTime.Now.Year; y > (DateTime.Now.Year - 300); y--)
            {
                Years.Add(new SelectListItem
                {
                    Text = (y + 543).ToString(),
                    Value = y.ToString()
                });
            }
            ViewBag.Year = new SelectList(Years, "Value", "Text");

            ViewBag.KickOffComment = TransMemberModel.KickOffComment;

            var OccuptionsDB = DB.SystemOccupationMaster.ToList();
            OccuptionsDB.Add(new SystemOccupationMaster { Id = 0, Name = "อาชีพอื่นๆ" });
            ViewBag.Occuption = new SelectList(OccuptionsDB, "Id", "Name");

            return View("FormEditMemberByCurrentUser", TransMemberModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCurrentMember(TransactionMemberVillage Member, DateCollection Date)
        {
            try
            {
                //Provide Object
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                //Check Approved Member
                if (DB.MemberVillage.Where(w => w.MemberId == CurrentUser.MemberId).Count() > 0)
                {
                    return Json(new { valid = false, message = "ลงทะเบียนเสร็จสิ้นแล้ว" });
                }

                //Edit
                var GetTransMember = DB.TransactionMemberVillage.Where(w => w.MemberId == Member.MemberId).FirstOrDefault();
                GetTransMember.CitizenId = Member.CitizenId;
                GetTransMember.MemberFirstName = Member.MemberFirstName;
                GetTransMember.MemberLastName = Member.MemberLastName;
                GetTransMember.GenderId = Member.GenderId;
                GetTransMember.Age = Member.Age;
                GetTransMember.Phone = Member.Phone;
                GetTransMember.MemberOccupation = Member.MemberOccupation;
                GetTransMember.MemberAddress = Member.MemberAddress;
                GetTransMember.MemberPositionId = Member.MemberPositionId;
                GetTransMember.MemberStatusId = Member.MemberStatusId;
                GetTransMember.MemberDate = DateTime.Now;
                GetTransMember.UpdateDate = DateTime.Now;
                GetTransMember.UpdateBy = CurrentUser.Id;
                GetTransMember.MemberBirthDate = new DateTime(Date.Year, Date.Month, Date.Day);
                GetTransMember.KickOffComment = Member.KickOffComment;
                GetTransMember.Connection = Member.Connection;
                GetTransMember.MemberEndDate = Member.MemberStatusId == 2 ? Member.MemberEndDate : GetTransMember.MemberEndDate;
                GetTransMember.NoCitizenId = Member.NoCitizenId;
                GetTransMember.NoBirthDate = Member.NoBirthDate;
                GetTransMember.MemberOccupationOther = Member.MemberOccupationOther;
                DB.TransactionMemberVillage.Update(GetTransMember);
                DB.SaveChanges();

                //Response OK
                string msg = "บันทึกข้อมูลสำเร็จ";
                return Json(new { valid = true, message = msg });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        #endregion

        #region Helper Function

        public string GetSubstringByString(string a, string b, string c)
        {
            return c.Substring((c.IndexOf(a) + a.Length), (c.IndexOf(b) - c.IndexOf(a) - a.Length));
        }

        #endregion

        #region Helper JsonAPI

        [HttpGet]
        public IActionResult GetMemberCode()
        {
            return Json(Json(new { Membercode = "" }));
        }

        #endregion

    }
}
