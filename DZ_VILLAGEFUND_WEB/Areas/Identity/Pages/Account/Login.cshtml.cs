using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.DirectoryServices.Protocols;

namespace DZ_VILLAGEFUND_WEB.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private ApplicationDbContext DB;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager,
             RoleManager<ApplicationRole> roleManager,
             IConfiguration configuration,
             ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "ข้อมูลจำเป็น")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "ข้อมูลจำเป็น")]
            [DataType(DataType.Password, ErrorMessage = "รหัสผ่านไม่ถูกต้อง")]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            //bool CheckLdad = LDAPConnection("admin", "admin99");
            //if (CheckLdad == false)
            //{
            //    ModelState.AddModelError(string.Empty, "ไม่พบข้อมูลใน (LDAP) กรุณาลองใหม่อีกครั้ง");
            //    return Page();
            //}

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var FileUser = await DB.Users.Where(w => w.UserName == Input.UserName).FirstOrDefaultAsync();
                if (FileUser != null)
                {
                    if (FileUser.Status == false)
                    {
                        ModelState.AddModelError(string.Empty, "กรุณายืนยันตัวตนผ่านอีเมล " + FileUser.Email);
                        return Page();
                    }
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // log
                    var CurrentUser = await _userManager.FindByNameAsync(Input.UserName);
                    Helper.AddUsedLog(CurrentUser.Id, "เข้าสู่ระบบ", HttpContext, "ระบบ Authentication");

                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "กรุณาลองใหม่อีกครั้ง");
                    return Page();
                }

            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        public bool LDAPConnection(string Username, string Password)
        {
            bool Status = false;
            try
            {
                // Create the new LDAP connection
                LdapDirectoryIdentifier ldi = new LdapDirectoryIdentifier("172.16.100.101", 389);
                System.DirectoryServices.Protocols.LdapConnection ldapConnection = new LdapConnection(ldi);
                ldapConnection.AuthType = AuthType.Basic;
                ldapConnection.SessionOptions.SecureSocketLayer = false;
                ldapConnection.SessionOptions.ProtocolVersion = 3;
                //NetworkCredential nc = new NetworkCredential("cn="+ Username + ",dc=poc-example,dc=org", Password);
                NetworkCredential nc = new NetworkCredential(Username, Password);
                Console.WriteLine("LdapConnection is created successfully.");
                ldapConnection.Bind(nc);
                ldapConnection.Dispose();

                Status = true;
            }
            catch (LdapException e)
            {
                Status = false;
                Console.WriteLine("\r\nUnable to login:\r\n\t" + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\nUnexpected exception occured:\r\n\t" + e.GetType() + ":" + e.Message);
            }


            return Status;
        }
    }
}
