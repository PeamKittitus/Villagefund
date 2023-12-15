using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.EOffice;
using DZ_VILLAGEFUND_WEB.ViewModels.EProjects;
using DZ_VILLAGEFUND_WEB.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class EOfficeController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IHostingEnvironment _environment;
        private readonly IConfiguration _configuration;

        public EOfficeController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext db,
            IConfiguration configuration,
            IHostingEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
            _environment = environment;
        }


        #region  sarasan

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Archives()
        {
            List<EOfficeOrganizations> Organization = new List<EOfficeOrganizations>();
            List<EOfficeArchivesType> ArchivesType = new List<EOfficeArchivesType>();

            ArchivesType = DB.EOfficeArchivesType.Where(w => new string[] { "03", "04" }.Contains(w.TypeCode)).ToList();
            var Months = Enumerable.Range(1, 12).Select(i => new { I = i, M = Helper.getMonthThai(i) });
            var BugetYear = Enumerable.Range(0, 5).Select(i => new { I = i, Y = (DateTime.Now.Year - i) + 543 });

            //User datas
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var CurrentEOfficeUser = DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();

            //Filter Data OrgId From User
            if (CurrentEOfficeUser != null)
            {
                Organization = DB.EOfficeOrganizations.Where(w => w.OrgId == CurrentEOfficeUser.OrgId).ToList();
            }

            //Create DropDown
            ViewBag.Organization = new SelectList(Organization, "OrgId", "OrgName");
            ViewBag.ArchivesType = new SelectList(ArchivesType, "TypeCode", "Name");
            ViewBag.Month = new SelectList(Months, "I", "M");
            ViewBag.BugetYear = new SelectList(BugetYear, "Y", "Y");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetArchives(string TypeCode, int Year, int Month, int OrgCode)
        {

            //Helper
            ViewBag.Helper = Helper;

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            //var GetOrgId = await DB.OrgUsers.Include(i=>i.Organizaions).Where(w => w.UserId == CurrentUser.Id && w.Organizaions.OrgType == 2).Select(s => s.Organizaions.OrgCode).FirstOrDefaultAsync();
            //var GetCurrentOrgId = await DB.Organizations.Where(w => w.OrgId == GetOrgId).Select(s => s.OrgCode).FirstOrDefaultAsync();

            // ViewModel
            var ArchivesViewModel = new List<ViewModels.EOffice.EOfficeArchives>();
            var Gets = await DB.EOfficeArchives
                .Where(w => w.OriginalOrgCode == OrgCode
                && w.BudgetYear == Year
                && w.CreateDate.Month == Month
                && w.IsDelete == false
                && w.TypeCode == TypeCode)
                .ToListAsync();

            var Orgs = DB.EOfficeOrganizations;

            foreach (var Get in Gets)
            {
                var Archives = new ViewModels.EOffice.EOfficeArchives();
                // Get Current ArcivesNumber
                var GetArcivesNumber = Orgs.Where(w => w.OrgId == Convert.ToInt32(Get.ArchiveOrgCode)).FirstOrDefault();
                if (GetArcivesNumber != null)
                {
                    Archives.ArchiveNumber = (Get.ArchiveOrgCode == null ? "-" : GetArcivesNumber.OrgShortName + " " + GetArcivesNumber.OrgNumber + "/" + (Get.IsCirculation == true ? "ว" : "") + "" + Get.ArchiveNumber);
                }
                else
                {
                    Archives.ArchiveNumber = "-";
                }



                Archives.ArchiveId = Get.ArchiveId;

                Archives.CreateDate = Helper.getShortDateThai(Get.CreateDate);
                Archives.FromOrg = Orgs.Where(w => w.OrgId == Get.OriginalOrgCode).Select(s => s.OrgShortName).FirstOrDefault();
                Archives.ToOrg = (Get.DestinationOrgCode == "" || Get.DestinationOrgCode == "Array" ? "-" : Orgs.Where(w => w.OrgId == Convert.ToInt32(Get.DestinationOrgCode)).Select(s => s.OrgShortName).FirstOrDefault());
                Archives.Title = Get.Title;
                Archives.StatusCode = Get.StatusCode;
                Archives.BudgetYear = Get.BudgetYear;
                Archives.ArchiveId = Get.ArchiveId;
                Archives.ArchiveOrgCode = Get.ArchiveOrgCode;
                Archives.StatusCode = DB.EOfficeArchivesStatus.Where(w => w.StatusCode == Get.StatusCode).Select(s => s.Name).FirstOrDefault();
                Archives.Record = DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == Get.ArchiveId).Count();
                ArchivesViewModel.Add(Archives);
            }

            return PartialView(ArchivesViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> FormAddArchives(bool IsCirculation, string TypeCode, int OrgCode)
        {
            ViewBag.Helper = Helper;
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetUser = await DB.Users.Where(a => a.Id == CurrentUser.Id).FirstOrDefaultAsync();

            var GetOrgId = await DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefaultAsync();
            var GetCurrentOrgId = await DB.EOfficeOrganizations.Where(w => w.OrgId == GetOrgId).Select(s => s.OrgName).FirstOrDefaultAsync();

            ViewBag.Date = Helper.getShortDate(DateTime.Now);
            ViewBag.FullName = GetUser.FirstName + " " + GetUser.LastName;
            ViewBag.ArchiveType = new SelectList(DB.EOfficeArchivesType.Where(b => b.TypeCode != "01" && b.TypeCode != "02").OrderBy(c => c.Id).ToList(), "TypeCode", "Name");
            ViewBag.AccessLevel = new SelectList(DB.EOfficeArchivesAccesslevel.Where(b => b.Enable == true).OrderBy(c => c.Name).ToList(), "AccessLevel", "Name");
            ViewBag.Expendition = new SelectList(DB.EOfficeArchivesExpedition.OrderByDescending(c => c.Id).ToList(), "Level", "Name");
            ViewBag.Command = new SelectList(DB.EOfficeArchivesCommand.Where(c => c.Enable == true).ToList(), "CmdCode", "Name");
            ViewBag.ArchivesStatus = new SelectList(DB.EOfficeArchivesStatus.Where(d => d.Enable == true).ToList(), "StatusCode", "Name");
            ViewBag.GetCurrentOrgId = GetCurrentOrgId;

            // Destination Org Archive Dropdown
            List<SelectListItem> DestinationOrgCode = new List<SelectListItem>();
            var GetOrgs = await DB.EOfficeOrganizations.Where(w => w.ParentId == 0).OrderBy(r => r.Position).ToListAsync();
            foreach (var Get in GetOrgs)
            {
                DestinationOrgCode.Add(new SelectListItem()
                {
                    Text = "|-" + Get.OrgName,
                    Value = Get.OrgId.ToString(),
                });

                //Level 2
                foreach (var Get2 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get.OrgMomId).OrderBy(r => r.Position))
                {
                    DestinationOrgCode.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + "|-" + Get2.OrgName,
                        Value = Get2.OrgId.ToString()
                    });

                    //Level 3
                    foreach (var Get3 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get2.OrgMomId).OrderBy(r => r.Position))
                    {
                        DestinationOrgCode.Add(new SelectListItem()
                        {
                            Text = SpaceString(20) + "|-" + Get3.OrgName,
                            Value = Get3.OrgId.ToString()
                        });

                        //Level 4
                        foreach (var Get4 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get3.OrgMomId).OrderBy(r => r.Position))
                        {
                            DestinationOrgCode.Add(new SelectListItem()
                            {
                                Text = SpaceString(20) + "|-" + Get4.OrgName,
                                Value = Get4.OrgId.ToString()
                            });

                            //Level 5
                            foreach (var Get5 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get4.OrgMomId))
                            {
                                DestinationOrgCode.Add(new SelectListItem()
                                {
                                    Text = SpaceString(20) + "|-" + Get5.OrgName,
                                    Value = Get5.OrgId.ToString()
                                });
                            }
                        }
                    }
                }
            }
            ViewBag.Org = DestinationOrgCode;

            var GetUserOrg = await DB.EOfficeOrgUsers.Where(a => a.UserId == CurrentUser.Id).FirstOrDefaultAsync();
            var GetCurrentYear = DateTime.Now.Year + 543;

            //Archive Number
            var GetCurrentNumber = DB.EOfficeOrganizations.Where(w => w.OrgId == GetOrgId).FirstOrDefault();
            ViewBag.CurrentNumber = "";
            if (GetCurrentNumber != null)
            {
                ViewBag.CurrentNumber = GetCurrentNumber.OrgShortName + GetCurrentNumber.OrgNumber;
            }

            var CheckedArchiveOrgCode = DB.EOfficeArchives.Where(w => w.OriginalOrgCode == GetOrgId).OrderByDescending(o => o.ArchiveOrgCode).FirstOrDefault();
            ViewBag.ArchivesNumber = 1;
            if (CheckedArchiveOrgCode != null)
            {
                var LastArchive = DB.EOfficeArchives.Where(a => a.ArchiveOrgCode == CheckedArchiveOrgCode.ArchiveOrgCode && a.BudgetYear == (DateTime.Now.Year + 543) && a.ArchiveNumber != 0).OrderByDescending(v => v.ArchiveNumber).LastOrDefault();

                ViewBag.ArchivesNumber = (LastArchive == null ? 1 : (LastArchive.ArchiveNumber + 1));
            }

            return View("FormAddArchives");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddArchive(Models.EOfficeArchives model, EOfficeArchivesRoutingTransaction Transaction, int ArchiveNumber, int IssueArchiveNumber, List<IFormFile> AttachFiles, bool IsSendNow, string ExternalOrgName, string ExternalCmdCode, string[] DestinationOrg, bool IsCirculation, string CmdCode, int IssueArchive, string ArchiveOrgCodes, string Prefix, string filename, int Day, int Month, int Year)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetOrgId = await DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefaultAsync();
            //var GetCurrentOrgId = await DB.EOfficeOrganizations.Where(w => w.OrgId == GetOrgId).Select(s => s.OrgCode).FirstOrDefaultAsync();

            string msg = "";
            try
            {
                int NewNumber = 0;
                model.OriginalOrgCode = GetOrgId;
                Transaction.FromOrgCode = GetOrgId;
                if (DB.EOfficeArchives.Where(a => a.ArchiveNumber == model.ArchiveNumber && a.ArchiveOrgCode == model.ArchiveOrgCode && a.BudgetYear == model.BudgetYear && a.TypeCode == model.TypeCode).Count() > 0)
                {
                    return Json(new { valid = false, message = "มีชื่อหนังสือ หรือเลขที่หนังอยู่แล้ว" });
                }
                else
                {
                    if (IssueArchiveNumber == 0 && IssueArchive == 0)
                    {
                        model.CreateByPCode = CurrentUser.Id;
                        model.CreateDate = DateTime.Now;
                        if (IsSendNow == true)
                        {
                            model.StatusCode = "01";
                        }
                        else
                        {
                            model.StatusCode = " ";
                        }
                        model.IsDelete = false;
                        model.ArchiveOrgCode = 0;
                        model.ArchiveNumber = 0;
                        model.Prefix = "";
                        model.EditDate = null;
                        model.RegisterDate = new DateTime(Year, Month, Day);
                        model.AttachFiles = "";
                        model.ExternalArchiveNumber = "";
                        model.ExternalOrgName = "";

                        if (model.TypeCode == "03")
                        {
                            model.ExternalOrgName = ExternalOrgName;
                            model.ExternalCmdCode = ExternalCmdCode;
                        }
                        if (model.IsCirculation == true)
                        {
                            if (model.AccessLevel != 4)
                            {
                                return Json(new { valid = false, message = "ไม่สามารถสร้างหนังสือแจ้งเวียนที่มีชั้นความลับได้" });
                            }
                            model.DestinationOrgCode = "Array";
                        }

                        DB.EOfficeArchives.Add(model);
                        await DB.SaveChangesAsync();
                        msg = "บันทึกข้อมูลสำเร็จ";

                        //UploadFile
                        string File = " ";
                        if (AttachFiles != null)
                        {
                            foreach (var AttachFile in AttachFiles)
                            {
                                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-office/");
                                if (AttachFile.Length > 0)
                                {
                                    string fileName = ContentDispositionHeaderValue.Parse(AttachFile.ContentDisposition).FileName.Trim('"');
                                    string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + fileName.ToString();

                                    using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                                    {
                                        await AttachFile.CopyToAsync(fileStream);
                                    }

                                    File = UniqueFileName.ToString();
                                }

                                var ArchiveFile = new EOfficeArchivesFile();
                                ArchiveFile.FileName = File;
                                ArchiveFile.ArchiveId = model.ArchiveId;
                                ArchiveFile.FileTitle = AttachFile.FileName;
                                DB.EOfficeArchivesFile.Add(ArchiveFile);
                                await DB.SaveChangesAsync();
                                msg = "บันทึกข้อมูลสำเร็จ";
                            }
                        }
                    }
                    // ติ๊กออกเลขหนังสือ
                    else if (IssueArchiveNumber == 1 || IssueArchive == 1)
                    {
                        model.CreateByPCode = CurrentUser.Id;
                        model.CreateDate = DateTime.Now;
                        if (IsSendNow == true)
                        {
                            model.StatusCode = "01";
                        }
                        else
                        {
                            model.StatusCode = " ";
                        }
                        model.IsDelete = false;
                        model.EditDate = DateTime.Now;
                        model.AttachFiles = "";
                        model.RegisterDate = new DateTime(Year, Month, Day); ////Edit Jean
                        model.ArchiveNumber = ArchiveNumber;
                        if (model.TypeCode == "04")
                        {
                            model.ExternalOrgName = ExternalOrgName;
                            model.ExternalCmdCode = ExternalCmdCode;
                        }
                        //if (model.TypeCode == "03" && model.OriginalOrgCode == 1)
                        if (model.TypeCode == "03" && model.OriginalOrgCode == 1)
                        {
                            model.ArchiveOrgCode = 1;
                            //  model.ArchiveOrgCode = Prefix;
                            // model.ArchiveOrgCode = "6";
                        }
                        else
                        {
                            //model.ArchiveOrgCode = ArchiveOrgCodes;
                            model.Prefix = "";
                        }

                        if (model.IsCirculation == true)
                        {
                            model.DestinationOrgCode = "Array"; //Circulation Archive
                        }

                        if (DB.EOfficeArchives.Where(b => b.ArchiveOrgCode == model.ArchiveOrgCode && b.ArchiveNumber == model.ArchiveNumber && b.BudgetYear == (DateTime.Now.Year + 543) && b.TypeCode == model.TypeCode).Count() > 0)
                        {
                            NewNumber = Convert.ToInt32(model.ArchiveNumber) + 1;
                            model.ArchiveNumber = NewNumber;
                        }

                        DB.EOfficeArchives.Add(model);
                        await DB.SaveChangesAsync();
                        msg = "บันทึกข้อมูลสำเร็จ";

                        //UploadFile
                        string File = " ";
                        if (AttachFiles != null)
                        {
                            foreach (var AttachFile in AttachFiles)
                            {
                                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-office/");
                                if (AttachFile.Length > 0)
                                {
                                    string fileName = ContentDispositionHeaderValue.Parse(AttachFile.ContentDisposition).FileName.Trim('"');
                                    string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + fileName.ToString();

                                    using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                                    {
                                        await AttachFile.CopyToAsync(fileStream);
                                    }

                                    File = UniqueFileName.ToString();
                                }

                                var ArchiveFile = new EOfficeArchivesFile();
                                ArchiveFile.FileName = File;
                                ArchiveFile.ArchiveId = model.ArchiveId;
                                ArchiveFile.FileTitle = AttachFile.FileName;

                                DB.EOfficeArchivesFile.Add(ArchiveFile);
                                await DB.SaveChangesAsync();
                                msg = "บันทึกข้อมูลสำเร็จ";
                            }
                        }
                    }
                    if (IsSendNow == true)
                    {
                        var Archives = await DB.EOfficeArchives.Where(a => a.ArchiveId == model.ArchiveId).FirstOrDefaultAsync();
                        if (IsCirculation == true)
                        {
                            foreach (var getCount in DestinationOrg)
                            {
                                var transac = new EOfficeArchivesRoutingTransaction();
                                transac.ArchiveId = model.ArchiveId;
                                transac.FromOrgCode = model.OriginalOrgCode;
                                transac.ToOrgCode = (IsSendNow == true ? Convert.ToInt32(getCount) : 0);
                                transac.SendByPCode = CurrentUser.Id;
                                transac.SendDate = DateTime.Now;
                                transac.BudgetYear = DateTime.Now.Year + 543;
                                transac.CmdCode = CmdCode;
                                transac.Comment = model.Message;
                                if (model.TypeCode == "04")
                                {
                                    transac.IsExternal = true;
                                }
                                else
                                {
                                    transac.IsExternal = false;
                                }

                                transac.StatusCode = (IsSendNow == true ? "01" : null);

                                DB.EOfficeArchivesRoutingTransaction.Add(transac);
                                await DB.SaveChangesAsync();
                                msg = "บันทึกข้อมูลสำเร็จ";
                            }
                        }
                        else
                        {
                            Transaction.ArchiveId = Archives.ArchiveId;
                            Transaction.FromOrgCode = Archives.OriginalOrgCode;
                            Transaction.ToOrgCode = Convert.ToInt32(Archives.DestinationOrgCode);
                            Transaction.SendByPCode = CurrentUser.Id;
                            Transaction.SendDate = DateTime.Now;
                            Transaction.BudgetYear = DateTime.Now.Year + 543;
                            Transaction.ReceiveNumber = 0;
                            if (model.TypeCode == "04")
                            {
                                Transaction.IsExternal = true;
                            }
                            else
                            {
                                Transaction.IsExternal = false;
                            }
                            Transaction.StatusCode = (IsSendNow == true ? "01" : null);

                            DB.EOfficeArchivesRoutingTransaction.Add(Transaction);
                            await DB.SaveChangesAsync();
                            msg = "บันทึกข้อมูลสำเร็จ";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                msg = "Error is :" + e.InnerException;
                return Json(new { valid = false, message = msg });
            }

            return Json(new { valid = true, message = msg });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteArchives(int Id)
        {
            string Msg = "";
            try
            {
                // Other Abount User
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                //Delete File in
                var Archives = await DB.EOfficeArchivesFile.Where(r => r.ArchiveId == Id).ToListAsync();

                if (Archives.Count() > 0)
                {
                    foreach (var ArchiveOLD in Archives)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-office/");
                        if (ArchiveOLD != null)
                        {
                            string oldfilepath = Path.Combine(Uploads, ArchiveOLD.FileName);
                            // ลบ File เดิม
                            if (System.IO.File.Exists(oldfilepath))
                            {
                                System.IO.File.Delete(oldfilepath);
                            }
                        }

                        DB.RemoveRange(ArchiveOLD);
                        await DB.SaveChangesAsync();
                    }
                }

                //var uploads = Path.Combine(_environment.WebRootPath, "uploads/archives/");
                //string oldPictureFile = await DB.Archives.Where(b => b.ID == Id).Select(b => b.AttachFiles).FirstOrDefaultAsync();

                //if (oldPictureFile != null)
                //{
                //    string oldfilepath = Path.Combine(uploads, oldPictureFile);
                //    // ลบ File เดิม
                //    if (System.IO.File.Exists(oldfilepath))
                //    {
                //        System.IO.File.Delete(oldfilepath);
                //    }
                //}

                // Check Status 
                var CheckArchivesStatus = await DB.EOfficeArchives.Where(a => a.ArchiveId == Id).Select(s => s.StatusCode).FirstOrDefaultAsync();
                if (CheckArchivesStatus == "02" || CheckArchivesStatus == "03" || CheckArchivesStatus == "04" || CheckArchivesStatus == "05" || CheckArchivesStatus == "06" || CheckArchivesStatus == "07" || CheckArchivesStatus == "10")
                {
                    return Json(new { valid = false, message = "ไม่สามารถลบรายการนี้ได้" });
                }

                var ArchivesModel = await DB.EOfficeArchives.Where(a => a.ArchiveId == Id).FirstOrDefaultAsync();
                ArchivesModel.IsDelete = true;
                ArchivesModel.DeleteByPcode = CurrentUser.Id;
                ArchivesModel.DeleteDate = DateTime.Now;
                DB.EOfficeArchives.Update(ArchivesModel);
                await DB.SaveChangesAsync();
                Msg = "ลบข้อมูลสำเร็จ";
            }
            catch (Exception e)
            {
                Msg = "Error is : " + e.Message;
                return Json(new { valid = false, message = Msg });
            }
            return Json(new { valid = true, message = Msg });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FormEditArchives(int ArchivesId, int OrgCode)
        {

            ViewBag.Helper = Helper;
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetUser = await DB.Users.Where(a => a.Id == CurrentUser.Id).FirstOrDefaultAsync();
            var Archives = await DB.EOfficeArchives.Where(a => a.ArchiveId == ArchivesId).FirstOrDefaultAsync();
            var GetArchivesNumber = await DB.EOfficeOrganizations.Where(w => w.OrgId == Convert.ToInt32(Archives.ArchiveOrgCode)).Select(s => s.OrgNumber).FirstOrDefaultAsync();
            ViewBag.Date = Helper.getShortDate(DateTime.Now);

            ViewBag.OldNumber = Archives.ArchiveNumber;
            ViewBag.NumberArchive = GetArchivesNumber + "/" + Archives.ArchiveNumber;

            ViewBag.AccessLevel = new SelectList(DB.EOfficeArchivesAccesslevel.Where(b => b.Enable == true).OrderByDescending(c => c.Id).ToList(), "AccessLevel", "Name",Archives.AccessLevel);
            ViewBag.FullName = GetUser.FirstName + " " + GetUser.LastName;
            ViewBag.Expendition = new SelectList(DB.EOfficeArchivesExpedition.OrderByDescending(c => c.Id).ToList(), "Level", "Name",Archives.Expedition);
            ViewBag.Command = new SelectList(DB.EOfficeArchivesCommand.Where(c => c.Enable == true).ToList(), "CmdCode", "Name");
            ViewBag.ArchivesStatus = new SelectList(DB.EOfficeArchivesStatus.Where(d => d.Flag1 == 2).ToList(), "Id", "Name");
            ViewBag.TypeCode = new SelectList(DB.EOfficeArchivesType.Where(a => a.TypeCode != "01" && a.TypeCode != "02").ToList(), "TypeCode", "Name", Archives.TypeCode);
            ViewBag.StatusCode = Archives.StatusCode;
            var Organization = DB.EOfficeOrganizations.ToList();

            ViewBag.CommentTransaction = DB.EOfficeArchivesRoutingTransaction.Where(a => a.ArchiveId == Archives.ArchiveId).Select(b => b.Comment).FirstOrDefault();

            // Destination Org Archive Dropdown
            List<SelectListItem> DestinationOrgCode = new List<SelectListItem>();
            var GetOrgs = await DB.EOfficeOrganizations.Where(w => w.ParentId == 0).OrderBy(r => r.Position).ToListAsync();
            foreach (var Get in GetOrgs)
            {
                DestinationOrgCode.Add(new SelectListItem()
                {
                    Text = "|-" + Get.OrgName,
                    Value = Get.OrgId.ToString(),
                });

                //Level 2
                foreach (var Get2 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get.OrgMomId).OrderBy(r => r.Position))
                {
                    DestinationOrgCode.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + "|-" + Get2.OrgName,
                        Value = Get2.OrgId.ToString()
                    });

                    //Level 3
                    foreach (var Get3 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get2.OrgMomId).OrderBy(r => r.Position))
                    {
                        DestinationOrgCode.Add(new SelectListItem()
                        {
                            Text = SpaceString(20) + "|-" + Get3.OrgName,
                            Value = Get3.OrgId.ToString()
                        });

                        //Level 4
                        foreach (var Get4 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get3.OrgMomId).OrderBy(r => r.Position))
                        {
                            DestinationOrgCode.Add(new SelectListItem()
                            {
                                Text = SpaceString(20) + "|-" + Get4.OrgName,
                                Value = Get4.OrgId.ToString()
                            });

                            //Level 5
                            foreach (var Get5 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get4.OrgMomId))
                            {
                                DestinationOrgCode.Add(new SelectListItem()
                                {
                                    Text = SpaceString(20) + "|-" + Get5.OrgName,
                                    Value = Get5.OrgId.ToString()
                                });
                            }
                        }
                    }
                }
            }
            ViewBag.Org = DestinationOrgCode;

            var ForwardViewModel = new List<ForwardViewModel>();
            var Gets = DB.EOfficeArchivesRoutingTransaction.Where(a => a.ArchiveId == ArchivesId).Select(b => b.ToOrgCode).ToList();
            foreach (var Get in Gets)
            {
                var Data = new ForwardViewModel();
                var GetOrg = await DB.EOfficeOrganizations.Where(a => a.OrgId == Convert.ToInt32(Get)).Select(b => b.OrgName).FirstOrDefaultAsync();
                Data.OrgName = GetOrg;
                ForwardViewModel.Add(Data);
            }
            ViewBag.OrgName = ForwardViewModel;
            var GetUserOrg = await DB.EOfficeOrgUsers.Where(a => a.UserId == CurrentUser.Id).FirstOrDefaultAsync();

            //var CurrentNumber = await DB.Organizations.Where(c => c.OrgId == GetUserOrg.OrgId).FirstOrDefaultAsync();
            var CurrentNumber = await DB.EOfficeOrganizations.Where(c => c.OrgId == OrgCode).FirstOrDefaultAsync();
            if (Archives.Prefix == null)
            {
                ViewBag.CurrentNumberOfcenter = await DB.EOfficeOrganizations.Where(a => a.OrgId == Convert.ToInt32(Archives.ArchiveOrgCode) && Archives.ArchiveId == ArchivesId).Select(a => a.OrgNumber).FirstOrDefaultAsync();
            }
            else
            {
                ViewBag.CurrentNumberOfcenter = await DB.EOfficeOrganizations.Where(a => a.OrgId == Convert.ToInt32(Archives.Prefix) && Archives.ArchiveId == ArchivesId).Select(a => a.OrgNumber).FirstOrDefaultAsync();
            }

            ViewBag.CurrentNumber = CurrentNumber.OrgNumber;
            ViewBag.OriginalOrgCode = CurrentNumber.OrgId;

            ViewBag.OrgUserId = GetUserOrg.OrgId;
            var GetCurrentYear = DateTime.Now.Year + 543;
            //เลขรัน
            //var LastArchiveNumber = await DB.Archives.Where(e =>Convert.ToInt32(e.ArchiveOrgCode) == CurrentNumber.OrgCode && e.BudgetYear == GetCurrentYear && e.ArchiveNumber != null).Select(s => int.Parse(s.ArchiveNumber)).MaxAsync();
            var LastArchiveNumber = await DB.EOfficeArchives.Where(e => Convert.ToInt32(e.ArchiveOrgCode) == CurrentNumber.OrgId && e.BudgetYear == GetCurrentYear && e.ArchiveNumber != null).Select(s => Convert.ToInt32(s.ArchiveNumber)).MaxAsync();

            if (LastArchiveNumber == 0)
            {
                ViewBag.LastArchiveNumber = 1;
            }
            else
            {
                ViewBag.LastArchiveNumber = LastArchiveNumber + 1;
            }

            ViewBag.GetArchiveNum = await DB.EOfficeArchives.Where(a => a.ArchiveId == ArchivesId && a.BudgetYear == DateTime.Now.Year + 543).Select(v => v.ArchiveNumber).FirstOrDefaultAsync();

            //Archive Number
            var GetCurrentNumber = DB.EOfficeOrganizations.Where(w => w.OrgId == OrgCode).FirstOrDefault();
            ViewBag.CurrentNumber = "";
            if (GetCurrentNumber != null)
            {
                ViewBag.CurrentNumber = GetCurrentNumber.OrgShortName + GetCurrentNumber.OrgNumber;
            }

            return View(Archives);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditArchives(Models.EOfficeArchives model, int IssueArchiveNumber, int IssueArchiveNumberOld, int EditArchiveNumber, int IssueArchive, List<IFormFile> AttachFiles, bool IsSendNow, string ExternalOrgName, string ExternalCmdCode, string[] DestinationOrg, string OldDestination, bool IsCirculation, string CmdCodes, string TypeCode, Int64 ArchiveOrgCodeOld, string ArchiveOrgCodes, string Comment, int Day, int Month, int Year)
        {
            string msg = " ";
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                var GetOrgId = await DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefaultAsync();
                var GetCurrentOrgId = await DB.EOfficeOrganizations.Where(w => w.OrgId == GetOrgId).Select(s => s.OrgId).FirstOrDefaultAsync();
                model.RegisterDate = Convert.ToDateTime(model.RegisterDate.Year + "-" + model.RegisterDate.Month + "-" + model.RegisterDate.Day);

                if (IsSendNow == true)
                {
                    if (model.StatusCode == null)
                    {
                        model.StatusCode = "01";
                    }
                }
                else
                {
                    model.StatusCode = null;
                }

                if (model.TypeCode == "03")
                {
                    model.ExternalOrgName = ExternalOrgName;
                    model.ExternalCmdCode = ExternalCmdCode;
                }
                else
                {
                    model.ExternalOrgName = null;
                    model.ExternalCmdCode = null;
                }
                if (model.TypeCode == "03" && model.OriginalOrgCode == 1 && EditArchiveNumber == 1)
                {
                    model.ArchiveOrgCode = 1;

                }
                else
                {
                    model.Prefix = null;
                }

                if (model.IsCirculation == true)
                {
                    if (model.AccessLevel != 4)
                    {
                        return Json(new { valid = false, message = "ไม่สามารถสร้างหนังสือแจ้งเวียนที่มีชั้นความลับได้" });
                    }
                    model.DestinationOrgCode = "Array";
                }

                if (TypeCode != "03")
                {
                    if (IssueArchiveNumber == 0 && IssueArchive == 0 && EditArchiveNumber == 0 && IssueArchiveNumberOld == 0)
                    {
                        model.ArchiveNumber = 0;
                        model.ArchiveOrgCode = 0;
                    }
                    else if (IssueArchiveNumberOld == 1 && EditArchiveNumber == 1)
                    {
                        // model.ArchiveOrgCode = ArchiveOrgCodeOld;
                    }
                    else if (IssueArchiveNumberOld == 1)
                    {
                        model.ArchiveOrgCode = ArchiveOrgCodeOld;
                    }
                    else if (IssueArchiveNumber == 1)
                    {

                    }

                }
                else
                {
                    if (IssueArchiveNumber == 0 && IssueArchive == 0 && EditArchiveNumber == 0 && IssueArchiveNumberOld == 0)
                    {
                        model.ArchiveNumber = 0;
                        model.ArchiveOrgCode = 0;
                    }
                    else if (IssueArchiveNumberOld == 1 && EditArchiveNumber == 1)
                    {
                        // model.ArchiveOrgCode = ArchiveOrgCodeOld;
                    }
                    else if (IssueArchiveNumberOld == 1)
                    {
                        model.ArchiveOrgCode = ArchiveOrgCodeOld;
                    }
                    else if (IssueArchiveNumber == 1)
                    {

                    }
                    else if (DB.EOfficeArchives.Where(b => b.ArchiveOrgCode == model.ArchiveOrgCode && b.ArchiveNumber == model.ArchiveNumber && b.BudgetYear == (DateTime.Now.Year + 543) && b.TypeCode == model.TypeCode).Count() > 0)
                    {
                        int NewNumber = 0;
                        NewNumber = Convert.ToInt32(model.ArchiveNumber) + 1;
                        model.ArchiveNumber = NewNumber;
                    }
                }

                model.EditDate = DateTime.Now;
                model.EditByPCode = CurrentUser.Id;
                if (model.DestinationOrgCode == null)
                {
                    model.DestinationOrgCode = OldDestination;
                }

                model.RegisterDate = new DateTime(Year, Month, Day);
                DB.EOfficeArchives.Attach(model);
                DB.EOfficeArchives.Update(model);
                await DB.SaveChangesAsync();
                msg = "แก้ไขข้อมูลสำเร็จ";

                // //UploadFile
                var ArchivesId = await DB.EOfficeArchives.Where(r => r.ArchiveId == model.ArchiveId).Select(s => s.ArchiveId).FirstOrDefaultAsync();
                var CountArchiveFileID = await DB.EOfficeArchivesFile.Where(s => s.ArchiveId == ArchivesId).ToListAsync();

                if (CountArchiveFileID.Count() > 0 && AttachFiles.Count() > 0)
                {
                    foreach (var ArchiveOLD in CountArchiveFileID)
                    {
                        var uploads = Path.Combine(_environment.WebRootPath, "uploads/archives/");
                        if (ArchiveOLD != null)
                        {
                            string oldfilepath = Path.Combine(uploads, ArchiveOLD.FileName);
                            // ลบ File เดิม
                            if (System.IO.File.Exists(oldfilepath))
                            {
                                System.IO.File.Delete(oldfilepath);
                            }
                        }

                        DB.RemoveRange(ArchiveOLD);
                        await DB.SaveChangesAsync();
                    }
                }


                string File = " ";
                if (AttachFiles != null || AttachFiles.Count() > 0)
                {
                    foreach (var AttachFile in AttachFiles)
                    {

                        var uploads = Path.Combine(_environment.WebRootPath, "uploads/archives/");
                        if (AttachFile.Length > 0)
                        {
                            string fileName = ContentDispositionHeaderValue.Parse(AttachFile.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + fileName.ToString();

                            using (var fileStream = new FileStream(Path.Combine(uploads, UniqueFileName), FileMode.Create))
                            {
                                await AttachFile.CopyToAsync(fileStream);
                            }

                            File = UniqueFileName.ToString();
                        }

                        var ArchiveFile = new EOfficeArchivesFile();
                        ArchiveFile.FileName = File;
                        ArchiveFile.ArchiveId = model.ArchiveId;
                        ArchiveFile.FileTitle = AttachFile.FileName;
                        DB.EOfficeArchivesFile.Add(ArchiveFile);
                        await DB.SaveChangesAsync();
                        msg = "บันทึกข้อมูลสำเร็จ";
                    }
                }


                var Archives = await DB.EOfficeArchives.Where(a => a.ArchiveId == model.ArchiveId).FirstOrDefaultAsync();
                var ArchivesRoutingTransaction = DB.EOfficeArchivesRoutingTransaction.Where(a => a.ArchiveId == model.ArchiveId).Select(a => a.TransactionId).Count();
                if (IsSendNow == true && ArchivesRoutingTransaction == 0)
                {
                    if (IsCirculation == true)
                    {
                        foreach (var getCount in DestinationOrg)
                        {
                            var transac = new EOfficeArchivesRoutingTransaction();
                            transac.ArchiveId = model.ArchiveId;
                            transac.FromOrgCode = model.OriginalOrgCode;
                            transac.ToOrgCode = (IsSendNow == true ? Convert.ToInt32(getCount) : 0);
                            transac.SendByPCode = CurrentUser.Id;
                            transac.SendDate = DateTime.Now;
                            transac.BudgetYear = DateTime.Now.Year + 543;
                            transac.CmdCode = CmdCodes;
                            transac.Comment = Comment;
                            if (model.TypeCode == "03")
                            {
                                transac.IsExternal = true;
                            }
                            else
                            {
                                transac.IsExternal = false;
                            }

                            if (IsSendNow == true)
                            {
                                if (model.StatusCode == null)
                                {
                                    transac.StatusCode = "01";
                                }
                                else
                                {
                                    transac.StatusCode = model.StatusCode;
                                }
                            }

                            //transac.StatusCode = (IsSendNow == true ? "01" : null);

                            DB.EOfficeArchivesRoutingTransaction.Add(transac);
                            await DB.SaveChangesAsync();
                            msg = "แก้ไขข้อมูลสำเร็จ";
                        }
                    }
                    else
                    {
                        var Transaction = new EOfficeArchivesRoutingTransaction();
                        Transaction.ArchiveId = Archives.ArchiveId;
                        Transaction.FromOrgCode = Archives.OriginalOrgCode;
                        Transaction.ToOrgCode = Convert.ToInt32(Archives.DestinationOrgCode);
                        Transaction.SendByPCode = CurrentUser.Id;
                        Transaction.SendDate = DateTime.Now;
                        Transaction.BudgetYear = DateTime.Now.Year + 543;
                        Transaction.CmdCode = CmdCodes;
                        Transaction.Comment = Comment;
                        if (model.TypeCode == "04")
                        {
                            Transaction.IsExternal = true;
                        }
                        else
                        {
                            Transaction.IsExternal = false;
                        }

                        if (IsSendNow == true)
                        {
                            if (model.StatusCode == null)
                            {
                                Transaction.StatusCode = "01";
                            }
                            else
                            {
                                Transaction.StatusCode = model.StatusCode;
                            }
                        }

                        //Transaction.StatusCode = (IsSendNow == true ? "01" : null);

                        DB.EOfficeArchivesRoutingTransaction.Add(Transaction);
                        await DB.SaveChangesAsync();
                        msg = "แก้ไขข้อมูลสำเร็จ";
                    }
                }
                else if (IsSendNow == true && ArchivesRoutingTransaction >= 1)
                {
                    if (IsCirculation == true)
                    {

                        if (DestinationOrg.Count() > 0)
                        {
                            var ArchivesRouting = DB.EOfficeArchivesRoutingTransaction.Where(a => a.ArchiveId == model.ArchiveId).ToList();
                            DB.EOfficeArchivesRoutingTransaction.RemoveRange(ArchivesRouting);
                            await DB.SaveChangesAsync();

                            foreach (var getCount in DestinationOrg)
                            {
                                var transacArchive = new EOfficeArchivesRoutingTransaction();
                                transacArchive.ArchiveId = model.ArchiveId;
                                transacArchive.FromOrgCode = model.OriginalOrgCode;
                                transacArchive.ToOrgCode = (IsSendNow == true ? Convert.ToInt32(getCount) : 0);
                                transacArchive.SendByPCode = CurrentUser.Id;
                                transacArchive.SendDate = DateTime.Now;
                                transacArchive.BudgetYear = DateTime.Now.Year + 543;
                                transacArchive.CmdCode = CmdCodes;
                                transacArchive.Comment = Comment;
                                if (model.TypeCode == "03")
                                {
                                    transacArchive.IsExternal = true;
                                }
                                else
                                {
                                    transacArchive.IsExternal = false;
                                }

                                transacArchive.StatusCode = (IsSendNow == true ? "01" : null);


                                DB.EOfficeArchivesRoutingTransaction.Add(transacArchive);
                                await DB.SaveChangesAsync();
                                msg = "แก้ไขข้อมูลสำเร็จ";
                            }
                        }
                    }
                    else
                    {

                        if (model.DestinationOrgCode != OldDestination)
                        {
                            var ArchivesRouting = DB.EOfficeArchivesRoutingTransaction.Where(a => a.ArchiveId == model.ArchiveId).ToList();
                            DB.EOfficeArchivesRoutingTransaction.RemoveRange(ArchivesRouting);
                            await DB.SaveChangesAsync();

                            var transacArchive = new EOfficeArchivesRoutingTransaction();
                            transacArchive.ArchiveId = Archives.ArchiveId;
                            transacArchive.FromOrgCode = Archives.OriginalOrgCode;
                            transacArchive.ToOrgCode = Convert.ToInt32(Archives.DestinationOrgCode);
                            transacArchive.SendByPCode = CurrentUser.Id;
                            transacArchive.SendDate = DateTime.Now;
                            transacArchive.BudgetYear = DateTime.Now.Year + 543;
                            transacArchive.CmdCode = CmdCodes;
                            transacArchive.Comment = Comment;
                            if (model.TypeCode == "04")
                            {
                                transacArchive.IsExternal = true;
                            }
                            else
                            {
                                transacArchive.IsExternal = false;
                            }

                            if (IsSendNow == true)
                            {
                                if (model.StatusCode == null)
                                {
                                    transacArchive.StatusCode = "01";
                                }
                                else
                                {
                                    transacArchive.StatusCode = model.StatusCode;
                                }
                            }

                            //transacArchive.StatusCode = (IsSendNow == true ? "01" : null);

                            DB.EOfficeArchivesRoutingTransaction.Add(transacArchive);
                            await DB.SaveChangesAsync();
                            msg = "แก้ไขข้อมูลสำเร็จ";
                        }
                        else
                        {
                            var TransectionId = DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == Archives.ArchiveId).Select(s => s.TransactionId).FirstOrDefault();
                            var Transaction = DB.EOfficeArchivesRoutingTransaction.Where(s => s.TransactionId == TransectionId).FirstOrDefault();

                            Transaction.FromOrgCode = Archives.OriginalOrgCode;
                            Transaction.ToOrgCode = Convert.ToInt32(Archives.DestinationOrgCode);
                            Transaction.SendByPCode = CurrentUser.Id;
                            Transaction.SendDate = new DateTime(Year, Month, Day);
                            Transaction.BudgetYear = DateTime.Now.Year + 543;
                            Transaction.CmdCode = CmdCodes;
                            Transaction.Comment = Comment;
                            if (model.TypeCode == "04")
                            {
                                Transaction.IsExternal = true;
                            }
                            else
                            {
                                Transaction.IsExternal = false;
                            }

                            if (IsSendNow == true)
                            {
                                if (model.StatusCode == null)
                                {
                                    Transaction.StatusCode = "01";
                                }
                                else
                                {
                                    Transaction.StatusCode = model.StatusCode;
                                }
                            }

                            DB.EOfficeArchivesRoutingTransaction.Attach(Transaction);
                            DB.EOfficeArchivesRoutingTransaction.Update(Transaction);
                            await DB.SaveChangesAsync();
                            msg = "แก้ไขข้อมูลสำเร็จ";
                        }

                    }
                }
            }
            catch (Exception e)
            {
                msg = "Error is :" + e.InnerException;
                return Json(new { valid = false, message = msg });

            }
            return Json(new { valid = true, message = msg });
        }


        #endregion

        #region  Receive ทะเบียนรับ

        [HttpGet]
        public async Task<IActionResult> ReceiveIndex()
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetOrgs = await DB.EOfficeOrganizations.ToListAsync();

            var OrgUsers = await DB.EOfficeOrgUsers.Where(b => b.UserId == CurrentUser.Id).ToListAsync();
            List<SelectListItem> Orgs = new List<SelectListItem>();
            foreach (var OrgUser in OrgUsers)
            {
                Orgs.Add(new SelectListItem
                {
                    Text = GetOrgs.Where(w => w.OrgId == OrgUser.OrgId).Select(s => s.OrgName).FirstOrDefault(),
                    Value = OrgUser.OrgId.ToString()
                });
            }

            ViewBag.OrgFilters = Orgs;
            ViewBag.ArchivesType = new SelectList(DB.EOfficeArchivesType.Where(w => w.TypeCode == "03" || w.TypeCode == "04").ToList(), "TypeCode", "Name");

            return View("ReceiveIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetReceives(Int64 OrgId, string Type, int Year, int Month)
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetOrganizations = await DB.EOfficeOrganizations.ToListAsync();
            var GetStatus = await DB.EOfficeArchivesStatus.ToListAsync();

            var OrgPrefix = GetOrganizations.Where(w => w.OrgId == OrgId).Select(s => s.OrgShortName + " " + s.OrgNumber + "/").FirstOrDefault();

            var Models = new List<ReceivesViewModel>();
            var GetArchives = await DB.EOfficeArchives.Include(f => f.EOfficeArchivesRoutingTransaction)
                .Where(a => (a.TypeCode == Type) && (a.CreateDate.Month == Month && a.BudgetYear == Year) && a.IsDelete == false && a.EOfficeArchivesRoutingTransaction.Any(b => b.ToOrgCode == OrgId)).ToListAsync();

            foreach (var Get in GetArchives)
            {
                var Model = new ReceivesViewModel();
                Model.ReceiveNumber = Convert.ToInt32(Get.EOfficeArchivesRoutingTransaction.Where(w => w.ToOrgCode == OrgId).Select(s => s.ReceiveNumber).LastOrDefault());
                Model.Title = Get.Title;
                Model.ArchiveId = Get.ArchiveId;
                Model.ArchiveNumber = (Get.ArchiveNumber == 0 ? "-" : OrgPrefix + Get.ArchiveNumber);
                Model.CreateDate = Helper.getShortDateThai(Get.CreateDate);
                Model.FromOrg = GetOrganizations.Where(w => w.OrgId == Get.EOfficeArchivesRoutingTransaction.Where(w => w.ToOrgCode == OrgId).Select(s => s.FromOrgCode).FirstOrDefault()).Select(s => s.OrgShortName).FirstOrDefault();
                Model.ToOrg = GetOrganizations.Where(w => w.OrgId == Get.EOfficeArchivesRoutingTransaction.Where(w => w.ToOrgCode == OrgId).Select(s => s.ToOrgCode).FirstOrDefault()).Select(s => s.OrgShortName).FirstOrDefault();
                Model.StatusCode = Get.EOfficeArchivesRoutingTransaction.Where(w => w.ToOrgCode == OrgId).Select(s => s.StatusCode).LastOrDefault();
                Model.Status = GetStatus.Where(w => w.StatusCode == Get.EOfficeArchivesRoutingTransaction.Where(w => w.ToOrgCode == OrgId).Select(s => s.StatusCode).LastOrDefault()).Select(s => s.Name).FirstOrDefault();
                Models.Add(Model);
            }

            return PartialView("GetReceives", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocuments(Int64 ArchiveId)
        {
            // master data
            var GetUsers = await DB.Users.ToListAsync();
            var GetTypeCode = await DB.EOfficeArchivesType.ToListAsync();
            var GetAssasLevel = await DB.EOfficeArchivesAccesslevel.ToListAsync();
            var GetExpedotion = await DB.EOfficeArchivesExpedition.ToListAsync();
            var GetStatus = await DB.EOfficeArchivesStatus.ToListAsync();
            var GetOrg = await DB.EOfficeOrganizations.ToListAsync();


            // detail document
            var GetArchives = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).FirstOrDefaultAsync();

            var Details = new ArchiveViewDetailViewModel();
            Details.ArchiveNumber = (GetArchives.ArchiveNumber == 0 ? "" : GetOrg.Where(w => w.OrgId == GetArchives.ArchiveOrgCode).Select(s => s.OrgShortName + "" + s.OrgNumber + "/").FirstOrDefault() + GetArchives.ArchiveNumber);
            Details.CreateBy = GetUsers.Where(w => w.Id == GetArchives.CreateByPCode).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
            Details.CreateDate = Helper.getDateThai(GetArchives.CreateDate);
            Details.DateOfdoc = Helper.getDateThai(GetArchives.DateOfDoc);
            Details.ExternalArchiveNumber = GetArchives.ExternalArchiveNumber;
            Details.TypeCode = GetArchives.TypeCode;
            Details.TypeName = GetTypeCode.Where(w => w.TypeCode == GetArchives.TypeCode).Select(s => s.Name).FirstOrDefault();
            Details.AssesLevelName = GetAssasLevel.Where(w => w.AccessLevel == GetArchives.AccessLevel).Select(s => s.Name).FirstOrDefault();
            Details.ExpedotionName = GetExpedotion.Where(w => w.Level == Convert.ToInt32(GetArchives.Expedition)).Select(s => s.Name).FirstOrDefault();
            Details.DocumentYear = GetArchives.BudgetYear;
            Details.Dear = GetArchives.Dear;
            Details.Title = GetArchives.Title;
            Details.Message = GetArchives.Message;
            ViewBag.Details = Details;

            // transaction 
            var OriginalOrgCode = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).Select(s => s.OriginalOrgCode).FirstOrDefaultAsync();
            ViewBag.StartOrgShortName = GetOrg.Where(w => w.OrgId == OriginalOrgCode).Select(s => s.OrgShortName).FirstOrDefault();
            ViewBag.StartOrgtName = GetOrg.Where(w => w.OrgId == OriginalOrgCode).Select(s => s.OrgName).FirstOrDefault();
            var TrasactionModels = new List<Transaction>();
            var GetTransactions = await DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId).ToListAsync();
            foreach (var GetTransaction in GetTransactions)
            {
                var TrasactionModel = new Transaction();
                TrasactionModel.Status = GetStatus.Where(w => w.StatusCode == GetTransaction.StatusCode).Select(s => s.Name).FirstOrDefault();
                TrasactionModel.StatusId = GetTransaction.StatusCode;
                TrasactionModel.OrgShortName = GetOrg.Where(w => w.OrgId == GetTransaction.ToOrgCode).Select(s => s.OrgShortName).FirstOrDefault();
                TrasactionModel.OrgtName = GetOrg.Where(w => w.OrgId == GetTransaction.ToOrgCode).Select(s => s.OrgName).FirstOrDefault();
                TrasactionModels.Add(TrasactionModel);
            }
            ViewBag.TrasactionModels = TrasactionModels;

            // transaction detail
            var TransactionDetailModels = new List<TransactionDetails>();
            foreach (var Detail in GetTransactions)
            {
                var TransactionDetailModel = new TransactionDetails();
                TransactionDetailModel.Comment = Detail.Comment;
                TransactionDetailModel.ReceiveDate = Helper.getShortDateAndTime(Convert.ToDateTime(Detail.ReceiveDate));
                TransactionDetailModel.ReceiveNumber = Convert.ToInt32(Detail.ReceiveNumber);
                TransactionDetailModel.ReceiveUserName = GetUsers.Where(w => w.Id == Detail.ReceiveByPCode).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                TransactionDetailModel.SendDate = Helper.getShortDateAndTime(Convert.ToDateTime(Detail.SendDate));
                TransactionDetailModel.StatusCode = Detail.StatusCode;
                TransactionDetailModel.StatusName = GetStatus.Where(w => w.StatusCode == Detail.StatusCode).Select(s => s.Name).FirstOrDefault();
                TransactionDetailModel.ToOrg = GetOrg.Where(w => w.OrgId == Detail.ToOrgCode).Select(s => s.OrgName).FirstOrDefault();
                TransactionDetailModels.Add(TransactionDetailModel);
            }
            ViewBag.TransactionDetailModels = TransactionDetailModels;

            // archive files
            var FileModels = new List<ArchiveFiles>();
            var GetFiles = await DB.EOfficeArchivesFile.Where(w => w.ArchiveId == ArchiveId).ToListAsync();
            foreach (var GetFile in GetFiles)
            {
                var FileModel = new ArchiveFiles();
                FileModel.FileName = GetFile.FileName;
                FileModel.Id = GetFile.FileID;
                FileModel.TitleFileName = GetFile.FileTitle;
                FileModels.Add(FileModel);
            }

            ViewBag.FileModels = FileModels;

            return PartialView("ViewDocuments");
        }

        [HttpGet]
        public async Task<IActionResult> ForworkDocument(Int64 ArchiveId, Int64 OrgId)
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var MyOrgCode = await DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefaultAsync();

            // master data
            var GetTypeCode = await DB.EOfficeArchivesType.ToListAsync();
            var GetAssasLevel = await DB.EOfficeArchivesAccesslevel.ToListAsync();
            var GetExpedotion = await DB.EOfficeArchivesExpedition.ToListAsync();
            var GetStatus = await DB.EOfficeArchivesStatus.ToListAsync();
            var GetOrg = await DB.EOfficeOrganizations.ToListAsync();


            // receive number
            var LastReceive = DB.EOfficeArchivesRoutingTransaction.Where(w => w.ToOrgCode == OrgId && w.BudgetYear == (DateTime.Now.Year + 543)).OrderByDescending(d => d.ReceiveNumber).FirstOrDefault();
            ViewBag.LastReceiveNumber = (LastReceive == null ? 1 : (LastReceive.ReceiveNumber + 1));

            // number of book
            var LastArchive = DB.EOfficeArchives.Where(a => a.ArchiveOrgCode == OrgId && a.BudgetYear == (DateTime.Now.Year + 543) && a.ArchiveNumber != 0).OrderByDescending(v => v.ArchiveNumber).FirstOrDefault();
            ViewBag.LastArchiveNumber = (LastArchive == null ? 1 : (LastArchive.ArchiveNumber + 1));

            // get last status
            var GetLastStatus = DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId && w.ToOrgCode == OrgId).OrderByDescending(g => g.TransactionId).FirstOrDefault();
            ViewBag.LastStatus = GetLastStatus.StatusCode;

            // get prefix name org
            ViewBag.PrefixCode = GetOrg.Where(w => w.OrgId == OrgId).Select(s => s.OrgShortName + "" + s.OrgNumber + "/").FirstOrDefault();

            // detail document
            var GetArchives = await DB.EOfficeArchives.Include(f => f.EOfficeArchivesRoutingTransaction).Where(w => w.ArchiveId == ArchiveId && w.EOfficeArchivesRoutingTransaction.Any(a => a.ToOrgCode == OrgId)).FirstOrDefaultAsync();

            var Details = new ArchiveViewDetailViewModel();
            Details.ArchiveNumber = (GetArchives.ArchiveNumber == 0 ? "" : ViewBag.PrefixCode + GetArchives.ArchiveNumber);
            Details.CreateBy = await DB.Users.Where(w => w.Id == GetArchives.CreateByPCode).Select(s => s.FirstName + " " + s.LastName).FirstOrDefaultAsync();
            Details.CreateDate = Helper.getDateThai(GetArchives.CreateDate);
            Details.DateOfdoc = Helper.getDateThai(GetArchives.DateOfDoc);
            Details.ExternalArchiveNumber = GetArchives.ExternalArchiveNumber;
            Details.TypeCode = GetArchives.TypeCode;
            Details.TypeName = GetTypeCode.Where(w => w.TypeCode == GetArchives.TypeCode).Select(s => s.Name).FirstOrDefault();
            Details.AssesLevelName = GetAssasLevel.Where(w => w.AccessLevel == GetArchives.AccessLevel).Select(s => s.Name).FirstOrDefault();
            Details.ExpedotionName = GetExpedotion.Where(w => w.Level == Convert.ToInt32(GetArchives.Expedition)).Select(s => s.Name).FirstOrDefault();
            Details.DocumentYear = GetArchives.BudgetYear;
            Details.Dear = GetArchives.Dear;
            Details.Title = GetArchives.Title;
            Details.Message = GetArchives.Message;
            Details.ReceiveNumber = Convert.ToInt32(GetArchives.EOfficeArchivesRoutingTransaction.Select(s => s.ReceiveNumber).FirstOrDefault());
            ViewBag.Details = Details;

            // form forword
            // Destination Org Archive Dropdown
            List<SelectListItem> DestinationOrgCode = new List<SelectListItem>();
            var GetOrgs = await DB.EOfficeOrganizations.Where(w => w.ParentId == 0).OrderBy(r => r.Position).ToListAsync();
            foreach (var Get in GetOrgs)
            {
                DestinationOrgCode.Add(new SelectListItem()
                {
                    Text = "|-" + Get.OrgName,
                    Value = Get.OrgId.ToString(),
                });

                //Level 2
                foreach (var Get2 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get.OrgMomId).OrderBy(r => r.Position))
                {
                    DestinationOrgCode.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + "|-" + Get2.OrgName,
                        Value = Get2.OrgId.ToString()
                    });

                    //Level 3
                    foreach (var Get3 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get2.OrgMomId).OrderBy(r => r.Position))
                    {
                        DestinationOrgCode.Add(new SelectListItem()
                        {
                            Text = SpaceString(20) + "|-" + Get3.OrgName,
                            Value = Get3.OrgId.ToString()
                        });

                        //Level 4
                        foreach (var Get4 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get3.OrgMomId).OrderBy(r => r.Position))
                        {
                            DestinationOrgCode.Add(new SelectListItem()
                            {
                                Text = SpaceString(20) + "|-" + Get4.OrgName,
                                Value = Get4.OrgId.ToString()
                            });

                            //Level 5
                            foreach (var Get5 in DB.EOfficeOrganizations.Where(w => w.ParentId == Get4.OrgMomId))
                            {
                                DestinationOrgCode.Add(new SelectListItem()
                                {
                                    Text = SpaceString(20) + "|-" + Get5.OrgName,
                                    Value = Get5.OrgId.ToString()
                                });
                            }
                        }
                    }
                }
            }
            ViewBag.Org = DestinationOrgCode;
            ViewBag.Command = new SelectList(DB.EOfficeArchivesCommand.Where(c => c.Enable == true).ToList(), "CmdCode", "Name");


            ViewBag.ArchiveId = ArchiveId;

            return PartialView("ForworkDocument");
        }

        [HttpGet]
        public async Task<IActionResult> AddArchiveNumber(int ArchiveNumber, Int64 ArchiveId, Int64 OrgId)
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var CheckArchivesNumber = DB.EOfficeArchives.Where(a => a.ArchiveOrgCode == OrgId && a.BudgetYear == (DateTime.Now.Year + 543) && a.ArchiveNumber == ArchiveNumber).FirstOrDefault();
                var LastArchive = DB.EOfficeArchives.Where(a => a.ArchiveOrgCode == OrgId && a.BudgetYear == (DateTime.Now.Year + 543) && a.ArchiveNumber == ArchiveNumber).OrderByDescending(d => d.ArchiveNumber).FirstOrDefault();

                // get archive data
                var GetArchive = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).FirstOrDefaultAsync();
                GetArchive.ArchiveOrgCode = DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefault();
                GetArchive.ArchiveNumber = (CheckArchivesNumber == null ? ArchiveNumber : LastArchive.ArchiveNumber + 1);
                DB.EOfficeArchives.Update(GetArchive);
                await DB.SaveChangesAsync();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> UpdateStatusArchive(string ActionName, Int64 ArchiveId, string Comment, int ReceiveNumber)
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // get archive 

                if (ActionName == "reply")
                {
                    // update status
                    var GetTransaction = await DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId).OrderByDescending(d => d.SendDate).FirstOrDefaultAsync();
                    GetTransaction.StatusCode = "04";
                    DB.EOfficeArchivesRoutingTransaction.Update(GetTransaction);
                    await DB.SaveChangesAsync();

                    var NewTransactrion = new EOfficeArchivesRoutingTransaction();
                    NewTransactrion.ArchiveId = ArchiveId;
                    NewTransactrion.FromOrgCode = GetTransaction.ToOrgCode;
                    NewTransactrion.ToOrgCode = GetTransaction.FromOrgCode;
                    NewTransactrion.Comment = Comment;
                    NewTransactrion.StatusCode = "01";
                    NewTransactrion.IsExternal = false;
                    NewTransactrion.ReceiveNumber = 0;
                    NewTransactrion.BudgetYear = DateTime.Now.Year + 543;
                    NewTransactrion.SendDate = DateTime.Now;
                    NewTransactrion.SendByPCode = CurrentUser.Id;
                    NewTransactrion.CmdCode = "02";
                    DB.EOfficeArchivesRoutingTransaction.Add(NewTransactrion);
                    await DB.SaveChangesAsync();
                }
                else
                {
                    // update status in main table
                    var GetArchive = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).FirstOrDefaultAsync();
                    GetArchive.StatusCode = "02";
                    DB.EOfficeArchives.Update(GetArchive);
                    await DB.SaveChangesAsync();

                    // update status in transaction
                    var GetTransaction = await DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId).OrderByDescending(f => f.TransactionId).FirstOrDefaultAsync();
                    GetTransaction.ReceiveByPCode = CurrentUser.Id;
                    GetTransaction.ReceiveDate = DateTime.Now;
                    GetTransaction.ReceiveNumber = ReceiveNumber;
                    GetTransaction.StatusCode = "02";
                    GetTransaction.Comment = Comment;
                    DB.EOfficeArchivesRoutingTransaction.Update(GetTransaction);
                    await DB.SaveChangesAsync();

                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForworkDocument(bool Circulation, Int64[] DestinationOrg, Int64 OrgId, string CommandCode, string Comment, Int64 ArchiveId)
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var MyOrgCode = await DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefaultAsync();
            try
            {
                if (Circulation == true)
                {
                    // archives 
                    var GetArchive = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).FirstOrDefaultAsync();
                    GetArchive.IsCirculation = true;
                    GetArchive.EditByPCode = CurrentUser.Id;
                    GetArchive.EditDate = DateTime.Now;
                    DB.EOfficeArchives.Update(GetArchive);
                    await DB.SaveChangesAsync();

                    // get last transaction 
                    var LastTransaction = await DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId).OrderByDescending(d => d.TransactionId).FirstOrDefaultAsync();
                    LastTransaction.StatusCode = "03";
                    DB.EOfficeArchivesRoutingTransaction.Update(LastTransaction);
                    await DB.SaveChangesAsync();

                    foreach (var _OrgId in DestinationOrg)
                    {
                        var Transaction = new EOfficeArchivesRoutingTransaction();
                        Transaction.ArchiveId = ArchiveId;
                        Transaction.FromOrgCode = MyOrgCode;
                        Transaction.ToOrgCode = _OrgId;
                        Transaction.SendByPCode = CurrentUser.Id;
                        Transaction.SendDate = DateTime.Now;
                        Transaction.BudgetYear = (DateTime.Now.Year + 543);
                        Transaction.Comment = Comment;
                        Transaction.CmdCode = CommandCode;
                        Transaction.StatusCode = "01";
                        Transaction.IsExternal = false;
                        DB.EOfficeArchivesRoutingTransaction.AddRange(Transaction);
                    }

                    await DB.SaveChangesAsync();
                }
                else
                {
                    // archives 
                    var GetArchive = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).FirstOrDefaultAsync();
                    GetArchive.StatusCode = "01";
                    GetArchive.EditByPCode = CurrentUser.Id;
                    GetArchive.EditDate = DateTime.Now;
                    DB.EOfficeArchives.Update(GetArchive);
                    await DB.SaveChangesAsync();

                    // get last transaction 
                    var LastTransaction = await DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId).OrderByDescending(d => d.TransactionId).FirstOrDefaultAsync();
                    LastTransaction.StatusCode = "03";
                    DB.EOfficeArchivesRoutingTransaction.Update(LastTransaction);
                    await DB.SaveChangesAsync();

                    // transaction 
                    var Transaction = new EOfficeArchivesRoutingTransaction();
                    Transaction.ArchiveId = ArchiveId;
                    Transaction.FromOrgCode = MyOrgCode;
                    Transaction.ToOrgCode = OrgId;
                    Transaction.SendByPCode = CurrentUser.Id;
                    Transaction.SendDate = DateTime.Now;
                    Transaction.BudgetYear = (DateTime.Now.Year + 543);
                    Transaction.Comment = Comment;
                    Transaction.CmdCode = CommandCode;
                    Transaction.IsExternal = false;
                    Transaction.StatusCode = "01";
                    DB.EOfficeArchivesRoutingTransaction.Add(Transaction);
                    await DB.SaveChangesAsync();
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> UpdateFinishDocument(Int64 ArchiveId, string Comment)
        {
            // current user info
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var MyOrgCode = await DB.EOfficeOrgUsers.Where(w => w.UserId == CurrentUser.Id).Select(s => s.OrgId).FirstOrDefaultAsync();
            try
            {
                // get main data
                var GetArchive = await DB.EOfficeArchives.Where(w => w.ArchiveId == ArchiveId).FirstOrDefaultAsync();
                GetArchive.EditByPCode = CurrentUser.Id;
                GetArchive.EditDate = DateTime.Now;
                GetArchive.StatusCode = "05";
                DB.EOfficeArchives.Update(GetArchive);
                await DB.SaveChangesAsync();

                // get transaction 
                var GetTransaction = await DB.EOfficeArchivesRoutingTransaction.Where(w => w.ArchiveId == ArchiveId && w.ToOrgCode == MyOrgCode).OrderByDescending(f => f.TransactionId).FirstOrDefaultAsync();
                GetTransaction.StatusCode = "05";
                GetTransaction.Comment = Comment;
                DB.EOfficeArchivesRoutingTransaction.Update(GetTransaction);
                await DB.SaveChangesAsync();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }
        
        #endregion

        #region organization

        [HttpGet]
        public async Task<IActionResult> OrganizationIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            return View("OrganizationIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            var Gets = await DB.EOfficeOrganizations.ToListAsync();
            return PartialView("GetOrganizations", Gets);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePosition(string Position)
        {
            /*
             * current user
             */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            try
            {
                var ListMenus = await DB.EOfficeOrganizations.ToListAsync();

                List<ListMenusViewModel> UpdateParentId = JsonConvert.DeserializeObject<List<ListMenusViewModel>>(Position);

                // Lv1
                int NewPosition = 1;
                foreach (var NewParent in UpdateParentId)
                {
                    var MainOrg = DB.EOfficeOrganizations.Where(a => a.OrgMomId == NewParent.id).FirstOrDefault();
                    MainOrg.ParentId = 0;
                    MainOrg.Position = NewPosition;
                    DB.EOfficeOrganizations.Update(MainOrg);
                    DB.SaveChanges();

                    if (NewParent.children != null)
                    {
                        //Lv2
                        int SubPosition = 1;
                        foreach (var List in NewParent.children)
                        {
                            var SubOrg = ListMenus.Where(a => a.OrgMomId == List.id).FirstOrDefault();
                            SubOrg.ParentId = NewParent.id;
                            SubOrg.Position = SubPosition;
                            DB.EOfficeOrganizations.Update(SubOrg);
                            DB.SaveChanges();

                            if (List.children != null)
                            {
                                //Lv3
                                int SubPosition2 = 1;
                                foreach (var List2 in List.children)
                                {
                                    var SubOrg2 = ListMenus.Where(a => a.OrgMomId == List2.id).FirstOrDefault();
                                    SubOrg2.ParentId = List.id;
                                    SubOrg2.Position = SubPosition2;
                                    DB.EOfficeOrganizations.Update(SubOrg2);
                                    DB.SaveChanges();

                                    if (List2.children != null)
                                    {
                                        //Lv4
                                        int SubPosition3 = 1;
                                        foreach (var List3 in List2.children)
                                        {
                                            var SubOrg3 = ListMenus.Where(a => a.OrgMomId == List3.id).FirstOrDefault();
                                            SubOrg3.ParentId = List2.id;
                                            SubOrg3.Position = SubPosition3;
                                            DB.EOfficeOrganizations.Update(SubOrg3);
                                            DB.SaveChanges();

                                            if (List3.children != null)
                                            {
                                                //Lv5
                                                int SubPosition4 = 1;
                                                foreach (var List4 in List3.children)
                                                {
                                                    var SubOrg4 = ListMenus.Where(a => a.OrgMomId == List4.id).FirstOrDefault();
                                                    SubOrg4.ParentId = List3.id;
                                                    SubOrg4.Position = SubPosition4;
                                                    DB.EOfficeOrganizations.Update(SubOrg4);
                                                    DB.SaveChanges();

                                                    SubPosition4++;
                                                }
                                            }

                                            SubPosition3++;
                                        }
                                    }

                                    SubPosition2++;
                                }
                            }

                            SubPosition++;
                        }
                    }

                    NewPosition++;
                }

                // log
                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขตำแหน่งโครงสร้าง", HttpContext, "E-Office");
            }
            catch (Exception e)
            {
                return Json(new { valid = false, message = e.InnerException });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteOrg(Int64 OrgId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetOrg = await DB.EOfficeOrganizations.FindAsync(OrgId);
                if (DB.EOfficeOrganizations.Where(w => w.ParentId == GetOrg.OrgMomId).Count() > 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูล" });
                }


                DB.EOfficeOrganizations.Remove(GetOrg);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "ลบตำแหน่งโครงสร้าง", HttpContext, "E-Office");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult FormAddOrg()
        {
            return PartialView("FormAddOrg");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddOrg(EOfficeOrganizations Model)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // get last data
                var GetLast = await DB.EOfficeOrganizations.OrderByDescending(f => f.OrgMomId).FirstOrDefaultAsync();

                Model.OrgMomId = (GetLast == null ? 0 : GetLast.OrgMomId + 1);
                Model.ParentId = 0;
                Model.Position = 1;
                DB.EOfficeOrganizations.Add(Model);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "เพิ่มตำแหน่งโครงสร้าง", HttpContext, "E-Office");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });

        }

        [HttpGet]
        public IActionResult FormEditOrg(Int64 OrgId)
        {
            var Get = DB.EOfficeOrganizations.Where(w => w.OrgId == OrgId).FirstOrDefault();
            return PartialView("FormEditOrg", Get);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditOrg(EOfficeOrganizations Model)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetData = await DB.EOfficeOrganizations.Where(w => w.OrgId == Model.OrgId).FirstOrDefaultAsync();
                GetData.OrgName = Model.OrgName;
                GetData.OrgNumber = Model.OrgNumber;
                GetData.OrgShortName = Model.OrgShortName;
                DB.EOfficeOrganizations.Update(GetData);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขตำแหน่งโครงสร้าง", HttpContext, "E-Office");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });

        }

        [HttpGet]
        public IActionResult FromAddUserOrg(Int64 OrgId)
        {
            ViewBag.OrgName = DB.EOfficeOrganizations.Where(w => w.OrgId == OrgId).Select(s => s.OrgName + "(" + s.OrgShortName + ")").FirstOrDefault();
            ViewBag.OrgId = OrgId;

            var UserModels = new List<SelectListItem>();
            var GetUsers = DB.Users.ToList();
            foreach (var GetUser in GetUsers)
            {
                UserModels.Add(new SelectListItem() { Text = GetUser.FirstName + " " + GetUser.LastName, Value = GetUser.Id });
            }

            ViewBag.Users = UserModels;
            return PartialView("FromAddUserOrg");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FromAddUserOrg(EOfficeOrgUsers Model, string[] UserId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                foreach (var Id in UserId)
                {
                    if (DB.EOfficeOrgUsers.Where(w => w.OrgId == Model.OrgId && w.UserId == Id).Count() == 0)
                    {
                        var _Model = new EOfficeOrgUsers();
                        _Model.UserId = Id;
                        _Model.OrgId = Model.OrgId;
                        DB.EOfficeOrgUsers.AddRange(_Model);
                    }
                }

                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขตำแหน่งโครงสร้าง", HttpContext, "E-Office");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });

        }

        [HttpGet]
        public async Task<IActionResult> ViewUsers(Int64 OrgId)
        {

            // master data
            var Users = await DB.Users.ToListAsync();

            var Models = new List<ViewUserViewModel>();
            var Gets = await DB.EOfficeOrgUsers.Where(w => w.OrgId == OrgId).ToListAsync();
            foreach (var Get in Gets)
            {
                var Model = new ViewUserViewModel();
                Model.Id = Get.OrgUserId;
                Model.Fullname = Users.Where(w => w.Id == Get.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Models.Add(Model);
            }

            return PartialView("ViewUsers", Models);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUserOrg(Int64 OrgUserId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = await DB.EOfficeOrgUsers.Where(w => w.OrgUserId == OrgUserId).FirstOrDefaultAsync();
                DB.EOfficeOrgUsers.Remove(Get);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "ลบข้อมูลผู้ใช้ออกจากโครงสร้าง", HttpContext, "E-Office");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #endregion

        #region Helper

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


        #endregion
    }
}
