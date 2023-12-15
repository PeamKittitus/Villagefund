using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DZ_VILLAGEFUND_WEB.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private ApplicationDbContext DB;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> loggerr,
            ApplicationDbContext db,
            IConfiguration configuration,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            DB = db;
            _roleManager = roleManager;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "กรุณากรอกเลขบัตรประจำตัวประชาชน")]
            [RegularExpression("^[0-9]*$", ErrorMessage = "กรุณากรอกเฉพาะตัวเลข")]
            [StringLength(13, ErrorMessage = "กรุณากรอกเลขบัตรประจำตัวประชาชน 13 หลัก", MinimumLength = 13)]
            public string IDCard { get; set; }

            [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้งาน (ภาษาอังกฤษ)")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "กรุณากรอกอีเมล")]
            [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
            public string Email { get; set; }

            [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
            [StringLength(30, MinimumLength = 6, ErrorMessage = "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร")]
            [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@@#$%^&*-]).{6,}$", ErrorMessage = "รหัสผ่านควรมีอย่างน้อย 6 ตัว\r\nมีตัวพิมพ์เล็ก (a-z) อย่างน้อย 1 ตัว\r\nมีตัวพิมพ์ใหญ่ (A-Z) อย่างน้อย 1 ตัว\r\nมีตัวเลข (0-9) อย่างน้อย 1 ตัว\r\nมีตัวอักษรพิเศษ (!@#$% 8*0_土|=) อย่างน้อย 1 ตัว")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
            [StringLength(30, MinimumLength = 6, ErrorMessage = "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร")]
            [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@@#$%^&*-]).{6,}$", ErrorMessage = "รหัสผ่านควรมีอย่างน้อย 6 ตัว\r\nมีตัวพิมพ์เล็ก (a-z) อย่างน้อย 1 ตัว\r\nมีตัวพิมพ์ใหญ่ (A-Z) อย่างน้อย 1 ตัว\r\nมีตัวเลข (0-9) อย่างน้อย 1 ตัว\r\nมีตัวอักษรพิเศษ (!@#$% 8*0_土|=) อย่างน้อย 1 ตัว")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "กรุณากรอกชื่อ")]
            public string Firstname { get; set; }

            [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
            public string Lastname { get; set; }

            public int OrgId { get; set; }

            public long VillageId { get; set; }

            public bool IsVillageMember { get; set; }
        }

        public async Task OnGetAsync(string IsVillageMember = null, string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ViewData["IsVillageMember"] = IsVillageMember == null || IsVillageMember.ToUpper() == "FALSE" ? false : true;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            Random generator = new Random();
            String SecurityCode = generator.Next(0, 999999).ToString("D6");

            returnUrl = Input.IsVillageMember == true ? "/Identity/Account/Register?IsVillageMember=True" : Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            //Check OrgID
            if (Input.IsVillageMember == false && Input.OrgId == 0)
            {
                ModelState.AddModelError(string.Empty, "กรุณาเลือกสาขา");
                return Page();
            }

            //Check ViilageId
            if (Input.IsVillageMember == true && Input.VillageId == 0)
            {
                ModelState.AddModelError(string.Empty, "กรุณาเลือกกองทุนหมู่บ้าน");
                return Page();
            }

            if (ModelState.IsValid)
            {
                if (await DB.Users.Where(w => w.Email == Input.Email || w.CitizenId == Input.IDCard).CountAsync() > 0)
                {
                    ViewData["ErrorMessage"] = "กรุณาตรวจสอบ E-mail หรือ เลขบัตรประจำตัวประชาชน";
                    return Page();
                }

                if (await DB.Users.Where(w => w.UserName == Input.UserName).CountAsync() > 0)
                {
                    ViewData["ErrorMessage"] = "Username (ชื่อใช้งาน) นี้มีในระบบแล้ว";
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = Input.UserName,
                    NormalizedUserName = Input.UserName,
                    Email = Input.Email,
                    CitizenId = Input.IDCard,
                    FirstName = Input.Firstname,
                    LastName = Input.Lastname,
                    OTPExpiryDate = DateTime.Now.AddMinutes(15),
                    OTP = SecurityCode,
                    Status = false,
                    OrgId = Input.OrgId,
                    VillageId = Input.IsVillageMember == true ? Input.VillageId : 0
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    var FindUser = await DB.Users.Where(w => w.UserName == Input.UserName).FirstOrDefaultAsync();

                    //Add Transaction Member Village
                    if (Input.IsVillageMember == true)
                    {
                        var MemberVillageUser = new TransactionMemberVillage
                        {
                            VillageId = Input.VillageId,
                            MemberCode = DB.Village.Where(w => w.VillageId == user.VillageId).Select(s => s.VillageCodeText).FirstOrDefault() + (DB.Village.Where(w => w.VillageId == user.VillageId).Count() + 1).ToString("D3"),
                            MemberFirstName = Input.Firstname,
                            MemberLastName = Input.Lastname,
                            MemberStatusId = 0,
                            MemberDate = DateTime.Now,
                            MemberEndDate = DateTime.Now.AddYears(2),
                            UpdateBy = FindUser.Id,
                            UpdateDate = DateTime.Now,
                            UserId = FindUser.Id
                        };
                        DB.TransactionMemberVillage.Add(MemberVillageUser);
                        DB.SaveChanges();

                        FindUser.MemberId = MemberVillageUser.MemberId;
                        DB.Users.Update(FindUser);
                        DB.SaveChanges();

                    }

                    // add role  
                    var GetAllRoles = Input.IsVillageMember == true ? await _roleManager.Roles.Where(w => w.Name == "VillageUser").ToListAsync() : await _roleManager.Roles.Where(w => w.Name == "user").ToListAsync();
                    foreach (var GetAllRole in GetAllRoles)
                    {
                        var roleresult = await _userManager.AddToRoleAsync(FindUser, GetAllRole.Name);
                    }

                    await SendMail(FindUser.Email, FindUser.Id, SecurityCode);

                    ViewData["SuccessMessage"] = "ลงทะเบียนสำเร็จ กรุณาตรวจสอบ E-mail ที่ได้ลงทะเบียน E-mail จะหมดอายุ (" + Helper.getDateThaiAndTime(FindUser.OTPExpiryDate) + ")";
                    ViewData["UId"] = FindUser.Id;

                    return Page();
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, (error.Description == "Passwords must have at least one non alphanumeric character." ? "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร โดยมีตัวอักษรทั้งพิมพ์ใหญ่ พิมพ์เล็ก ตัวเลข และอักขระพิเศษผสมกัน" : error.Description));
                }
            }

            return Page();
        }

        public async Task SendMail(string Email, string UserId, string SecurityCode)
        {
            try
            {
                var host = HttpContext.Request.Host.ToString();
                bool IsHttps = HttpContext.Request.IsHttps;

                var message = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                       + "\n <br/> " + "<div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                       + "\n <br/> รหัส OTP ของท่านคือ " + SecurityCode + " ยืนยันการเข้าใช้งานระบบ" + " กรุณากด link เพื่อยืนยัน <a href='" + (IsHttps == true ? "https" : "http") + "://" + host + "/Identity/Account/ConfirmAccount?Id=" + UserId + "&OTP=" + SecurityCode + "'>ยืนยัน</a><br/>"
                       + "\n <br/> "
                       + "\n <br/> - ที่ตั้งสำนักงานส่วนกลาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)"
                       + "\n <br/> - เบอร์โทรศัพท์: 02-100-4209"
                       + "\n <br/> - เบอร์แฟกซ์: 02-100-4203";

                await Helper.SendMail(Email, message);
            }
            catch (Exception Error)
            {

            }

        }
    }
}
