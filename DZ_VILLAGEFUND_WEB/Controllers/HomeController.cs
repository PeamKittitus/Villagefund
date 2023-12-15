using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.News;
using DZ_VILLAGEFUND_WEB.ViewModels.Home;
using DZ_VILLAGEFUND_WEB.ViewModels.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using DZ_VILLAGEFUND_WEB.ViewModels.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Json;
using DZ_VILLAGEFUND_WEB.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ServiceModel.Channels;
using System.Net;
using System.Security.Cryptography;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;
        public DZ_VILLAGEFUND_WEB.Helpers.Encrypt Encrypt;

        public HomeController(
             ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext db,
            IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
            Encrypt = new DZ_VILLAGEFUND_WEB.Helpers.Encrypt();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            /*
             * get user profile
             * get transaction
             */
            ViewBag.IdCard = CurrentUser.UserName;
            ViewBag.IsAdministrator = Helper.GetRoleUser(CurrentUser.Id);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MainIndex()
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.CurrentRoleName = Helper.GetRoleUser(CurrentUser.Id);
            ViewBag.GetdataUser = CurrentUser;

            var UserRole = await DB.UserRoles.Where(a => a.UserId == CurrentUser.Id).FirstOrDefaultAsync();
            var Models = new List<SystemMenus>();
            var Gets = await DB.SystemRolemenus.Where(w => w.RoleId == UserRole.RoleId).ToListAsync();
            foreach (var Get in Gets)
            {
                var GetMenus = await DB.SystemMenus.Where(w => w.Id == Get.MenuId && w.ParentId == 0 && w.ImageMenu != null).ToListAsync();
                foreach (var GetMenu in GetMenus)
                {
                    var Model = new SystemMenus();
                    Model.Id = GetMenu.Id;
                    Model.MenuName = GetMenu.MenuName;
                    Model.ParentId = GetMenu.ParentId;
                    Model.Position = GetMenu.Position;
                    Model.ControllerName = GetMenu.ControllerName;
                    Model.ActionName = GetMenu.ActionName;
                    Model.Icon = GetMenu.Icon;
                    Model.IsLink = GetMenu.IsLink;
                    Model.PathLink = GetMenu.PathLink;
                    Model.ImageMenu = GetMenu.ImageMenu;
                    Models.Add(Model);
                }
            }

            return View("MainIndex", Models);
        }

        [HttpGet]
        public async Task<IActionResult> GetNewsIndex()
        {
            //เรียกข่าวมาแสดง
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            var Models = new List<ListNewsViewModel>();
            var Gets = await DB.TransactionNews.Where(w => w.IsActive == true && w.IsApprove == true).OrderByDescending(o => o.UpdateDate).ToListAsync();
            foreach (var Get in Gets)
            {
                var Model = new ListNewsViewModel();
                if (Get.TransactionType == false)
                {
                    var RoldId = DB.UserRoles.Where(w => w.UserId == Get.TransactionBy).Select(s => s.RoleId).FirstOrDefault();
                    Model.TransactionNewsId = Get.TransactionNewsId;
                    Model.TransactionTitle = Get.TransactionTitle;
                    Model.TransactionBy = DB.Roles.Where(w => w.Id == RoldId).Select(s => s.Name).FirstOrDefault();
                    Model.FullName = DB.Users.Where(w => w.Id == Get.TransactionBy).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                    Model.NewsStartDate = Helper.getShortDateThai(Get.NewsStartDate);
                    Model.NewsEndDate = Helper.getShortDateThai(Get.NewsEndDate);
                    Model.UpdateDate = Helper.TimeAgo(Get.UpdateDate);
                    Model.TransactionType = Get.TransactionType;
                    Model.CountReader = DB.TransactionReader.Where(w => w.TransactionNewsId == Get.TransactionNewsId && w.IsRead == true).Count();
                    Models.Add(Model);
                }
                else
                {
                    var GetsNews = DB.TransactionReader.Where(w => w.UserId == CurrentUser.Id && w.TransactionNewsId == Get.TransactionNewsId).OrderByDescending(o => o.TransactionNewsId).Count();
                    if (GetsNews > 0)
                    {
                        var RoldId = DB.UserRoles.Where(w => w.UserId == Get.TransactionBy).Select(s => s.RoleId).FirstOrDefault();
                        Model.TransactionNewsId = Get.TransactionNewsId;
                        Model.TransactionTitle = Get.TransactionTitle;
                        Model.TransactionBy = DB.Roles.Where(w => w.Id == RoldId).Select(s => s.Name).FirstOrDefault();
                        Model.FullName = DB.Users.Where(w => w.Id == Get.TransactionBy).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                        Model.NewsStartDate = Helper.getShortDateThai(Get.NewsStartDate);
                        Model.NewsEndDate = Helper.getShortDateThai(Get.NewsEndDate);
                        Model.UpdateDate = Helper.TimeAgo(Get.UpdateDate);
                        Model.TransactionType = Get.TransactionType;
                        Model.CountReader = DB.TransactionReader.Where(w => w.TransactionNewsId == Get.TransactionNewsId && w.IsRead == true).Count();
                        Models.Add(Model);

                    }
                }
            }

            return PartialView("GetNewsIndex", Models);
        }

        [HttpGet]
        public async Task<IActionResult> News(string Id)
        {
            //รายละเอียดข่าว
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetNews = DB.TransactionNews.Where(w => w.TransactionNewsId == int.Parse(Id)).FirstOrDefault();
            var GetFileNews = DB.TransactionFileNews.Where(w => w.TransactionNewsId == int.Parse(Id)).FirstOrDefault();

            if (DB.TransactionReader.Where(w => w.TransactionNewsId == int.Parse(Id) && w.UserId == CurrentUser.Id).Count() > 0)
            {
                var ModelReader = DB.TransactionReader.Where(w => w.TransactionNewsId == int.Parse(Id) && w.UserId == CurrentUser.Id).FirstOrDefault();
                ModelReader.IsRead = true;
                ModelReader.UpdateDate = DateTime.Now;
                ModelReader.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                DB.TransactionReader.Update(ModelReader);
                await DB.SaveChangesAsync();
            }

            var ModelNews = new TransactionNewsViewModel();
            var Models = new List<ListNewsItemModel>();
            var FileNewsData = DB.TransactionFileNews.Where(w => w.TransactionNewsId == int.Parse(Id)).ToList();
            foreach (var Get in FileNewsData)
            {
                var Model = new ListNewsItemModel();
                Model.GencodeFileName = Get.GencodeFileName;
                Model.FileName = Get.FileName;
                Models.Add(Model);
            }

            ModelNews.UserId = DB.Users.Where(w => w.Id == GetNews.TransactionBy).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
            ModelNews.TransactionNewsId = int.Parse(Id);
            ModelNews.TransactionTitle = GetNews.TransactionTitle;
            ModelNews.TransactionDetail = GetNews.TransactionDetail;
            ModelNews.NewsStartDate = Helper.getDateThaiAndTime(GetNews.UpdateDate);
            ModelNews.UpdateDate = Helper.TimeAgo(GetNews.UpdateDate);
            ModelNews.FileNames = Models;
            return View("News", ModelNews);
        }

        [HttpGet]
        public async Task<IActionResult> LeftSide()
        {
            // master data 
            var OrgStrs = await DB.SystemOrgStructures.ToListAsync();

            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var UserInfo = new UserInfoViewModel();
            UserInfo.FullName = "คุณ" + CurrentUser.FirstName + " " + CurrentUser.LastName;
            UserInfo.OrgName = OrgStrs.Where(w => w.OrgId == CurrentUser.OrgId).Select(s => s.OrgName).FirstOrDefault();
            UserInfo.UserId = CurrentUser.Id;
            UserInfo.RoleName = Helper.GetUserRoleName(CurrentUser.Id);
            ViewBag.UserInfo = UserInfo;

            // get member lists
            var MemberModels = new List<ListMembers>();
            var GetMembers = await DB.UserRoles.Where(w => w.RoleId == "e3e3a4aa-4e53-40d4-9f55-53192d5fa761").ToListAsync();
            foreach (var GetMember in GetMembers)
            {
                var MemberModel = new ListMembers();
                MemberModel.FullName = DB.Users.Where(w => w.Id == GetMember.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                MemberModels.Add(MemberModel);
            }

            ViewBag.Members = MemberModels;

            return PartialView("LeftSide");
        }

        [HttpGet]
        public async Task<IActionResult> RightSide()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            // get data master
            var GetUsers = await DB.Users.ToListAsync();

            ViewBag.CountVillageAll = DB.Village.Where(w => w.IsActive == true).Count();
            ViewBag.CountERegisterWaiting = DB.TransactionReqVillage.Where(w => w.StatusId == 0).Count();
            ViewBag.IsAdministrator = Helper.GetRoleUser(CurrentUser.Id);

            // get last used logs
            var LogModels = new List<UsedLogs>();
            var GetUsedLogs = DB.SystemLogs.OrderByDescending(f => f.ActionDate).Take(3).ToList();
            foreach (var GetUsedLog in GetUsedLogs)
            {
                var LogModel = new UsedLogs();
                LogModel.ActiveDate = Helper.getShortDateThaiAndTime(GetUsedLog.ActionDate);
                LogModel.EventName = GetUsedLog.Action;
                LogModel.FullName = GetUsers.Where(w => w.Id == GetUsedLog.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                LogModels.Add(LogModel);
            }

            ViewBag.UsedLogs = LogModels;

            return PartialView("RightSide");
        }

        [HttpGet]
        public IActionResult AccessDenied(string ReturnUrl)
        {
            return View("AccessDenied");
        }

        #region edit user profile

        [HttpGet]
        public async Task<IActionResult> ProfileData()
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.UserRole = await DB.UserRoles.Where(w => w.UserId == CurrentUser.Id).Select(s => s.RoleId).FirstOrDefaultAsync();
            ViewBag.Role = Helper.GetUserRoleName(CurrentUser.Id);

            //กลุ่ม
            ViewBag.UserLookupValueDivision = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupValue", "LookupText");
            //ฝ่าย
            ViewBag.UserLookupValueDepartment = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupValue", "LookupText");

            //Branch User
            if (ViewBag.Role == "SuperUser")
            {
                ViewBag.Org = new SelectList(DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).ToList(), "OrgId", "OrgName");
            }
            else
            {
                ViewBag.Org = new SelectList(DB.SystemOrgStructures.Where(w => w.OrgId == (DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).FirstOrDefault().ParentId == 0 ? 1 : DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).FirstOrDefault().ParentId)).ToList(), "OrgId", "OrgName");
            }

            return View("ProfileData", CurrentUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditUser(ApplicationUser Model, string Password, string Roles)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = string.Empty;
            try
            {
                // set data user
                var ThisUser = await _userManager.FindByIdAsync(Model.Id);
                ThisUser.UserName = Model.UserName;
                ThisUser.FirstName = Model.FirstName;
                ThisUser.LastName = Model.LastName;
                ThisUser.Email = Model.Email;
                ThisUser.PhoneNumber = Model.PhoneNumber;
                ThisUser.CitizenId = Model.CitizenId;
                ThisUser.LookupValueDivision = Model.LookupValueDivision;
                ThisUser.LookupValueDepartment = Model.LookupValueDepartment;

                // reset new password
                if (Password != null)
                {
                    var Code = await _userManager.GeneratePasswordResetTokenAsync(ThisUser);
                    var ChangePassword = await _userManager.ResetPasswordAsync(ThisUser, Code, Password);
                    if (!ChangePassword.Succeeded)
                    {
                        return Json(new { valid = false, message = ChangePassword.Errors.Select(s => s.Code + " " + s.Description).FirstOrDefault() });
                    }
                }

                var result = await _userManager.UpdateAsync(ThisUser);
                if (result.Succeeded)
                {
                    if (DB.UserRoles.Where(w => w.UserId == Model.Id).Count() > 0)
                    {
                        var RoleThisUser = DB.UserRoles.Where(w => w.UserId == Model.Id).FirstOrDefault();
                        DB.UserRoles.Remove(RoleThisUser);
                        DB.SaveChanges();

                        var GetAllRoles = await _roleManager.Roles.Where(w => w.Id == Roles).ToListAsync();
                        foreach (var GetAllRole in GetAllRoles)
                        {
                            var roleresult = await _userManager.AddToRoleAsync(ThisUser, GetAllRole.NormalizedName);
                        }
                    }

                    // add log
                    Helper.AddUsedLog(CurrentUser.Id, "แก้ไขข้อมูลผู้ใช้งาน", HttpContext, "ระบบจัดการผู้ใช้งาน");

                    Message = "บักทึกข้อมูลสำเร็จ";
                }
                else
                {
                    foreach (var Error in result.Errors)
                    {
                        Message = Error.Description + "<br/>";
                    }

                    return Json(new { valid = false, message = Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #endregion

        #region notification 

        [HttpGet]
        public async Task<IActionResult> NewsNotification()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<Notification>();
            var Gets = DB.TransactionNews.Where(w => w.IsApprove == true && w.IsActive == true && w.TransactionType == false || (w.UserId.Contains(CurrentUser.Id))).Take(5).OrderByDescending(f => f.UpdateDate);
            foreach (var Get in Gets)
            {
                var Model = new Notification();
                Model.TimeAgo = Helper.TimeAgo(Get.UpdateDate);
                Model.Title = Get.TransactionTitle;
                Model.Id = Get.TransactionNewsId;
                Models.Add(Model);
            }

            ViewBag.Count = DB.TransactionNews.Where(w => w.IsApprove == true && w.IsActive == true && w.TransactionType == false || (w.UserId.Contains(CurrentUser.Id))).Take(5).Count();

            return PartialView("NewsNotification", Models);
        }

        #endregion

        #region helper

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserRole()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            return Json(new { data = Helper.GetUserRoleName(CurrentUser.Id) });
        }

        [HttpGet]
        public async Task<IActionResult> RedirectToOutSite(int MenuId)
        {
            string Data = "";
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            // generate token key
            string TokenKey = Helper.GenerateJWTToken(CurrentUser.UserName, CurrentUser.Id);
            var Jtoken = (JObject)JsonConvert.DeserializeObject(TokenKey);
            if (Jtoken["valid"].ToString() == "false")
            {
                return Json(new { valid = false, data = Jtoken["data"].ToString() });
            }
            var GetMenu = DB.SystemMenus.Where(w => w.Id == MenuId).FirstOrDefault();
            if (GetMenu.IsLink == true)
            {
                string SetData = (CurrentUser.OrgId != 0 ? DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).Select(s => s.OrgName).FirstOrDefault() : "-")
                + "|" + Helper.getDateThaiAndTime(DateTime.Now.AddHours(2))
                + "|" + CurrentUser.FirstName
                + "|" + CurrentUser.LastName
                + "|" + CurrentUser.UserName
                + "|" + Jtoken["data"].ToString()
                + "|" + CurrentUser.CitizenId
                + "|" + "นักวิชาการ(รอข้อมูลต้นทาง)";

                //string JsonConverted = Newtonsoft.Json.JsonConvert.SerializeObject(Datas);
                string EncriptAssii = Helper.EnryptString(SetData);
                string DecriptAssii = Helper.DecryptString(EncriptAssii);
                Data = GetMenu.PathLink + EncriptAssii.Trim();

                // add log 
                Helper.AddUsedLog(CurrentUser.Id, "เข้าสู่ระบบ" + GetMenu.MenuName, HttpContext, "เข้าสู่ระบบหน้าหลัก");
            }
            else
            {
                return Json(new { valid = false });
            }

            return Json(new { valid = true, data = Data });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetOrg()
        {
            var OrgStrus = new List<SelectListItem>();

            OrgStrus.Add(new SelectListItem()
            {
                Text = "กรุณาเลือกสาขา",
                Value = "0"
            });

            var GetOrgs = DB.SystemOrgStructures.Where(w => w.ParentId == 0).OrderBy(w => w.OrgId).ToList();
            foreach (var Get in GetOrgs)
            {
                OrgStrus.Add(new SelectListItem()
                {
                    Text = "- " + Get.OrgName,
                    Value = Get.OrgId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = DB.SystemOrgStructures.Where(w => w.ParentId == Get.OrgId).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    OrgStrus.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + "- " + Get2.OrgName,
                        Value = Get2.OrgId.ToString(),
                        Disabled = true
                    });

                    var GetLevel3 = DB.SystemOrgStructures.Where(w => w.ParentId == Get2.OrgId).ToList();
                    foreach (var Get3 in GetLevel3)
                    {
                        OrgStrus.Add(new SelectListItem()
                        {
                            Text = SpaceString(20) + "- " + Get3.OrgName,
                            Value = Get3.OrgId.ToString()
                        });
                    }
                }
            }
            return Json(OrgStrus);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetVillages(int OrgId)
        {
            var Villages = new List<SelectListItem>();

            Villages.Add(new SelectListItem()
            {
                Text = "กรุณาเลือกกองทุนหมู่บ้าน",
                Value = "0"
            });

            var GetVillages = DB.Village.Where(w => (OrgId == 0 || w.OrgId == OrgId)).ToList();
            if (GetVillages.Count() > 0)
            {
                foreach (var Get in GetVillages)
                {
                    Villages.Add(new SelectListItem()
                    {
                        Text = Get.VillageName,
                        Value = Get.VillageId.ToString()
                    });
                }
            }
            else
            {
                Villages.Add(new SelectListItem()
                {
                    Text = "--ไม่มีข้อมูล--",
                    Value = "0"
                });
            }

            return Json(Villages);
        }

        public string SpaceString(int Count)
        {
            var JsonValues = new List<object>();
            string Spacing = "\xA0";
            for (int i = 1; i < Count; i++)
            {
                JsonValues.Add(Spacing);
            }

            return string.Join("", JsonValues.ToArray());
        }

        [HttpGet]
        public async Task<IActionResult> TestSendMail()
        {
            return Json(await Helper.SendMail("sorawit.kam@gmail.com", "assssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssaddd"));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult InsertLogSystemApi(string SystemName, string Uid, string _Action)
        {

            try
            {

                // add log
                Helper.AddUsedLog(Uid, _Action, HttpContext, SystemName);

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult GetCurrentBudgetYears()
        {

            var CurentBudgetYears = new List<SelectListItem>();

            List<int> GenerateIntList(int start, int decreaseCount, int increaseCount)
            {
                List<int> intList = new List<int>();

                // Increase
                for (int i = start; i <= start + increaseCount; i += 1)
                {
                    intList.Add(i);
                }

                // Increase
                for (int i = start-1; i >= start - decreaseCount; i -= 1)
                {
                    intList.Add(i);
                }

                return intList;
            }

            //GenerateIntList decreaseCount คือ ย้อนหลัง N ปี increaseCount คือ อนาคต N ปี start คือปีงบปัจจุบัน
            var GetCurentBudgetYears = GenerateIntList(Helper.CurrentBudgetYear(), 10, 5).OrderByDescending(o => o);
            foreach (var Get in GetCurentBudgetYears)
            {
                CurentBudgetYears.Add(new SelectListItem()
                {
                    Text = Get.ToString(),
                    Value = Get.ToString(),
                    Selected = Get == Helper.CurrentBudgetYear() ? true : false
                });
            }

            return Json(CurentBudgetYears);
        }

        #endregion
    }
}
