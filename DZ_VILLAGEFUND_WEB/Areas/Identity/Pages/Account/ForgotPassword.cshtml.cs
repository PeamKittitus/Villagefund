using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using DZ_VILLAGEFUND_WEB.Models;
using System.Net.Mail;
using System.Net;
using DZ_VILLAGEFUND_WEB.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DZ_VILLAGEFUND_WEB.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private ApplicationDbContext DB;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public ForgotPasswordModel(
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

        public class InputModel
        {
            [Required(ErrorMessage = "ข้อมมูลจำเป็น")]
            [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "ไม่พบ E-mail ในระบบ");
                    return Page();
                }

                Random generator = new Random();
                String SecurityCode = generator.Next(0, 999999).ToString("D6");

                user.OTP = SecurityCode;
                user.OTPExpiryDate = DateTime.Now.AddMinutes(15);
                DB.Users.Update(user);
                await DB.SaveChangesAsync();

                // send mail 
                await SendMail(user.Email, SecurityCode, Helper.getDateThaiAndTime(user.OTPExpiryDate));

                return RedirectToPage("./ForgotPasswordConfirmation", new  { Id = user.Id });
            }

            return Page();
        }

        public async Task SendMail(string Email, string OTP, string ExpiryDate)
        {
            try
            {
                string message = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                       + "\n <br/> " + "<div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                       + "\n <br/> รหัส OTP ของท่านคือ <span style='color:red'> " + OTP + " </span> รหัส OTP จะหมดอายุในวันที่ " + ExpiryDate + "<br/>"
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
