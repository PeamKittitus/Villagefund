using AutoMapper;
using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.DateCollection;
using DZ_VILLAGEFUND_WEB.ViewModels.EAccount;
using DZ_VILLAGEFUND_WEB.ViewModels.Members;
using DZ_VILLAGEFUND_WEB.ViewModels.Villages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.WebRequestMethods;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class VillagesController : Controller
    {
        private readonly ILogger<VillagesController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        [Obsolete]
        private Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        [Obsolete]
        public VillagesController(
        ILogger<VillagesController> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        IConfiguration configuration,
        Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _environment = environment;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }


        public async Task<IActionResult> Index()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));


            var GetTransVillage = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();

            //Flag Registered
            ViewBag.CheckRegistered = GetTransVillage != null ? "1" : "0";

            //Check Edit Register
            var GetTransactionReqVillage = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
            if (GetTransactionReqVillage != null)
            {
                ViewBag.Type = GetTransactionReqVillage.TransactionType;
            }


            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetVillages()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            List<VillagesViewModel> Villages = new List<VillagesViewModel>();
            var GetVillages = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).ToList();
            foreach (var Village in GetVillages)
            {
                var VillageViewModel = new VillagesViewModel();
                VillageViewModel.VillageId = Village.VillageId;
                VillageViewModel.VillageCode = Village.VillageCode;
                VillageViewModel.VillageCodeText = Village.VillageCodeText;
                VillageViewModel.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(Village.VillageBbdId)));
                VillageViewModel.VillageName = Village.VillageName;
                Villages.Add(VillageViewModel);
            }

            var TransactionType = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).Select(s => s.TransactionType).FirstOrDefault();
            ViewBag.Type = TransactionType;

            return PartialView(Villages);
        }

        // GET: Villages/FormAddVillage
        [HttpGet]
        public async Task<IActionResult> FormAddVillage()
        {

            //Prepare Data
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            //Check Data
            if (DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).Count() > 0)
            {
                return RedirectToAction("FormEditVillageRegister");
            }

            //Account
            ViewBag.Banks = new SelectList(DB.AccountBankMaster.ToList(), "BankCode", "BankName");

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


            return View("FormAddVillage");
        }

        // GET: Villages/SelectProvince
        [HttpGet]
        public IActionResult SelectProvince()
        {
            ViewBag.ProvinceId = new SelectList(DB.SystemProvince.OrderBy(d => d.ProvinceId).ToList(), "ProvinceId", "ProvinceName");
            return PartialView("SelectProvince");
        }

        // GET: Villages/SelectDistric
        [HttpGet]
        public IActionResult SelectDistric(int ProvinceId)
        {
            ViewBag.DistrictId = new SelectList(DB.SystemDistrict.Where(w => w.ProvinceId == (ProvinceId != 0 ? ProvinceId : 1))
                .OrderBy(d => d.AmphurId).ToList(), "AmphurId", "AmphurName");
            return PartialView("SelectDistric");
        }

        // GET: Villages/SelectSubDistric
        [HttpGet]
        public IActionResult SelectSubDistric(int ProvinceId, int DistrictId)
        {
            ViewBag.SubDistrictId = new SelectList(DB.SystemSubDistrict.Where(w => w.ProvinceId == (ProvinceId != 0 ? ProvinceId : 1)
            && w.AmphurId == (DistrictId != 0 ? DistrictId : 1))
                .OrderBy(d => d.SubdistrictId).ToList(), "SubdistrictId", "SubdistrictName");
            return PartialView("SelectSubDistric");
        }

        // GET: Villages/Poscode
        [HttpGet]
        public IActionResult Poscode(int DistrictId)
        {
            //ViewBag.PostCode = DB.SystemSubDistrict.Where(w => w.AmphurId == (DistrictId != 0 ? DistrictId : 1)).Select(s => s.SubdistrictCode).FirstOrDefault();
            //Pending
            return PartialView("Poscode");
        }

        // POST: Villages/AddVillage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVillage(TransactionVillage VillageModel, DateCollection Date, AccountBookBank BookBankModel, IFormFile BookbankFile)
        {
            try
            {

                //Check Village Code Conflict(เช็ครหัสกองทุนซ้ำ)
                if (DB.Village.Where(w => w.VillageCodeText == VillageModel.VillageCodeText).Count() > 0)
                {
                    return Json(new { valid = false, message = "รหัสกองทุนนี้มีในระบบแล้ว" });
                }

                //Prepare Data
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                var TransVillageReq = new TransactionReqVillage();

                //Insert TransactionReq
                TransVillageReq.UserId = CurrentUser.Id;
                TransVillageReq.TransactionType = 1;
                TransVillageReq.StatusId = 3;
                TransVillageReq.TransactionYear = DateTime.Now.Year + 543;
                TransVillageReq.UpdateDate = DateTime.Now;
                TransVillageReq.ApproveDate = DateTime.Now;
                TransVillageReq.UpdateBy = CurrentUser.Id;
                TransVillageReq.OrgId = CurrentUser.OrgId;
                DB.TransactionReqVillage.Add(TransVillageReq);
                DB.SaveChanges();

                //Insert TransactionVillage
                var GetTranId = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.TransactionVillageId).Select(s => s.TransactionVillageId).FirstOrDefault();
                VillageModel.TransactionVillageId = GetTranId;
                VillageModel.IsActive = true;
                VillageModel.VillageStartDate = DateTime.Now;
                VillageModel.VillageEndDate = DateTime.Now.AddYears(1);
                VillageModel.UpdateDate = DateTime.Now;
                VillageModel.BbdDate = new DateTime(Date.Year, Date.Month, Date.Day);
                VillageModel.UserId = CurrentUser.Id;
                VillageModel.UpdateBy = CurrentUser.Id;
                VillageModel.OrgId = CurrentUser.OrgId;
                DB.TransactionVillage.Add(VillageModel);
                DB.SaveChanges();

                //Update TransVillageId in TransactionMemberVillage
                var GetTransMembers = DB.TransactionMemberVillage.Where(w => w.UpdateBy == CurrentUser.Id).ToList();
                if (GetTransMembers.Count() > 0)
                {
                    foreach (var GetTransMember in GetTransMembers)
                    {
                        GetTransMember.TransactionVillageId = GetTranId;
                        DB.TransactionMemberVillage.Update(GetTransMember);
                    }
                    DB.SaveChanges();
                }

                //Update TransVillageId in TransFileVillage
                var GetTransFileVillages = DB.TransactionFileVillage.Where(w => w.UpdateBy == CurrentUser.Id).ToList();
                if (GetTransFileVillages.Count() > 0)
                {
                    foreach (var GetTransFileVillage in GetTransFileVillages)
                    {
                        GetTransFileVillage.TransactionVillageId = GetTranId;
                        DB.TransactionFileVillage.Update(GetTransFileVillage);
                    }
                    DB.SaveChanges();
                }

                //Response OK
                string msg = "บันทึกข้อมูลสำเร็จ";
                return Json(new { valid = true, message = msg });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message, villagecode = VillageModel.VillageCodeText });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRegisterVillage(TransactionVillage Model, DateCollection Date, IFormFile BookbankFile, int BookbankFileId)
        {

            try
            {

                //Prepare Data
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                //Check Village Code Conflict(เช็ครหัสกองทุนซ้ำ)
                if (DB.Village.Where(w => w.VillageCodeText == Model.VillageCodeText).Count() > 0)
                {
                    return Json(new { valid = false, message = "รหัสกองทุนนี้มีในระบบแล้ว" });
                }

                var GetTra = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.VillageId).FirstOrDefault();
                GetTra.TransactionVillageId = Model.TransactionVillageId;
                GetTra.UserId = CurrentUser.Id;
                GetTra.VillageBbdId = Model.VillageBbdId;
                GetTra.VillageName = Model.VillageName;
                GetTra.VillageAddress = Model.VillageAddress;
                GetTra.VillageMoo = Model.VillageMoo;
                GetTra.ProvinceId = Model.ProvinceId;
                GetTra.DistrictId = Model.DistrictId;
                GetTra.SubDistrictId = Model.SubDistrictId;
                GetTra.PostCode = Model.PostCode;
                GetTra.Phone = Model.Phone;
                GetTra.Email = Model.Email;
                GetTra.UpdateBy = CurrentUser.Id;
                GetTra.BbdDate = new DateTime(Date.Year, Date.Month, Date.Day);
                GetTra.UpdateDate = DateTime.Now;
                GetTra.IsActive = false;
                GetTra.VillageCodeText = Model.VillageCodeText;
                GetTra.VillageBbdCode = Model.VillageBbdCode;
                GetTra.OrgId = CurrentUser.OrgId;
                DB.TransactionVillage.Update(GetTra);
                DB.SaveChanges();

                var GetReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == Model.TransactionVillageId).FirstOrDefault();
                GetReq.StatusId = 3;
                GetReq.TransactionType = 1;
                GetReq.OrgId = CurrentUser.OrgId;
                DB.TransactionReqVillage.Update(GetReq);
                DB.SaveChanges();

                //Upload Bookbank File 
                if (BookbankFile != null)
                {
                    var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/e-account/");
                    if (BookbankFile.ContentType != "application/x-msdownload" && Path.GetExtension(BookbankFile.FileName) != ".msi" && Path.GetExtension(BookbankFile.FileName) != ".bat")
                    {
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(BookbankFile.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            BookbankFile.CopyTo(fileStream);
                        }
                        var FileModel = new TransactionFileBookbank();
                        FileModel.TransactionYear = DateTime.Now.Year + 543;
                        FileModel.FileName = BookbankFile.FileName;
                        FileModel.GencodeFileName = UniqueFileName;
                        FileModel.UpdateDate = DateTime.Now;
                        FileModel.UpdateBy = CurrentUser.Id;
                        DB.TransactionFileBookbank.Add(FileModel);
                        DB.SaveChanges();
                    }

                    //Delete Old File
                    var GetFile = DB.TransactionFileBookbank.Where(w => w.TransactionFileBookbankId == BookbankFileId).FirstOrDefault();
                    if (GetFile != null)
                    {
                        string oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                        if (System.IO.File.Exists(oldfilepath))
                        {
                            System.IO.File.Delete(oldfilepath);
                        }
                        DB.TransactionFileBookbank.Remove(GetFile);
                        DB.SaveChanges();
                    }
                }

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            //Response OK
            string msg = "บันทึกข้อมูลสำเร็จ";
            return Json(new { valid = true, message = msg, villagecode = Model.VillageCodeText });
        }

        // GET: Villages/Edit/5
        public IActionResult Edit(int id)
        {
            return View();
        }

        // POST: 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndAddVillage()
        {
            try
            {
                // get current user
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                var GetTransVillageReq = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.StatusId == 3 && w.TransactionType == 1)
                    .OrderByDescending(o => o.TransactionVillageId).FirstOrDefault();
                GetTransVillageReq.TransactionType = 1;
                GetTransVillageReq.StatusId = 0;
                DB.TransactionReqVillage.Update(GetTransVillageReq);
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

        // GET: Villages/Delete/5
        public IActionResult Delete(int id)
        {
            return View();
        }

        // POST: Villages/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public async Task<IActionResult> VillageIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);
            if (UserRole == "HeadQuarterAdmin" || UserRole == "officer" || UserRole == "administrator")
            {
                return RedirectToAction("VillageListDetail");
            }
            else if (UserRole == "user")
            {
                return RedirectToAction("RegisterVillageIndex");
            }
            else if (UserRole == "BranchAdmin" || UserRole == "ProvinceAdmin")
            {
                return RedirectToAction("AdminVillagesIndex");
            }

            ViewBag.CheckVillage = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();

            //Check Req Status
            ViewBag.Status = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.TransactionType != 2)
                .OrderByDescending(o => o.TransactionVillageId).Select(s => s.StatusId).FirstOrDefault();

            //Check Req Data
            ViewBag.IsNull = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.TransactionType != 2)
                .OrderByDescending(o => o.TransactionVillageId).FirstOrDefault() == null ? true : false;

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetVillagesApproved()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            List<VillagesViewModel> Villages = new List<VillagesViewModel>();
            var GetVillages = DB.Village.Where(w => w.UserId == CurrentUser.Id).ToList();
            foreach (var Village in GetVillages)
            {
                var VillageViewModel = new VillagesViewModel();
                VillageViewModel.VillageId = Village.VillageId;
                VillageViewModel.VillageCode = Village.VillageCode;
                VillageViewModel.VillageCodeText = Village.VillageCodeText;
                VillageViewModel.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(Village.VillageBbdId)));
                VillageViewModel.VillageName = Village.VillageName;
                VillageViewModel.IsActive = Village.IsActive;
                VillageViewModel.BbdDate = Helper.getDateThai(Village.BbdDate);
                VillageViewModel.ExpirydDate = Helper.getDateThai(Village.BbdDate.AddYears(1));
                Villages.Add(VillageViewModel);
            }
            return PartialView(Villages);
        }

        [HttpGet]
        public IActionResult UploadDocIndex()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> UploadVillageDoc()
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadDoc(IFormFile[] FileName, int VillageId)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                if (FileName.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/village/");
                    foreach (var FileAtt in FileName)
                    {
                        var test = Path.GetExtension(FileAtt.FileName);

                        if (FileAtt.ContentType != "application/x-msdownload" && Path.GetExtension(FileAtt.FileName) != ".msi" && Path.GetExtension(FileAtt.FileName) != ".bat")
                        {
                            string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(FileAtt.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await FileAtt.CopyToAsync(fileStream);
                            }

                            var Model = new TransactionFileVillage();
                            Model.GencodeFileName = UniqueFileName;
                            Model.TransactionYear = DateTime.Now.Year;
                            Model.FileName = FileAtt.FileName;
                            Model.UpdateBy = CurrentUser.Id;
                            Model.UpdateDate = DateTime.Now;
                            Model.VillageId = VillageId;
                            DB.TransactionFileVillage.Add(Model);
                            await DB.SaveChangesAsync();

                            var GetTransactionVillage = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).OrderByDescending(o => o.TransactionVillageId).FirstOrDefault();
                            if (GetTransactionVillage != null)
                            {
                                Model.TransactionVillageId = GetTransactionVillage.TransactionVillageId;
                                DB.TransactionFileVillage.Update(Model);
                                DB.SaveChanges();
                            }

                        }
                        else
                        {
                            throw new Exception(" อัปโหลดไฟล์ Document, Sheet, Image, Multimedia, Zip หรือ Rar เท่านั้น");
                        }
                    }
                }

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> GetFileAttachDoc()
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var FileRecord = DB.TransactionFileVillage.Where(w => w.UpdateBy == CurrentUser.Id).ToList();

            ViewBag.Helper = Helper;

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView(FileRecord);
        }

        [HttpGet]
        public IActionResult DeleteFile(int TransactionFileId)
        {
            try
            {
                var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/village/");
                var GetOldFile = DB.TransactionFileVillage.Where(w => w.TransactionFileId == TransactionFileId).FirstOrDefault();
                if (GetOldFile.GencodeFileName != null)
                {
                    string oldfilepath = Path.Combine(Uploads, GetOldFile.GencodeFileName);
                    if (System.IO.File.Exists(oldfilepath))
                    {
                        System.IO.File.Delete(oldfilepath);
                    }
                }
                DB.TransactionFileVillage.Remove(GetOldFile);
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
        public IActionResult VillageTerminateOrRenewal()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetVillageTerminateOrRenewal()
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var GetTransaction = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
            ViewBag.TransactionTypeFlag = new int[] { 3, 2 }.Contains(GetTransaction.TransactionType) ? true : false;

            var GetVillages = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
            ViewBag.IsActive = GetVillages.IsActive;

            return PartialView(GetVillages);
        }

        [HttpPost]
        public async Task<IActionResult> TerminateVillage()
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                // add new TransactionReqVillage
                var ReqModel = new TransactionReqVillage();
                ReqModel.UserId = CurrentUser.Id;
                ReqModel.TransactionType = 3;
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

                // add new TransactionVillage
                var Model = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
                var TransactioVilage = new TransactionVillage();
                TransactioVilage.TransactionVillageId = ReqModel.TransactionVillageId;
                TransactioVilage.UserId = CurrentUser.Id;
                TransactioVilage.VillageCode = Model.VillageCode;
                TransactioVilage.VillageBbdId = Model.VillageBbdId;
                TransactioVilage.VillageName = Model.VillageName;
                TransactioVilage.VillageAddress = Model.VillageAddress;
                TransactioVilage.VillageMoo = Model.VillageMoo;
                TransactioVilage.ProvinceId = Model.ProvinceId;
                TransactioVilage.DistrictId = Model.DistrictId;
                TransactioVilage.SubDistrictId = Model.SubDistrictId;
                TransactioVilage.PostCode = Model.PostCode;
                TransactioVilage.Phone = Model.Phone;
                TransactioVilage.Email = Model.Email;
                TransactioVilage.BbdDate = Model.BbdDate;
                TransactioVilage.VillageStartDate = Model.VillageStartDate;
                TransactioVilage.VillageEndDate = Model.VillageEndDate;
                TransactioVilage.IsActive = true;
                TransactioVilage.UpdateBy = CurrentUser.Id;
                TransactioVilage.UpdateDate = DateTime.Now;
                TransactioVilage.OrgId = CurrentUser.OrgId;
                TransactioVilage.VillageCodeText = Model.VillageCodeText;
                DB.TransactionVillage.Add(TransactioVilage);
                await DB.SaveChangesAsync();

                return Json(new { valid = true, message = "ยื่นคำขอยกเลิกบัญชีสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { valid = false, message = ex });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RenewalVillage()
        {
            try
            {
                // get current user
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

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

                // add new TransactionVillage
                var Model = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
                var TransactioVilage = new TransactionVillage();
                TransactioVilage.TransactionVillageId = ReqModel.TransactionVillageId;
                TransactioVilage.UserId = CurrentUser.Id;
                TransactioVilage.VillageCode = Model.VillageCode;
                TransactioVilage.VillageBbdId = Model.VillageBbdId;
                TransactioVilage.VillageName = Model.VillageName;
                TransactioVilage.VillageAddress = Model.VillageAddress;
                TransactioVilage.VillageMoo = Model.VillageMoo;
                TransactioVilage.ProvinceId = Model.ProvinceId;
                TransactioVilage.DistrictId = Model.DistrictId;
                TransactioVilage.SubDistrictId = Model.SubDistrictId;
                TransactioVilage.PostCode = Model.PostCode;
                TransactioVilage.Phone = Model.Phone;
                TransactioVilage.Email = Model.Email;
                TransactioVilage.BbdDate = Model.BbdDate;
                TransactioVilage.VillageStartDate = Model.VillageStartDate;
                TransactioVilage.VillageEndDate = Model.VillageEndDate.AddYears(1);
                TransactioVilage.IsActive = true;
                TransactioVilage.UpdateBy = CurrentUser.Id;
                TransactioVilage.UpdateDate = DateTime.Now;
                TransactioVilage.OrgId = CurrentUser.OrgId;
                TransactioVilage.VillageCodeText = Model.VillageCodeText;
                DB.TransactionVillage.Add(TransactioVilage);
                await DB.SaveChangesAsync();

                return Json(new { valid = true, message = "ยื่นคำขอต่ออายุสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { valid = false, message = ex });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VillageEdit(Int64 VillageId)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            #region master
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

            #endregion

            // add new TransactionReqVillage
            var Model = new FormUpdateViewModel();
            var CheckTransReq = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.TransactionType == 4).Any(i => i.StatusId == 3);
            if (!CheckTransReq)
            {
                var ReqModel = new TransactionReqVillage();
                ReqModel.UserId = CurrentUser.Id;
                ReqModel.TransactionType = 4;
                ReqModel.TransactionYear = (DateTime.Now.Year + 543);
                ReqModel.TransactionDetail = null;
                ReqModel.TransactionEdit = null;
                ReqModel.StatusId = 3;
                ReqModel.UpdateBy = CurrentUser.Id;
                ReqModel.UpdateDate = DateTime.Now;
                ReqModel.ApproveBy = null;
                ReqModel.ApproveDate = DateTime.Now;
                ReqModel.OrgId = CurrentUser.OrgId;
                DB.TransactionReqVillage.Add(ReqModel);
                await DB.SaveChangesAsync();


                // reqId
                ViewBag.ReqId = ReqModel.TransactionVillageId;

                //check transaction by req
                var GetTransactionVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == ReqModel.TransactionVillageId).FirstOrDefault();
                if (GetTransactionVillage != null)
                {
                    Model.VillageId = GetTransactionVillage.VillageId;
                    Model.TransactionVillageId = GetTransactionVillage.TransactionVillageId;
                    Model.UserId = GetTransactionVillage.UserId;
                    Model.VillageCodeText = GetTransactionVillage.VillageCodeText;
                    Model.VillageBbdId = GetTransactionVillage.VillageBbdId;
                    Model.VillageName = GetTransactionVillage.VillageName;
                    Model.VillageAddress = GetTransactionVillage.VillageAddress;
                    Model.VillageMoo = GetTransactionVillage.VillageMoo;
                    Model.ProvinceId = GetTransactionVillage.ProvinceId;
                    Model.DistrictId = GetTransactionVillage.DistrictId;
                    Model.SubDistrictId = GetTransactionVillage.SubDistrictId;
                    Model.PostCode = GetTransactionVillage.PostCode;
                    Model.Phone = GetTransactionVillage.Phone;
                    Model.Email = GetTransactionVillage.Email;
                    Model.BbdDate = GetTransactionVillage.BbdDate;
                    Model.VillageStartDate = GetTransactionVillage.VillageStartDate;
                    Model.VillageEndDate = GetTransactionVillage.VillageEndDate;
                    Model.IsActive = Convert.ToBoolean(GetTransactionVillage.IsActive);
                    Model.UpdateDate = GetTransactionVillage.UpdateDate;
                    Model.UpdateBy = GetTransactionVillage.UpdateBy;
                    Model.OrgId = GetTransactionVillage.OrgId;
                    Model.VillageBbdCode = GetTransactionVillage.VillageBbdCode;
                }
                else
                {
                    var GetVillage = DB.Village.Where(w => w.VillageId == VillageId).FirstOrDefault();
                    Model.VillageId = GetVillage.VillageId;
                    Model.UserId = GetVillage.UserId;
                    Model.VillageCodeText = GetVillage.VillageCodeText;
                    Model.VillageBbdId = GetVillage.VillageBbdId;
                    Model.VillageName = GetVillage.VillageName;
                    Model.VillageAddress = GetVillage.VillageAddress;
                    Model.VillageMoo = GetVillage.VillageMoo;
                    Model.ProvinceId = GetVillage.ProvinceId;
                    Model.DistrictId = GetVillage.DistrictId;
                    Model.SubDistrictId = GetVillage.SubDistrictId;
                    Model.PostCode = GetVillage.PostCode;
                    Model.Phone = GetVillage.Phone;
                    Model.Email = GetVillage.Email;
                    Model.BbdDate = GetVillage.BbdDate;
                    Model.VillageStartDate = GetVillage.VillageStartDate;
                    Model.VillageEndDate = GetVillage.VillageEndDate;
                    Model.IsActive = GetVillage.IsActive;
                    Model.UpdateDate = GetVillage.UpdateDate;
                    Model.UpdateBy = GetVillage.UpdateBy;
                    Model.OrgId = GetVillage.OrgId;
                    Model.VillageBbdCode = GetVillage.VillageBbdCode;
                }
            }
            else
            {
                var GetCurrentReq = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.TransactionType == 4 && w.StatusId == 3).OrderByDescending(o => o.TransactionVillageId).FirstOrDefault();
                // reqId
                ViewBag.ReqId = GetCurrentReq.TransactionVillageId;

                //check transaction by req
                var GetTransactionVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == GetCurrentReq.TransactionVillageId).FirstOrDefault();
                if (GetTransactionVillage != null)
                {
                    Model.VillageId = GetTransactionVillage.VillageId;
                    Model.TransactionVillageId = GetTransactionVillage.TransactionVillageId;
                    Model.UserId = GetTransactionVillage.UserId;
                    Model.VillageCodeText = GetTransactionVillage.VillageCodeText;
                    Model.VillageBbdId = GetTransactionVillage.VillageBbdId;
                    Model.VillageName = GetTransactionVillage.VillageName;
                    Model.VillageAddress = GetTransactionVillage.VillageAddress;
                    Model.VillageMoo = GetTransactionVillage.VillageMoo;
                    Model.ProvinceId = GetTransactionVillage.ProvinceId;
                    Model.DistrictId = GetTransactionVillage.DistrictId;
                    Model.SubDistrictId = GetTransactionVillage.SubDistrictId;
                    Model.PostCode = GetTransactionVillage.PostCode;
                    Model.Phone = GetTransactionVillage.Phone;
                    Model.Email = GetTransactionVillage.Email;
                    Model.BbdDate = GetTransactionVillage.BbdDate;
                    Model.VillageStartDate = GetTransactionVillage.VillageStartDate;
                    Model.VillageEndDate = GetTransactionVillage.VillageEndDate;
                    Model.IsActive = Convert.ToBoolean(GetTransactionVillage.IsActive);
                    Model.UpdateDate = GetTransactionVillage.UpdateDate;
                    Model.UpdateBy = GetTransactionVillage.UpdateBy;
                    Model.OrgId = GetTransactionVillage.OrgId;
                    Model.VillageBbdCode = GetTransactionVillage.VillageBbdCode;
                }
                else
                {
                    var GetVillage = DB.Village.Where(w => w.VillageId == VillageId).FirstOrDefault();
                    Model.VillageId = GetVillage.VillageId;
                    Model.UserId = GetVillage.UserId;
                    Model.VillageCodeText = GetVillage.VillageCodeText;
                    Model.VillageBbdId = GetVillage.VillageBbdId;
                    Model.VillageName = GetVillage.VillageName;
                    Model.VillageAddress = GetVillage.VillageAddress;
                    Model.VillageMoo = GetVillage.VillageMoo;
                    Model.ProvinceId = GetVillage.ProvinceId;
                    Model.DistrictId = GetVillage.DistrictId;
                    Model.SubDistrictId = GetVillage.SubDistrictId;
                    Model.PostCode = GetVillage.PostCode;
                    Model.Phone = GetVillage.Phone;
                    Model.Email = GetVillage.Email;
                    Model.BbdDate = GetVillage.BbdDate;
                    Model.VillageStartDate = GetVillage.VillageStartDate;
                    Model.VillageEndDate = GetVillage.VillageEndDate;
                    Model.IsActive = GetVillage.IsActive;
                    Model.UpdateDate = GetVillage.UpdateDate;
                    Model.UpdateBy = GetVillage.UpdateBy;
                    Model.OrgId = GetVillage.OrgId;
                    Model.VillageBbdCode = GetVillage.VillageBbdCode;
                }
            }

            ViewBag.Province = new SelectList(DB.SystemProvince.ToList(), "ProvinceId", "ProvinceName");

            ViewBag.VillageId = VillageId;
            return View(Model);
        }

        [HttpPost]
        public async Task<IActionResult> PostVillageEdit(FormUpdateViewModel Model, DateCollection Date)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetTra = DB.TransactionVillage.Where(w => w.TransactionVillageId == Model.TransactionVillageId).FirstOrDefault();
                if (GetTra == null)
                {
                    // add new TransactionVillage
                    var TransactioVilage = new TransactionVillage();
                    TransactioVilage.TransactionVillageId = Model.TransactionVillageId;
                    TransactioVilage.UserId = CurrentUser.Id;
                    TransactioVilage.VillageCode = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault().VillageCode;
                    TransactioVilage.VillageBbdId = Model.VillageBbdId;
                    TransactioVilage.VillageName = Model.VillageName;
                    TransactioVilage.VillageAddress = Model.VillageAddress;
                    TransactioVilage.VillageMoo = Model.VillageMoo;
                    TransactioVilage.ProvinceId = Model.ProvinceId;
                    TransactioVilage.DistrictId = Model.DistrictId;
                    TransactioVilage.SubDistrictId = Model.SubDistrictId;
                    TransactioVilage.PostCode = Model.PostCode;
                    TransactioVilage.Phone = Model.Phone;
                    TransactioVilage.Email = Model.Email;
                    TransactioVilage.BbdDate = new DateTime(Date.Year, Date.Month, Date.Day);
                    TransactioVilage.VillageStartDate = DateTime.Now;
                    TransactioVilage.VillageEndDate = DateTime.Now.AddYears(1);
                    TransactioVilage.IsActive = true;
                    TransactioVilage.UpdateBy = CurrentUser.Id;
                    TransactioVilage.UpdateDate = DateTime.Now;
                    TransactioVilage.OrgId = CurrentUser.OrgId;
                    TransactioVilage.VillageCodeText = Model.VillageCodeText;
                    TransactioVilage.VillageBbdCode = Model.VillageBbdCode;
                    DB.TransactionVillage.Add(TransactioVilage);
                    await DB.SaveChangesAsync();
                }
                else
                {
                    // update
                    GetTra.TransactionVillageId = Model.TransactionVillageId;
                    GetTra.UserId = CurrentUser.Id;
                    GetTra.VillageCode = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault().VillageCode;
                    GetTra.VillageBbdId = Model.VillageBbdId;
                    GetTra.VillageName = Model.VillageName;
                    GetTra.VillageAddress = Model.VillageAddress;
                    GetTra.VillageMoo = Model.VillageMoo;
                    GetTra.ProvinceId = Model.ProvinceId;
                    GetTra.DistrictId = Model.DistrictId;
                    GetTra.SubDistrictId = Model.SubDistrictId;
                    GetTra.PostCode = Model.PostCode;
                    GetTra.Phone = Model.Phone;
                    GetTra.Email = Model.Email;
                    GetTra.BbdDate = new DateTime(Date.Year, Date.Month, Date.Day);
                    GetTra.VillageStartDate = DateTime.Now;
                    GetTra.VillageEndDate = DateTime.Now.AddYears(1);
                    GetTra.IsActive = true;
                    GetTra.UpdateBy = CurrentUser.Id;
                    GetTra.UpdateDate = DateTime.Now;
                    GetTra.OrgId = CurrentUser.OrgId;
                    GetTra.VillageCodeText = Model.VillageCodeText;
                    GetTra.VillageBbdCode = Model.VillageBbdCode;
                    DB.TransactionVillage.Update(GetTra);
                    await DB.SaveChangesAsync();
                }

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { valid = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult VillageEditIndex()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EndEdit(int TransactionVillageId, Int64 VillageId, int BankCode, string BookBankId, string BookBankName, TransactionVillage Transaction, DateCollection Date)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {

                // add new record in req
                var GetTransaction = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
                if (GetTransaction == null)
                {
                    var GetVilllage = DB.Village.Where(w => w.VillageId == VillageId).FirstOrDefault();

                    var TranVillage = new TransactionVillage();
                    TranVillage.TransactionVillageId = TransactionVillageId;
                    TranVillage.UserId = GetVilllage.UserId;
                    TranVillage.VillageCode = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault().VillageCode;
                    TranVillage.VillageBbdId = Transaction.VillageBbdId;
                    TranVillage.VillageName = Transaction.VillageName;
                    TranVillage.VillageAddress = Transaction.VillageAddress;
                    TranVillage.VillageMoo = Transaction.VillageMoo;
                    TranVillage.ProvinceId = Transaction.ProvinceId;
                    TranVillage.DistrictId = Transaction.DistrictId;
                    TranVillage.SubDistrictId = Transaction.SubDistrictId;
                    TranVillage.PostCode = Transaction.PostCode;
                    TranVillage.Phone = Transaction.Phone;
                    TranVillage.Email = Transaction.Email;
                    TranVillage.BbdDate = new DateTime(Date.Year, Date.Month, Date.Day);
                    TranVillage.VillageStartDate = GetVilllage.VillageStartDate;
                    TranVillage.VillageEndDate = GetVilllage.VillageEndDate;
                    TranVillage.IsActive = GetVilllage.IsActive;
                    TranVillage.UpdateDate = DateTime.Now;
                    TranVillage.UpdateBy = GetVilllage.UpdateBy;
                    TranVillage.OrgId = GetVilllage.OrgId;
                    TranVillage.VillageCodeText = Transaction.VillageCodeText;
                    TranVillage.VillageBbdCode = Transaction.VillageBbdCode;
                    DB.TransactionVillage.Add(TranVillage);
                    DB.SaveChanges();
                }


                var GetTransVillageReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
                GetTransVillageReq.UserId = CurrentUser.Id;
                GetTransVillageReq.TransactionType = 4;
                GetTransVillageReq.StatusId = 0;
                GetTransVillageReq.UpdateDate = DateTime.Now;
                GetTransVillageReq.UpdateBy = CurrentUser.Id;
                GetTransVillageReq.ApproveDate = DateTime.Now;
                GetTransVillageReq.OrgId = CurrentUser.OrgId;
                DB.TransactionReqVillage.Update(GetTransVillageReq);
                DB.SaveChanges();

            }
            catch (Exception ex)
            {
                return Json(new { valid = false, message = ex });
            }

            return Json(new { valid = true, message = "ยื่นคำขอเปลี่ยนแปลงข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> GetVillageEdit()
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var GetTransaction = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
            ViewBag.TransactionTypeFlag = GetTransaction.TransactionType == 4 ? true : false;

            var GetVillages = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
            if (GetVillages != null)
            {
                ViewBag.IsActive = GetVillages.IsActive;
            }


            return PartialView(GetVillages);
        }

        [HttpPost]
        public IActionResult RenewalVillageApproved(int TransactionVillageId)
        {

            //Update Req Status Id
            var GetTransVillageReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            GetTransVillageReq.TransactionType = 2;
            GetTransVillageReq.StatusId = 1;
            DB.TransactionReqVillage.Update(GetTransVillageReq);
            DB.SaveChanges();

            // TransactionVillage Update 1 year
            var GetTransactionVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            GetTransactionVillage.VillageEndDate = GetTransactionVillage.VillageEndDate.AddYears(1);
            DB.TransactionVillage.UpdateRange(GetTransactionVillage);
            DB.SaveChanges();

            // update village
            var GetVillage = DB.Village.Where(w => w.UserId == GetTransactionVillage.UserId).FirstOrDefault();
            GetVillage.VillageEndDate = GetVillage.VillageEndDate.AddYears(1);
            DB.Village.Update(GetVillage);
            DB.SaveChanges();

            return Json(new { valid = true, message = "อนุมัติสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> VillageDetail(Int64 VillageId)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var GetVillage = DB.Village.Where(w => w.VillageId == VillageId).FirstOrDefault();

            //BdbDate
            ViewBag.BdbDate = Helper.getDateThai(GetVillage.BbdDate);

            //StartDate EndDate
            ViewBag.StartDate = Helper.getDateThai(GetVillage.VillageStartDate);
            ViewBag.EndDate = Helper.getDateThai(GetVillage.VillageEndDate);

            //Address
            ViewBag.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
            ViewBag.District = DB.SystemDistrict.Where(w => w.AmphurId == GetVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
            ViewBag.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
            //Member
            List<Member> Members = new List<Member>();
            var GetMembers = DB.MemberVillage.Where(w => w.VillageId == VillageId).ToList();
            var GetMemberPosition = DB.SystemMemberPosition.ToList();
            var GetMemberStatus = DB.SystemMemberStatus.ToList();
            foreach (var item in GetMembers)
            {
                var Member = new Member();
                Member.MemberId = item.MemberId;
                Member.MemberCode = item.MemberCode;
                Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                Member.MemberPosition = GetMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                Member.MemberStatus = GetMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                Member.Remark = item.KickOffComment;
                Member.Connection = item.Connection;
                Members.Add(Member);
            }
            ViewBag.Members = Members;
            //File
            ViewBag.Files = DB.TransactionFileVillage.Where(w => w.UpdateBy == CurrentUser.Id).ToList();

            //Account

            ViewBag.Helper = Helper;

            ViewBag.Addresses = new List<VillagesViewModel>();

            //Get by statusId = 1
            foreach (var item in DB.TransactionVillage.Where(w => w.UserId == DB.Village.Where(w => w.VillageId == VillageId).Select(s => s.UserId).FirstOrDefault() && DB.TransactionReqVillage.Where(w => w.StatusId == 1).Any(s => s.TransactionVillageId == w.TransactionVillageId)))
            {
                var Model = new VillagesViewModel();
                Model.VillageMoo = item.VillageMoo;
                Model.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == item.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
                Model.District = DB.SystemDistrict.Where(w => w.AmphurId == item.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
                Model.Province = DB.SystemProvince.Where(w => w.ProvinceId == item.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
                Model.PostCode = item.PostCode;
                ViewBag.Addresses.Add(Model);
            }

            return View(GetVillage);
        }

        [HttpGet]
        public async Task<IActionResult> VillageRequestDetail(Int64 TransactionVillageId)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var GetVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();

            //BdbDate
            var BdbDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.BbdDate).FirstOrDefault();
            ViewBag.BdbDate = Helper.getDateThai((DateTime)BdbDate);

            //StartDate EndDate
            var StartDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageStartDate).FirstOrDefault();
            ViewBag.StartDate = Helper.getDateThai(StartDate);
            var EndDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageEndDate).FirstOrDefault();
            @ViewBag.EndDate = Helper.getDateThai(EndDate);

            //Address
            ViewBag.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
            ViewBag.District = DB.SystemDistrict.Where(w => w.AmphurId == GetVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
            ViewBag.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
            //Member
            List<Member> Members = new List<Member>();
            var GetMembers = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToList();
            var GetMemberPosition = DB.SystemMemberPosition.ToList();
            var GetMemberStatus = DB.SystemMemberStatus.ToList();
            foreach (var item in GetMembers)
            {
                var Member = new Member();
                Member.MemberId = item.MemberId;
                Member.MemberCode = item.MemberCode;
                Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                Member.MemberPosition = GetMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                Member.MemberStatus = GetMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                Members.Add(Member);
            }
            ViewBag.Members = Members;
            //File
            ViewBag.Files = DB.TransactionFileVillage.Where(w => w.UpdateBy == CurrentUser.Id).ToList();

            //Account

            var BankCode = DB.AccountBookBank.Where(w => w.UpdateBy == GetVillage.UserId).FirstOrDefault();
            if (BankCode != null)
            {
                ViewBag.BankName = DB.AccountBankMaster.Where(w => w.BankCode == BankCode.BankCode).Select(s => s.BankName).FirstOrDefault();
                ViewBag.BookBankId = BankCode.BookBankId;
                ViewBag.BookBankName = BankCode.BookBankName;
            }
            else
            {
                ViewBag.BankName = "";
                ViewBag.BookBankId = "";
                ViewBag.BookBankName = "";
            }

            ViewBag.Helper = Helper;

            return View(GetVillage);
        }

        [HttpGet]
        public IActionResult AdminVillageEditView(int TransactionVillageId)
        {
            var GetTransVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            TransactionVillage Model = new TransactionVillage();
            Model = GetTransVillage;

            var UserId = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.UserId).FirstOrDefault();

            //BdbDate
            var BdbDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.BbdDate).FirstOrDefault();
            ViewBag.BdbDate = Helper.getDateThai((DateTime)BdbDate);

            //StartDate EndDate
            var StartDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageStartDate).FirstOrDefault();
            ViewBag.StartDate = Helper.getDateThai(StartDate);
            var EndDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageEndDate).FirstOrDefault();
            @ViewBag.EndDate = Helper.getDateThai(EndDate);

            //Address
            ViewBag.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetTransVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
            ViewBag.District = DB.SystemDistrict.Where(w => w.AmphurId == GetTransVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
            ViewBag.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetTransVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
            //Member
            List<Member> Members = new List<Member>();
            var GetMembers = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToList();
            var GetMemberPosition = DB.SystemMemberPosition.ToList();
            var GetMemberStatus = DB.SystemMemberStatus.ToList();
            foreach (var item in GetMembers)
            {
                var Member = new Member();
                Member.MemberId = item.MemberId;
                Member.MemberCode = item.MemberCode;
                Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                Member.MemberPosition = GetMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                Member.MemberStatus = GetMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                Members.Add(Member);
            }
            ViewBag.Members = Members;
            //File
            ViewBag.Files = DB.TransactionFileVillage.Where(w => w.UpdateBy == UserId).ToList();

            ViewBag.TransactionVillageId = TransactionVillageId;

            //Account
            var BankCode = DB.AccountBookBank.Where(w => w.UpdateBy == UserId).FirstOrDefault();
            if (BankCode != null)
            {
                ViewBag.BankName = DB.AccountBankMaster.Where(w => w.BankCode == BankCode.BankCode).Select(s => s.BankName).FirstOrDefault();
                ViewBag.BookBankId = BankCode.BookBankId;
                ViewBag.BookBankName = BankCode.BookBankName;
            }
            else
            {
                ViewBag.BankName = "";
                ViewBag.BookBankId = "";
                ViewBag.BookBankName = "";
            }

            ViewBag.Helper = Helper;

            return View(Model);
        }

        [HttpGet]
        public IActionResult AdminVillageRenewalView(int TransactionVillageId)
        {
            var GetTransVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            TransactionVillage Model = new TransactionVillage();
            Model = GetTransVillage;

            var UserId = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.UserId).FirstOrDefault();

            //BdbDate
            var BdbDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.BbdDate).FirstOrDefault();
            ViewBag.BdbDate = Helper.getDateThai((DateTime)BdbDate);

            //StartDate EndDate
            var StartDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageStartDate).FirstOrDefault();
            ViewBag.StartDate = Helper.getDateThai(StartDate);
            var EndDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageEndDate).FirstOrDefault();
            @ViewBag.EndDate = Helper.getDateThai(EndDate);

            //Address
            ViewBag.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetTransVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
            ViewBag.District = DB.SystemDistrict.Where(w => w.AmphurId == GetTransVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
            ViewBag.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetTransVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
            //Member
            List<Member> Members = new List<Member>();
            var GetMembers = DB.MemberVillage.Where(w => w.UpdateBy == UserId).ToList();
            var GetMemberPosition = DB.SystemMemberPosition.ToList();
            var GetMemberStatus = DB.SystemMemberStatus.ToList();
            foreach (var item in GetMembers)
            {
                var Member = new Member();
                Member.MemberId = item.MemberId;
                Member.MemberCode = item.MemberCode;
                Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                Member.MemberPosition = GetMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                Member.MemberStatus = GetMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                Members.Add(Member);
            }
            ViewBag.Members = Members;
            //File
            ViewBag.Files = DB.TransactionFileVillage.Where(w => w.UpdateBy == UserId).ToList();

            //For Approve DisApprove
            var GetUserIdReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.UserId).FirstOrDefault();
            ViewBag.LastReq = DB.TransactionReqVillage.Where(w => w.UserId == GetUserIdReq).OrderByDescending(o => o.TransactionVillageId).Select(s => s.TransactionVillageId).FirstOrDefault();

            //Account
            var BankCode = DB.AccountBookBank.Where(w => w.UpdateBy == UserId).FirstOrDefault();
            if (BankCode != null)
            {
                ViewBag.BankName = DB.AccountBankMaster.Where(w => w.BankCode == BankCode.BankCode).Select(s => s.BankName).FirstOrDefault();
                ViewBag.BookBankId = BankCode.BookBankId;
                ViewBag.BookBankName = BankCode.BookBankName;
            }
            else
            {
                ViewBag.BankName = "";
                ViewBag.BookBankId = "";
                ViewBag.BookBankName = "";
            }

            ViewBag.Helper = Helper;

            return View(Model);
        }

        [HttpGet]
        public IActionResult AdminVillageTerminateView(int TransactionVillageId)
        {
            var GetTransVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            TransactionVillage Model = new TransactionVillage();
            Model = GetTransVillage;

            var UserId = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.UserId).FirstOrDefault();

            //BdbDate
            var BdbDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.BbdDate).FirstOrDefault();
            ViewBag.BdbDate = Helper.getDateThai((DateTime)BdbDate);

            //StartDate EndDate
            var StartDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageStartDate).FirstOrDefault();
            ViewBag.StartDate = Helper.getDateThai(StartDate);
            var EndDate = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.VillageEndDate).FirstOrDefault();
            ViewBag.EndDate = Helper.getDateThai(EndDate);

            //Address
            ViewBag.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetTransVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
            ViewBag.District = DB.SystemDistrict.Where(w => w.AmphurId == GetTransVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
            ViewBag.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetTransVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
            //Member
            List<Member> Members = new List<Member>();
            var GetMembers = DB.MemberVillage.Where(w => w.UpdateBy == UserId).ToList();
            var GetMemberPosition = DB.SystemMemberPosition.ToList();
            var GetMemberStatus = DB.SystemMemberStatus.ToList();
            foreach (var item in GetMembers)
            {
                var Member = new Member();
                Member.MemberId = item.MemberId;
                Member.MemberCode = item.MemberCode;
                Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                Member.MemberPosition = GetMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                Member.MemberStatus = GetMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                Members.Add(Member);
            }
            ViewBag.Members = Members;
            //File
            ViewBag.Files = DB.TransactionFileVillage.Where(w => w.UpdateBy == UserId).ToList();

            //Account
            var BankCode = DB.AccountBookBank.Where(w => w.UpdateBy == UserId).FirstOrDefault();
            if (BankCode != null)
            {
                ViewBag.BankName = DB.AccountBankMaster.Where(w => w.BankCode == BankCode.BankCode).Select(s => s.BankName).FirstOrDefault();
                ViewBag.BookBankId = BankCode.BookBankId;
                ViewBag.BookBankName = BankCode.BookBankName;
            }
            else
            {
                ViewBag.BankName = "";
                ViewBag.BookBankId = "";
                ViewBag.BookBankName = "";
            }

            ViewBag.Helper = Helper;

            return View(Model);
        }

        [HttpGet]
        public IActionResult VillageList()
        {
            return View("VillageList");
        }

        [HttpGet]
        public IActionResult VillageListDetail()
        {
            return View("VillageListDetail");
        }

        [HttpGet]
        public async Task<IActionResult> GetVillageList()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Model = DB.Village.Where(w => w.OrgId == CurrentUser.OrgId).ToList();
            if (CurrentUser.OrgId == 0 || CurrentUser.OrgId == 1)
            {
                Model = DB.Village.ToList();
            }

            return PartialView(Model);
        }

        [HttpGet]
        public async Task<IActionResult> GetVillageListDetail()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<AdminVillageViewIndex>();
            var GetTransactionVillage = DB.Village.ToList();
            foreach (var Village in GetTransactionVillage)
            {
                string BbdId = GetTransactionVillage.Where(w => w.UserId == Village.UserId).Select(s => s.VillageBbdId).FirstOrDefault();

                var AdminVillageView = new AdminVillageViewIndex();
                AdminVillageView.VillageId = Village.VillageId;
                AdminVillageView.VillageCodeText = DB.Village.Where(w => w.UserId == Village.UserId).Select(s => s.VillageCodeText).FirstOrDefault();
                AdminVillageView.VillageName = DB.Village.Where(w => w.UserId == Village.UserId).Select(s => s.VillageName).FirstOrDefault();
                AdminVillageView.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(BbdId)));
                AdminVillageView.President = DB.MemberVillage.Where(w => w.VillageId == Village.VillageId && w.MemberPositionId == 1).Select(s => s.MemberFirstName).FirstOrDefault() + " " + DB.MemberVillage.Where(w => w.VillageId == Village.VillageId && w.MemberPositionId == 1).Select(s => s.MemberLastName).FirstOrDefault();
                Models.Add(AdminVillageView);
            }


            return PartialView(Models);
        }

        [HttpGet]
        public IActionResult RequestIndex()
        {
            ViewBag.TransactionType = new SelectList(DB.SystemTransactionType.Where(w => w.TransactionTypeId < 5).ToList(), "TransactionTypeId", "TransactionTypeNameTH");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRequest(int TransactionType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));


            string[] StatusName = { "รออนุมัติ", "อนุมัติ", "ไม่อนุมัติ", "รอยื่นเรื่อง" };

            List<VillagesViewModel> Villages = new List<VillagesViewModel>();
            var GetReqVillages = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.TransactionType == TransactionType).ToList();
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);
            if (UserRole == "HeadQuarterAdmin" || UserRole == "officer" || UserRole == "administrator")
            {
                GetReqVillages = DB.TransactionReqVillage.Where(w => w.TransactionType == TransactionType).ToList();
            }
            foreach (var item in GetReqVillages)
            {
                var VillageViewModel = new VillagesViewModel();
                VillageViewModel.VillageCode = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.VillageCode).FirstOrDefault();
                var BdbId = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.VillageBbdId).FirstOrDefault();
                VillageViewModel.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(BdbId)));
                VillageViewModel.VillageName = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.VillageName).FirstOrDefault();
                VillageViewModel.TransactionType = DB.SystemTransactionType.Where(w => w.TransactionTypeId == item.TransactionType).Select(s => s.TransactionTypeNameTH).FirstOrDefault();
                VillageViewModel.UpdateDate = Helper.getDateThai(item.UpdateDate);
                VillageViewModel.ApproveDate = Helper.getDateThai(item.ApproveDate);
                VillageViewModel.TransactionTypeId = item.TransactionType;
                VillageViewModel.IsActive = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.IsActive).FirstOrDefault();
                VillageViewModel.StatusName = StatusName[item.StatusId];
                VillageViewModel.StatusId = item.StatusId;
                VillageViewModel.TransactionVillageId = item.TransactionVillageId;

                if (VillageViewModel.VillageName != null)
                {
                    Villages.Add(VillageViewModel);
                }

            }
            return PartialView(Villages);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateTransactionReqStatus(int StatusId, Int64 TransactionVillageId, string Comment)
        {
            try
            {
                var GetReq = await DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefaultAsync();
                GetReq.StatusId = StatusId;
                GetReq.TransactionDetail = Comment;
                DB.TransactionReqVillage.Update(GetReq);
                await DB.SaveChangesAsync();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> RegisterVillageIndex()
        {

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            if (Helper.GetUserRoleName(CurrentUser.Id) != "user")
            {
                return RedirectToAction("VillageIndex");
            }

            var TransactionVillage = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).ToList().OrderByDescending(o => o.VillageId).FirstOrDefault();
            if (TransactionVillage != null)
            {
                var GetReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillage.TransactionVillageId).FirstOrDefault();
                if (GetReq != null)
                {
                    // If Register is not Approve
                    ViewBag.EditCheck = DB.TransactionReqVillage.Where(w => w.TransactionType == 1 && w.StatusId == 2 || w.StatusId == 3 && w.TransactionVillageId == TransactionVillage.TransactionVillageId).FirstOrDefault();
                }
                //Pending Admin Check
                ViewBag.PendingCheck = DB.TransactionReqVillage.Where(w => w.TransactionType == 1 && w.StatusId == 0 && w.TransactionVillageId == TransactionVillage.TransactionVillageId).FirstOrDefault();
            }

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> FormEditVillageRegister()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var TransactionVillage = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).ToList().OrderByDescending(o => o.VillageId).FirstOrDefault();

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

            var GetThisVillage = DB.TransactionVillage.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();
            var BankCode = DB.AccountBookBank.Where(w => w.UpdateBy == GetThisVillage.UserId).FirstOrDefault();
            if (BankCode != null)
            {
                ViewBag.Banks = new SelectList(DB.AccountBankMaster.ToList(), "BankCode", "BankName", BankCode.BankCode);
                ViewBag.BankCode = BankCode.BankCode;
                ViewBag.BankName = DB.AccountBankMaster.Where(w => w.BankCode == BankCode.BankCode).Select(s => s.BankName).FirstOrDefault();
                ViewBag.BookBankId = BankCode.BookBankId;
                ViewBag.BookBankName = BankCode.BookBankName;
            }
            else
            {
                ViewBag.Banks = new SelectList(DB.AccountBankMaster.ToList(), "BankCode", "BankName");
                ViewBag.BookBankId = "";
                ViewBag.BookBankName = "";
            }
            //Bookbank File
            var GetBookbankFile = DB.TransactionFileBookbank.Where(w => w.UpdateBy == CurrentUser.Id).FirstOrDefault();
            if (GetBookbankFile != null)
            {
                ViewBag.File = GetBookbankFile;
                ViewBag.FileId = GetBookbankFile.TransactionFileBookbankId;
            }

            ViewBag.Province = new SelectList(DB.SystemProvince.ToList(), "ProvinceId", "ProvinceName");

            return View(TransactionVillage);
        }

        [HttpDelete]
        public IActionResult DeleteBookbankFile(int FileId)
        {
            try
            {
                var GetFile = DB.TransactionFileBookbank.Where(w => w.TransactionFileBookbankId == FileId).FirstOrDefault();

                var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/e-account/");
                if (GetFile.GencodeFileName != null)
                {
                    string oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                    if (System.IO.File.Exists(oldfilepath))
                    {
                        System.IO.File.Delete(oldfilepath);
                    }
                }
                DB.TransactionFileBookbank.Remove(GetFile);
                DB.SaveChanges();

                return Json(new { valid = true, message = "ลบข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRegisterVillageRequest()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));


            string[] StatusName = { "รออนุมัติ", "อนุมัติ", "ไม่อนุมัติ", "รอยื่นเรื่อง" };

            List<VillagesViewModel> Villages = new List<VillagesViewModel>();
            var GetReqVillages = DB.TransactionReqVillage.Where(w => w.UserId == CurrentUser.Id && w.TransactionType == 1).ToList();
            foreach (var item in GetReqVillages)
            {
                var VillageViewModel = new VillagesViewModel();
                VillageViewModel.VillageCode = DB.TransactionVillage.Where(w => w.UserId == item.UserId).Select(s => s.VillageCode).FirstOrDefault();
                var BdbId = DB.TransactionVillage.Where(w => w.UserId == item.UserId).Select(s => s.VillageBbdId).FirstOrDefault();
                VillageViewModel.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(BdbId)));
                VillageViewModel.VillageName = DB.TransactionVillage.Where(w => w.UserId == item.UserId).Select(s => s.VillageName).FirstOrDefault();
                VillageViewModel.TransactionType = DB.SystemTransactionType.Where(w => w.TransactionTypeId == item.TransactionType).Select(s => s.TransactionTypeNameTH).FirstOrDefault();
                VillageViewModel.UpdateDate = Helper.getDateThai(item.UpdateDate);
                VillageViewModel.ApproveDate = Helper.getDateThai(item.ApproveDate);
                VillageViewModel.TransactionTypeId = item.TransactionType;
                VillageViewModel.IsActive = DB.TransactionVillage.Where(w => w.UserId == item.UserId).Select(s => s.IsActive).FirstOrDefault();
                VillageViewModel.StatusName = StatusName[item.StatusId];
                VillageViewModel.StatusId = item.StatusId;
                Villages.Add(VillageViewModel);
            }
            return PartialView(Villages);
        }

        [HttpGet]
        public async Task<IActionResult> VillageLicense()
        {
            ViewBag.Helper = Helper;
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var Model = new ViewModels.Villages.VillagesViewModel();
            var GetVillage = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();

            if (GetVillage != null)
            {
                Model.VillageName = GetVillage.VillageName;
                Model.VillageMoo = GetVillage.VillageMoo;
                Model.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
                Model.District = DB.SystemDistrict.Where(w => w.AmphurId == GetVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
                Model.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
                Model._ApproveDate = GetVillage.BbdDate;

            }

            var File = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/village/template/Name.txt");


            try
            {
                using (StreamReader reader = new StreamReader(File))
                {

                    string firstLine = reader.ReadLine();
                    ViewBag.Name = firstLine;
                }
            }
            catch (Exception ex)
            {
                ViewBag.Name = "Could not find file Name.txt";
            }

            return View(Model);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateVillageFileStatus(int TransactionFileId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // update status
                var Get = DB.TransactionFileVillage.Where(w => w.TransactionFileId == TransactionFileId).FirstOrDefault();
                Get.Approvemark = true;
                DB.TransactionFileVillage.Update(Get);
                DB.SaveChanges();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "เปลี่ยนสถานะไฟล์ e-Register", HttpContext, "e-Register");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #region รายงาน
        [HttpGet]
        public async Task<IActionResult> ReportStatus()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.CurrentBudgetYear = (DateTime.Now.Year < 2500 ? DateTime.Now.Year + 543 : DateTime.Now.Year);
            return View("ReportStatus");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportStatus(int Years)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();
            String[] Months = { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            for (int i = 1; i <= 12; i++)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                int Y = (Years > 2500 ? Years - 543 : Years);
                Model.Year = Years.ToString();
                Model.Month = Months[i - 1];
                Model.AmountReq = DB.TransactionReqVillage.Where(w => w.UpdateDate.Year == Y && w.UpdateDate.Month == i && w.TransactionType == 1).Count();
                Model.AmountEdit = DB.TransactionReqVillage.Where(w => w.UpdateDate.Year == Y && w.UpdateDate.Month == i && w.TransactionType == 4).Count();
                Model.AmountCancle = DB.TransactionReqVillage.Where(w => w.UpdateDate.Year == Y && w.UpdateDate.Month == i && w.TransactionType == 3).Count();
                Model.AmountExp = DB.TransactionReqVillage.Where(w => w.UpdateDate.Year == Y && w.UpdateDate.Month == i && w.TransactionType == 2).Count();
                Model.Amount = DB.TransactionReqVillage.Where(w => w.UpdateDate.Year == Y && w.UpdateDate.Month == i).Count();
                Models.Add(Model);
            }

            return PartialView("GetReportStatus", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportNameVillage()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.ProvinecId = new SelectList(DB.SystemProvince.Select(s => new { Id = s.ProvinceId, Name = s.ProvinceName }).ToList(), "Id", "Name");

            return View("ReportNameVillage");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportNameVillage(int ProvinecId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();
            var Gets = DB.Village.Where(w => w.ProvinceId == ProvinecId && w.IsActive == true).ToList();
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                Model.Province = DB.SystemProvince.Where(w => w.ProvinceId == ProvinecId).Select(s => s.ProvinceName).FirstOrDefault();
                Model.Name = Get.VillageName;
                Models.Add(Model);
            }

            return PartialView("GetReportNameVillage", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportNameOrg()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.OrgId = new SelectList(DB.SystemOrgStructures.Select(s => new { Id = s.OrgId, Name = s.OrgName }).ToList(), "Id", "Name");

            return View("ReportNameOrg");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportNameOrg(int OrgId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();
            var Gets = DB.Village.Where(w => w.OrgId == OrgId && w.IsActive == true).ToList();
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                Model.Province = DB.SystemOrgStructures.Where(w => w.OrgId == OrgId).Select(s => s.OrgName).FirstOrDefault();
                Model.Name = Get.VillageName;
                Models.Add(Model);
            }

            return PartialView("GetReportNameOrg", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportNameChairman()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.ProvinecId = new SelectList(DB.SystemProvince.Select(s => new { Id = s.ProvinceId, Name = s.ProvinceName }).ToList(), "Id", "Name");

            return View("ReportNameChairman");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportNameChairman(int ProvinecId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<object>();
            var Gets = DB.Village.Where(w => w.ProvinceId == ProvinecId && w.IsActive == true).ToList();
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                if (DB.MemberVillage.Where(w => w.VillageId == Get.VillageId && w.MemberPositionId == 1).Count() > 0)
                {
                    var Member = DB.MemberVillage.Where(w => w.VillageId == Get.VillageId && w.MemberPositionId == 1).FirstOrDefault();
                    Models.Add(new
                    {
                        VillageName = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageName).FirstOrDefault(),
                        Name = Member.MemberFirstName + " " + Member.MemberLastName,
                        Status = DB.SystemMemberStatus.Where(w => w.StatusId == Member.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault(),
                        Phone = Member.Phone
                    });
                }
            }

            return PartialView("GetReportNameChairman", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMember()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();

            var Gets = DB.Village.Where(w => w.IsActive == true).ToList().GroupBy(g => g.OrgId);
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                Model.SubName = DB.SystemOrgStructures.Where(w => w.OrgId == Get.Key).Select(s => s.OrgName).FirstOrDefault();
                Model.Amount = DB.Village.Where(w => w.IsActive == true && w.OrgId == Get.Key).Count();
                Model.AmountExp = DB.Village.Where(w => w.IsActive == true).Count();
                Models.Add(Model);
            }

            return View("ReportMember", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMemberByProvince()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();

            var Gets = DB.Village.Where(w => w.IsActive == true).ToList().GroupBy(g => g.ProvinceId);
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                Model.Province = DB.SystemProvince.Where(w => w.ProvinceId == Get.Key).Select(s => s.ProvinceName).FirstOrDefault();
                Model.Amount = DB.Village.Where(w => w.IsActive == true && w.ProvinceId == Get.Key).Count();
                Model.AmountExp = DB.Village.Where(w => w.IsActive == true).Count();
                Models.Add(Model);
            }

            return View("ReportMemberByProvince", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMemberAge()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();

            var Gets = DB.MemberVillage.ToList().GroupBy(g => g.MemberStartDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                var Years = Get.Select(s => s.MemberStartDate.Year).FirstOrDefault();
                Model.Year = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.AmountExp = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years && DateTime.Now.Year - w.MemberBirthDate.Year < 30).Count();
                Model.AmountReq = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years && DateTime.Now.Year - w.MemberBirthDate.Year >= 30 && DateTime.Now.Year - w.MemberBirthDate.Year <= 50).Count();
                Model.AmountCancle = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years && DateTime.Now.Year - w.MemberBirthDate.Year > 50).Count();
                Model.Amount = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years).Count();
                Models.Add(Model);
            }

            return View("ReportMemberAge", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMemberGender()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();

            var Gets = DB.MemberVillage.ToList().GroupBy(g => g.MemberStartDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                var Years = Get.Select(s => s.MemberStartDate.Year).FirstOrDefault();
                Model.Year = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.AmountExp = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years && w.GenderId == 0).Count();
                Model.AmountReq = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years && w.GenderId == 1).Count();
                Model.AmountCancle = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years && w.GenderId == 2).Count();
                Model.Amount = DB.MemberVillage.Where(w => w.MemberStartDate.Year == Years).Count();
                Models.Add(Model);
            }

            return View("ReportMemberGender", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMemberOccupation()
        {
            //Master
            var GetMasterOccuption = DB.SystemOccupationMaster.ToList();

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();
            var Gets = DB.MemberVillage.ToList().GroupBy(g => g.MemberOccupation).Select(s => new { Count = s.Count(), Occupation = s.Max(m => m.MemberOccupation) }).OrderByDescending(o => o.Count);
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                Model.Name = GetMasterOccuption.Where(w => w.Id == Get.Occupation).Select(s => s.Name).FirstOrDefault();
                Model.AmountExp = Get.Count;
                Model.Amount = DB.MemberVillage.Count();
                Models.Add(Model);
            }

            return View("ReportMemberOccupation", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMemberBank()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ViewModels.Villages.ReportViewModel>();
            var Gets = DB.AccountBankMaster.ToList();
            foreach (var Get in Gets)
            {
                var Model = new ViewModels.Villages.ReportViewModel();
                Model.Name = Get.BankName;
                Model.SubName = Get.BankShotName;
                Model.AmountExp = DB.AccountBookBank.Where(w => w.IsBrance == true && w.BankCode == Get.BankCode).Count();
                Model.Amount = DB.AccountBookBank.Where(w => w.IsBrance == true).Count();
                if (Model.AmountExp > 0)
                    Models.Add(Model);
            }

            return View("ReportMemberBank", Models);
        }

        #endregion

        #region  helper
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
        public IActionResult GetPostcode(int SubDistrictId)
        {

            var Subdistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == SubDistrictId).FirstOrDefault();


            return Json(new { message = Subdistrict.PostCode });

        }

        [HttpGet]
        public IActionResult GetDistrict(int ProvinceId)
        {
            List<SelectListItem> Districts = new List<SelectListItem>();
            var GetDistricts = DB.SystemDistrict.Where(w => w.ProvinceId == ProvinceId).ToList();
            foreach (var item in GetDistricts)
            {
                Districts.Add(
                    new SelectListItem()
                    {
                        Text = item.AmphurName,
                        Value = item.AmphurId.ToString()
                    });
            }
            ViewBag.District = Districts;


            return Json(ViewBag.District);
        }

        [HttpGet]
        public IActionResult GetSubDistrict(int AmphurId)
        {
            List<SelectListItem> SubDistricts = new List<SelectListItem>();
            var GetSubDistricts = DB.SystemSubDistrict.Where(w => w.AmphurId == AmphurId).ToList();
            foreach (var item in GetSubDistricts)
            {
                SubDistricts.Add(
                    new SelectListItem()
                    {
                        Text = item.SubdistrictName,
                        Value = item.SubdistrictId.ToString()
                    });
            }
            ViewBag.District = SubDistricts;


            return Json(ViewBag.District);
        }

        #endregion

        #region Admin

        [HttpGet]
        public async Task<IActionResult> AdminVillagesIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);

            ViewBag.TransactionType = new SelectList(DB.SystemTransactionType.Where(w => w.TransactionTypeId < 5).ToList(), "TransactionTypeId", "TransactionTypeNameTH");
            ViewBag.Role = UserRole;

            return View("AdminVillagesIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetRegisterVillages(int TransactionType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);
            var Villages = new List<AdminVillageViewIndex>();
            var GetTransactionReqVillage = DB.TransactionReqVillage.Where(w => w.TransactionType == TransactionType && w.StatusId == 0).ToList();

            if (UserRole == "BranchAdmin")
            {
                GetTransactionReqVillage = DB.TransactionReqVillage.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).Any(s => s.OrgId == w.OrgId) && w.TransactionType == TransactionType && w.StatusId == 0 && w.IsAlreadyCheck == false).ToList();
            }
            if (UserRole == "ProvinceAdmin")
            {
                GetTransactionReqVillage = DB.TransactionReqVillage.Where(w => w.OrgId == DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).Select(s => s.OrgId).FirstOrDefault() && w.TransactionType == TransactionType && w.StatusId == 0 && w.IsAlreadyCheck == false).ToList();
            }

            foreach (var Village in GetTransactionReqVillage)
            {
                if (DB.TransactionVillage.Where(w => w.UserId == Village.UserId).Count() > 0)
                {
                    if (Village.TransactionType == 2)
                    {

                        if (DB.MemberVillage.Where(w => w.VillageId == DB.Village.Where(w => w.UserId == Village.UserId).Select(s => s.VillageId).FirstOrDefault() && w.MemberRenewal == true).Count() > 0)
                        {
                            if (Villages.Where(w => w.VillageId == DB.Village.Where(w => w.UserId == Village.UserId).Select(s => s.VillageId).FirstOrDefault()).Count() < 1)
                            {
                                string BbdId = DB.TransactionVillage.Where(w => w.UserId == Village.UserId).Select(s => s.VillageBbdId).FirstOrDefault();

                                var AdminVillageView = new AdminVillageViewIndex();
                                AdminVillageView.TransactionVillageId = Village.TransactionVillageId;
                                AdminVillageView.VillageCode = DB.TransactionVillage.Where(w => w.TransactionVillageId == Village.TransactionVillageId).Select(s => s.VillageCode).FirstOrDefault();
                                AdminVillageView.VillageName = DB.TransactionVillage.Where(w => w.UserId == Village.UserId).Select(s => s.VillageName).FirstOrDefault();
                                AdminVillageView.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(BbdId)));
                                AdminVillageView.StatusId = Village.StatusId;
                                AdminVillageView.VillageCodeText = DB.Village.Where(w => w.UserId == Village.UserId).Select(s => s.VillageCodeText).FirstOrDefault();
                                AdminVillageView.TransactionType = Village.TransactionType;
                                AdminVillageView.VillageId = DB.Village.Where(w => w.UserId == Village.UserId).Select(s => s.VillageId).FirstOrDefault();

                                Villages.Add(AdminVillageView);
                            }
                        }
                    }
                    else
                    {
                        string BbdId = DB.TransactionVillage.Where(w => w.UserId == Village.UserId).Select(s => s.VillageBbdId).FirstOrDefault();

                        var AdminVillageView = new AdminVillageViewIndex();
                        AdminVillageView.TransactionVillageId = Village.TransactionVillageId;
                        AdminVillageView.VillageCode = DB.TransactionVillage.Where(w => w.TransactionVillageId == Village.TransactionVillageId).Select(s => s.VillageCode).FirstOrDefault();
                        AdminVillageView.VillageName = DB.TransactionVillage.Where(w => w.UserId == Village.UserId).Select(s => s.VillageName).FirstOrDefault();
                        AdminVillageView.VillageBbdId = (String.Format("{0:0-0000-00000-00-0}", Convert.ToInt64(BbdId)));
                        AdminVillageView.StatusId = Village.StatusId;
                        AdminVillageView.VillageCodeText = DB.TransactionVillage.Where(w => w.TransactionVillageId == Village.TransactionVillageId).Select(s => s.VillageCodeText).FirstOrDefault();
                        AdminVillageView.TransactionType = Village.TransactionType;

                        Villages.Add(AdminVillageView);
                    }
                }
            }

            return PartialView(Villages);
        }

        [HttpGet]
        public async Task<IActionResult> AdminVillageView(int TransactionVillageId, int Type)
        {
            var GetTransVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            TransactionVillage Model = new TransactionVillage();
            Model = GetTransVillage;

            //BdbDate
            ViewBag.BdbDate = Helper.getDateThai(GetTransVillage.BbdDate);

            //StartDate EndDate
            ViewBag.StartDate = Helper.getDateThai(GetTransVillage.VillageStartDate);
            ViewBag.EndDate = Helper.getDateThai(GetTransVillage.VillageEndDate);

            //Address
            ViewBag.Province = DB.SystemProvince.Where(w => w.ProvinceId == GetTransVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
            ViewBag.District = DB.SystemDistrict.Where(w => w.AmphurId == GetTransVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault();
            ViewBag.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetTransVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault();
            //Member
            List<Member> Members = new List<Member>();
            var GetMembers = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToList();
            var GetMemberPosition = DB.SystemMemberPosition.ToList();
            var GetMemberStatus = DB.SystemMemberStatus.ToList();
            foreach (var item in GetMembers)
            {
                var Member = new Member();
                Member.MemberId = item.MemberId;
                Member.MemberCode = item.MemberCode;
                Member.FullName = item.MemberFirstName + " " + item.MemberLastName;
                Member.MemberPosition = GetMemberPosition.Where(w => w.PositionId == item.MemberPositionId).Select(s => s.PositionNameTH).FirstOrDefault();
                Member.MemberStatus = GetMemberStatus.Where(w => w.StatusId == item.MemberStatusId).Select(s => s.StatusNameTH).FirstOrDefault();
                Member.Remark = item.KickOffComment;
                Member.Connection = item.Connection;
                Members.Add(Member);
            }
            ViewBag.Members = Members;

            //File
            ViewBag.Files = DB.TransactionFileVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToList();

            var OrgStrus = new List<SelectListItem>();
            var Gets = await DB.SystemOrgStructures.Where(w => w.ParentId == 0).OrderBy(w => w.OrgId).ToListAsync();
            foreach (var Get in Gets)
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

            ViewBag.Orgs = OrgStrus;

            //Account

            ViewBag.BookbankFiles = new List<ViewModels.EAccount.BookBankListViewModel>();

            var GetBookbank = await DB.AccountBookBank.Where(w => w.UpdateBy == Model.UserId).ToListAsync();
            foreach (var Get in GetBookbank)
            {

                var BookBankModel = new BookBankListViewModel();
                BookBankModel.AccountName = Get.BookBankName + " (" + Get.BookBankId + ")";
                BookBankModel.BankName = DB.AccountBankMaster.Where(w => w.BankCode == Get.BankCode).Select(s => s.BankName + " (" + s.BankShotName + ")").FirstOrDefault();
                BookBankModel.Id = Get.ProjectBbId;
                ViewBag.BookbankFiles.Add(BookBankModel);
            }

            ViewBag.Helper = Helper;

            ViewBag.Type = Type;

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            ViewBag.Role = Helper.GetRoleUser(CurrentUser.Id);

            return View(Model);
        }

        [HttpPut]
        public async Task<IActionResult> VillagesApprove(int TransactionVillageId, int OrgId, int Type)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                string UserRole = Helper.GetRoleUser(CurrentUser.Id);

                //For ProvinceAdmin or BranchAdmin
                bool IsAlreadyCheck = UserRole == "BranchAdmin" || UserRole == "ProvinceAdmin" ? true : false;

                /*
                   Type = 1	ขึ้นทะเบียน
                   Type = 2	ต่ออายุ
                   Type = 3	ยกเลิก
                   Type = 4	เปลี่ยนแปลงข้อมูล                 
                 */

                if (Type == 1)
                {
                    await VillagesApproveAdmin(TransactionVillageId, OrgId, IsAlreadyCheck);
                }
                if (Type == 4)
                {
                    VillagesApproveEdit(TransactionVillageId, IsAlreadyCheck);
                }
                if (Type == 3)
                {
                    TerminateVillageApproved(TransactionVillageId, IsAlreadyCheck);
                }

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        #endregion

        #region Admin Helper
        public async Task VillagesApproveAdmin(int TransactionVillageId, int OrgId, bool IsAlreadyCheck)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetTransactionReqVillage = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();

            //For ProvinceAdmin or BranchAdmin
            if (IsAlreadyCheck == true)
            {
                GetTransactionReqVillage.IsAlreadyCheck = true;
                DB.TransactionReqVillage.Update(GetTransactionReqVillage);
                DB.SaveChanges();
                return;
            }

            //Check Data
            if (DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId && w.StatusId == 0 && w.TransactionType == 1).Count() == 0)
            {
                return;
            }

            GetTransactionReqVillage.TransactionType = 1;
            GetTransactionReqVillage.StatusId = 1;
            GetTransactionReqVillage.UpdateDate = DateTime.Now;
            GetTransactionReqVillage.UpdateBy = CurrentUser.Id;
            GetTransactionReqVillage.ApproveDate = DateTime.Now;
            GetTransactionReqVillage.ApproveBy = CurrentUser.Id;
            DB.TransactionReqVillage.Update(GetTransactionReqVillage);
            DB.SaveChanges();

            //Insert Village Data
            var GetTransactionVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            var Village = new Village();

            Village.UserId = GetTransactionVillage.UserId;
            Village.VillageCode = GetTransactionVillage.VillageCode;
            Village.VillageBbdId = GetTransactionVillage.VillageBbdId;
            Village.VillageName = GetTransactionVillage.VillageName;
            Village.VillageAddress = GetTransactionVillage.VillageAddress;
            Village.VillageMoo = GetTransactionVillage.VillageMoo;
            Village.ProvinceId = GetTransactionVillage.ProvinceId;
            Village.DistrictId = GetTransactionVillage.DistrictId;
            Village.SubDistrictId = GetTransactionVillage.SubDistrictId;
            Village.PostCode = GetTransactionVillage.PostCode;
            Village.Phone = GetTransactionVillage.Phone;
            Village.Email = GetTransactionVillage.Email;
            Village.BbdDate = GetTransactionVillage.BbdDate;
            Village.VillageStartDate = DateTime.Now;
            Village.VillageEndDate = DateTime.Now.AddYears(1);
            Village.IsActive = true;
            Village.UpdateDate = DateTime.Now;
            Village.UpdateBy = CurrentUser.Id;
            Village.VillageCodeText = GetTransactionVillage.VillageCodeText;
            Village.VillageBbdCode = GetTransactionVillage.VillageBbdCode;
            DB.Village.Add(Village);
            DB.SaveChanges();

            //Move Transaction Member To MemberVillage
            //Insert Member Data
            var GetTransMember = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToList();
            var GetVillageId = DB.Village.Where(w => w.UserId == GetTransactionVillage.UserId).Select(s => s.VillageId).FirstOrDefault();
            foreach (var item in GetTransMember)
            {
                var Member = new MemberVillage();
                Member.VillageId = GetVillageId;
                Member.MemberCode = item.MemberCode;
                Member.CitizenId = item.CitizenId;
                Member.MemberFirstName = item.MemberFirstName;
                Member.MemberLastName = item.MemberLastName;
                Member.GenderId = item.GenderId;
                Member.Age = DateTime.Now.Year - item.MemberBirthDate.Year;
                Member.Phone = item.Phone;
                Member.MemberOccupation = item.MemberOccupation;
                Member.MemberAddress = item.MemberAddress;
                Member.MemberPositionId = item.MemberPositionId;
                Member.MemberStatusId = item.MemberStatusId;
                Member.MemberStartDate = item.MemberDate;
                Member.MemberEndDate = item.MemberDate.AddYears(1);
                Member.UpdateDate = item.UpdateDate;
                Member.UpdateBy = item.UpdateBy;
                Member.MemberBirthDate = item.MemberBirthDate;
                Member.KickOffComment = item.KickOffComment;
                Member.Connection = item.Connection;
                DB.MemberVillage.Add(Member);
                DB.SaveChanges();
            }

            //Add OrgId
            GetTransactionReqVillage.OrgId = OrgId;
            DB.TransactionReqVillage.Update(GetTransactionReqVillage);
            GetTransactionVillage.OrgId = OrgId;
            DB.TransactionVillage.Update(GetTransactionVillage);
            var GetVillage = DB.Village.Where(w => w.UserId == GetTransactionVillage.UserId).FirstOrDefault();
            GetVillage.OrgId = OrgId;
            DB.Village.Update(GetVillage);
            var GetUser = DB.Users.Where(w => w.Id == GetTransactionVillage.UserId).FirstOrDefault();
            GetUser.OrgId = OrgId;
            GetUser.VillageId = GetVillage.VillageId;
            DB.Users.Update(GetUser);
            DB.SaveChanges();

            //Change User To SuperUser Role
            var RoleThisUser = DB.UserRoles.Where(w => w.UserId == GetTransactionVillage.UserId).FirstOrDefault();
            DB.UserRoles.Remove(RoleThisUser);
            DB.SaveChanges();

            var ThisUser = await _userManager.FindByIdAsync(GetTransactionVillage.UserId);
            var roleresult = await _userManager.AddToRoleAsync(ThisUser, "SUPERUSER");

            var UserId = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(s => s.UserId).FirstOrDefault();
            var GetBookBankId = DB.AccountBookBank.Where(w => w.UpdateBy == UserId).FirstOrDefault();
            if (GetBookBankId != null)
            {
                GetBookBankId.OrgId = OrgId;
                DB.AccountBookBank.Update(GetBookBankId);
                DB.SaveChanges();
            }
        }

        public async void VillagesApproveEdit(int TransactionVillageId, bool IsAlreadyCheck)
        {
            // get current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            //Req Update
            var GetTransactionReqVillage = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();

            //For ProvinceAdmin or BranchAdmin
            if (IsAlreadyCheck == true)
            {
                GetTransactionReqVillage.IsAlreadyCheck = true;
                DB.TransactionReqVillage.Update(GetTransactionReqVillage);
                DB.SaveChanges();
                return;
            }

            //Check Data
            if (DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId && w.StatusId == 0 && w.TransactionType == 4).Count() == 0)
            {
                return;
            }

            GetTransactionReqVillage.TransactionType = 4;
            GetTransactionReqVillage.StatusId = 1;
            GetTransactionReqVillage.UpdateDate = DateTime.Now;
            GetTransactionReqVillage.UpdateBy = CurrentUser.Id;
            GetTransactionReqVillage.ApproveDate = DateTime.Now;
            GetTransactionReqVillage.ApproveBy = CurrentUser.Id;
            DB.TransactionReqVillage.Update(GetTransactionReqVillage);
            DB.SaveChanges();

            //Update Village Data
            var GetTransactionVillage = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            var Village = DB.Village.Where(w => w.UserId == GetTransactionVillage.UserId).FirstOrDefault();

            Village.VillageCode = GetTransactionVillage.VillageCode;
            Village.VillageBbdId = GetTransactionVillage.VillageBbdId;
            Village.VillageName = GetTransactionVillage.VillageName;
            Village.VillageAddress = GetTransactionVillage.VillageAddress;
            Village.VillageMoo = GetTransactionVillage.VillageMoo;
            Village.ProvinceId = GetTransactionVillage.ProvinceId;
            Village.DistrictId = GetTransactionVillage.DistrictId;
            Village.SubDistrictId = GetTransactionVillage.SubDistrictId;
            Village.PostCode = GetTransactionVillage.PostCode;
            Village.Phone = GetTransactionVillage.Phone;
            Village.Email = GetTransactionVillage.Email;
            Village.BbdDate = GetTransactionVillage.BbdDate;
            Village.UpdateDate = DateTime.Now;
            Village.UpdateBy = CurrentUser.Id;
            Village.VillageCodeText = GetTransactionVillage.VillageCodeText;
            DB.Village.Update(Village);
            DB.SaveChanges();

            //Insert Member Data
            var GetTransMember = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == TransactionVillageId).ToList();
            var GetVillageId = DB.Village.Where(w => w.UserId == GetTransactionVillage.UserId).Select(s => s.VillageId).FirstOrDefault();
            //Remove Member
            var GetMember = DB.MemberVillage.Where(w => w.VillageId == GetVillageId).ToList();
            foreach (var item in GetMember)
            {
                DB.MemberVillage.Remove(item);
                DB.SaveChanges();
            }
            foreach (var item in GetTransMember)
            {
                var Member = new MemberVillage();
                Member.VillageId = GetVillageId;
                Member.MemberCode = item.MemberCode;
                Member.CitizenId = item.CitizenId;
                Member.MemberFirstName = item.MemberFirstName;
                Member.MemberLastName = item.MemberLastName;
                Member.GenderId = item.GenderId;
                Member.Age = item.Age;
                Member.Phone = item.Phone;
                Member.MemberOccupation = item.MemberOccupation;
                Member.MemberAddress = item.MemberAddress;
                Member.MemberPositionId = item.MemberPositionId;
                Member.MemberStatusId = item.MemberStatusId;
                Member.MemberStartDate = item.MemberDate;
                Member.MemberEndDate = item.MemberDate.AddYears(1);
                Member.UpdateDate = item.UpdateDate;
                Member.UpdateBy = item.UpdateBy;
                Member.MemberBirthDate = item.MemberBirthDate;
                Member.KickOffComment = item.KickOffComment;
                Member.Connection = item.Connection;
                DB.MemberVillage.Add(Member);
                DB.SaveChanges();
            }

            //Account
            var UserId = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
            var GetBookBankId = DB.AccountBookBank.Where(w => w.UpdateBy == UserId.UserId).FirstOrDefault();
            if (GetBookBankId != null)
            {
                GetBookBankId.OrgId = UserId.OrgId;
                DB.AccountBookBank.Update(GetBookBankId);
                DB.SaveChanges();
            }
        }

        public void TerminateVillageApproved(int TransactionVillageId, bool IsAlreadyCheck)
        {
            //Update Req Status Id
            var GetTransVillageReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();

            //For ProvinceAdmin or BranchAdmin
            if (IsAlreadyCheck == true)
            {
                GetTransVillageReq.IsAlreadyCheck = true;
                DB.TransactionReqVillage.Update(GetTransVillageReq);
                DB.SaveChanges();
                return;
            }

            //Check Data
            if (DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId && w.StatusId == 0 && w.TransactionType == 3).Count() == 0)
            {
                return;
            }

            GetTransVillageReq.TransactionType = 3;
            GetTransVillageReq.StatusId = 1;
            DB.TransactionReqVillage.Update(GetTransVillageReq);

            //Update Village Status
            var GetTransactionVillageUserId = DB.TransactionVillage.Where(w => w.TransactionVillageId == TransactionVillageId).Select(w => w.UserId).FirstOrDefault();
            var GetVillage = DB.Village.Where(w => w.UserId == GetTransactionVillageUserId).FirstOrDefault();
            GetVillage.IsActive = false;
            DB.Village.Update(GetVillage);

            //Update User Status
            var User = DB.Users.Where(w => w.VillageId == GetVillage.VillageId).FirstOrDefault();
            User.Status = false;
            DB.Users.Update(User);

            //Save
            DB.SaveChanges();

        }

        [HttpPut]
        public IActionResult Disapproved(int TransactionVillageId, int TransactionType, string Comment)
        {

            try
            {
                var GetTransVillageReq = DB.TransactionReqVillage.Where(w => w.TransactionVillageId == TransactionVillageId).FirstOrDefault();
                GetTransVillageReq.TransactionType = TransactionType;
                GetTransVillageReq.StatusId = 2;
                GetTransVillageReq.TransactionDetail = Comment;
                DB.TransactionReqVillage.Update(GetTransVillageReq);
                DB.SaveChanges();
                return Json(new { valid = true, message = "ดำเนินการสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportEregister()
        {
            try
            {
                //Master

                //Export Excel
                var templateFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/village/template/Export_e-register.xlsx");
                FileInfo templateFile = new FileInfo(templateFilePath);
                var newFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/village/export/Export_e-register.xlsx");
                FileInfo newFile = new FileInfo(newFilePath);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage package = new ExcelPackage(newFile, templateFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    int RowStart = 2;

                    //
                    foreach (var item in DB.TransactionReqVillage.Where(w => w.IsAlreadyCheck == true && w.StatusId == 0).ToList())
                    {
                        worksheet.Cells["A" + RowStart].Value = +item.TransactionVillageId;
                        worksheet.Cells["A" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["B" + RowStart].Value = DB.TransactionVillage.Where(w => w.TransactionVillageId == item.TransactionVillageId).Select(s => s.VillageCodeText).FirstOrDefault();
                        worksheet.Cells["B" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["C" + RowStart].Value = DB.TransactionVillage.Where(w => w.TransactionVillageId == item.TransactionVillageId).Select(s => s.VillageName).FirstOrDefault();
                        worksheet.Cells["C" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["D" + RowStart].Value = "[" + item.TransactionType + "]" + DB.SystemTransactionType.Where(w => w.TransactionTypeId == item.TransactionType).Select(s => s.TransactionTypeNameTH).FirstOrDefault();
                        worksheet.Cells["D" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["E" + RowStart].Value = item.OrgId;
                        worksheet.Cells["E" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["F" + RowStart].Value = "ผ่านการตรวจสอบแล้ว";
                        worksheet.Cells["F" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }

                    foreach (var item in DB.TransactionReqVillage.Where(w => w.TransactionType == 2 && DB.MemberVillage.Where(w => w.MemberRenewal == true).Select(s => s.UpdateBy).Contains(w.UserId)).Select(s => new { s.UserId }).Distinct())
                    {
                        worksheet.Cells["A" + RowStart].Value = item.UserId;
                        worksheet.Cells["A" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["B" + RowStart].Value = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.VillageCodeText).FirstOrDefault();
                        worksheet.Cells["B" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["C" + RowStart].Value = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.VillageName).FirstOrDefault();
                        worksheet.Cells["C" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["D" + RowStart].Value = "[" + 2 + "]" + "ต่ออายุสมาชิกกองทุนหมู่บ้าน";
                        worksheet.Cells["D" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["E" + RowStart].Value = DB.Village.Where(w => w.UserId == item.UserId).Select(s => s.OrgId).FirstOrDefault();
                        worksheet.Cells["E" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["F" + RowStart].Value = "ผ่านการตรวจสอบแล้ว";
                        worksheet.Cells["F" + RowStart].Style.Font.Size = 12;

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

        [HttpGet]
        public IActionResult FormImportEregister()
        {
            return PartialView("FormImportEregister");
        }

        [HttpPost]
        public async Task<IActionResult> ImportEregister(IFormFile FileUpload)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                //Head Admin only
                bool IsAlreadyCheck = false;

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

                        /*
                           Type = 1	ขึ้นทะเบียน
                           Type = 2	ต่ออายุ
                           Type = 3	ยกเลิก
                           Type = 4	เปลี่ยนแปลงข้อมูล                 
                        */

                        for (int Row = 2; Row <= worksheet.Dimension.End.Row; Row++)
                        {

                            if (Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString())) == 1)
                            {
                                await VillagesApproveAdmin(Convert.ToInt32(worksheet.Cells["A" + Row].Value.ToString()), Convert.ToInt32(worksheet.Cells["E" + Row].Value.ToString()), IsAlreadyCheck);
                            }

                            if (Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString())) == 4)
                            {
                                VillagesApproveEdit(Convert.ToInt32(worksheet.Cells["A" + Row].Value.ToString()), IsAlreadyCheck);
                            }

                            if (Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString())) == 3)
                            {
                                TerminateVillageApproved(Convert.ToInt32(worksheet.Cells["A" + Row].Value.ToString()), IsAlreadyCheck);
                            }

                            if (Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString())) == 2)
                            {
                                var Get = DB.MemberVillage.Where(w => w.UpdateBy == worksheet.Cells["A" + Row].Value.ToString() && w.MemberRenewal == true).ToList();

                                if (Get.Count() > 0)
                                {
                                    foreach (var item in Get)
                                    {
                                        item.MemberRenewal = false;
                                    }
                                    DB.MemberVillage.UpdateRange(Get);
                                    DB.SaveChanges();
                                }

                            }

                        }
                    }
                }
                else
                {
                    return Json(new { valid = false, message = " ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                }

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error });
            }
        }

        public string GetSubstringByString(string a, string b, string c)
        {
            return c.Substring((c.IndexOf(a) + a.Length), (c.IndexOf(b) - c.IndexOf(a) - a.Length));
        }

        #endregion

    }
}
