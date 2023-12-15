using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using QRCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Helpers
{
    public class Utility
    {
        private ApplicationDbContext DB;
        private readonly IConfiguration _configuration;

        public Utility(ApplicationDbContext db, IConfiguration configuration)
        {
            DB = db;
            _configuration = configuration;
        }

        #region general function

        public string getMonthShort(int month)
        {
            string[] MonthName = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" };
            return MonthName[month - 1].PadLeft(2, '0');
        }

        public string getMonthThai(int month)
        {
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฏาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            return MonthName[month];
        }

        public string getMonthThaiShort(int month)
        {
            string[] MonthName = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            return MonthName[month];
        }

        public string getShortDate(DateTime DateCheck)
        {
            string month = getMonthShort(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + "/" + month + "/" + (Year + 543);

        }

        public string getShortDateThai(DateTime DateCheck)
        {
            string month = getMonthThaiShort(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + " " + month + " " + (Year + 543);

        }

        public string getDateThai(DateTime DateCheck)
        {
            string month = getMonthThai(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + " " + month + " " + (Year + 543);

        }

        public string getDateThaiAndTime(DateTime DateCheck)
        {
            string month = getMonthThai(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + " " + month + " " + (Year + 543) + " เวลา " + DateCheck.Hour + ":" + DateCheck.Minute.ToString().PadLeft(2, '0') + " น.";

        }

        public string getDateThaiFromTHYear(DateTime DateCheck)
        {
            string month = getMonthThai(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + " " + month + " " + Year;

        }

        public string getDateEnShort(DateTime DateCheck)
        {
            int Year = DateCheck.Year;
            return Year.ToString() + DateCheck.ToString("-MM-dd H:mm");
        }

        public string getShortDateAndTime(DateTime DateCheck)
        {
            string month = getMonthThaiShort(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + " " + month + " " + (Year + 543) + " " + DateCheck.Hour + ":" + DateCheck.Minute.ToString().PadLeft(2, '0') + ":" + DateCheck.Second.ToString().PadLeft(2, '0');
        }

        public string getShortDateThaiAndTime(DateTime DateCheck)
        {
            string month = getMonthThaiShort(DateCheck.Month);
            int Year = DateCheck.Year;
            return DateCheck.ToString("dd") + " " + month + " " + (Year + 543) + " เวลา " + DateCheck.Hour + ":" + DateCheck.Minute.ToString().PadLeft(2, '0') + " น.";
        }

        public int getDayCrop(DateTime DateStart)
        {
            int TotalDayCrop = Convert.ToInt32((DateTime.Today - DateStart).TotalDays);
            return TotalDayCrop;
        }

        public int getProgressGrowUp(DateTime DateStart, int Age)
        {
            int DayCrop = getDayCrop(DateStart);
            int Progress = ((DayCrop * 100) / Age);
            return (Progress > 100 ? 100 : Progress);

        }

        public string TimeAgo(DateTime DateCheck)
        {
            string result = string.Empty;
            var timeSpan = DateTime.Now.Subtract(DateCheck);

            if (timeSpan <= TimeSpan.FromSeconds(60))
            {
                result = string.Format("{0} วินาทีที่แล้ว", timeSpan.Seconds);
            }
            else if (timeSpan <= TimeSpan.FromMinutes(60))
            {
                result = timeSpan.Minutes > 1 ?
                    String.Format("{0} นาทีที่แล้ว", timeSpan.Minutes) :
                    "1 นาทีที่แล้ว";
            }
            else if (timeSpan <= TimeSpan.FromHours(24))
            {
                result = timeSpan.Hours > 1 ?
                    String.Format("{0} ชั่วโมงที่แล้ว", timeSpan.Hours) :
                    "1 ชั่วโมงที่แล้ว";
            }
            else if (timeSpan <= TimeSpan.FromDays(30))
            {
                result = timeSpan.Days > 1 ?
                    String.Format("{0} วันที่แล้ว", timeSpan.Days) :
                    "เมื่อวาน";
            }
            else if (timeSpan <= TimeSpan.FromDays(365))
            {
                result = timeSpan.Days > 30 ?
                    String.Format("{0} เดือนที่แล้ว", timeSpan.Days / 30) :
                    "1 เดือนที่แล้ว";
            }
            else
            {
                result = timeSpan.Days > 365 ?
                    String.Format("{0} ปีที่แล้ว", timeSpan.Days / 365) :
                    "1 ปีที่แล้ว";
            }

            return result;
        }

        public int CurrentBudgetYear()
        {
            DateTime ThisDate = DateTime.Now;
            int ThisYear = (ThisDate.Year + 543);
            int ThisMonth = ThisDate.Month;
            int BudgetYear = 0;

            if (ThisMonth >= 10)
            {
                BudgetYear = (ThisYear + 1);
            }
            else
            {
                if (ThisMonth >= 1)
                {
                    BudgetYear = ThisYear;
                }
            }
            return BudgetYear;
        }

        public string ToAgeString(DateTime dob)
        {
            DateTime Today = DateTime.Now;
            if (dob < Today)
            {
                return string.Format("");
            }

            int oldMonth = dob.Month;
            while (oldMonth == dob.Month)
            {
                Today = Today.AddDays(-1);
                dob = dob.AddDays(-1);
            }

            int years = 0, months = 0, days = 0;

            // getting number of years
            while (dob.CompareTo(Today) >= 0)
            {
                years++;
                dob = dob.AddYears(-1);
            }
            dob = dob.AddYears(1);
            years--;


            // getting number of months and days
            oldMonth = dob.Month;
            while (dob.CompareTo(Today) >= 0)
            {
                days++;
                dob = dob.AddDays(-1);
                if ((dob.CompareTo(Today) >= 0) && (oldMonth != dob.Month))
                {
                    months++;
                    days = 0;
                    oldMonth = dob.Month;
                }
            }
            dob = dob.AddDays(1);
            days--;

            return string.Format((years == 0 ? "" : years.ToString() + " ปี ")
                + (months == 0 ? "" : months.ToString() + " เดือน ")
                + (days == 0 ? "" : days.ToString() + " วัน"));

        }

        public int ToAgeInt(DateTime dob)
        {
            DateTime Today = DateTime.Now;
            if (dob < Today)
            {
                return 0;
            }

            int oldMonth = dob.Month;
            while (oldMonth == dob.Month)
            {
                Today = Today.AddDays(-1);
                dob = dob.AddDays(-1);
            }

            int years = 0, months = 0, days = 0;

            // getting number of years
            while (dob.CompareTo(Today) >= 0)
            {
                years++;
                dob = dob.AddYears(-1);
            }
            dob = dob.AddYears(1);
            years--;


            // getting number of months and days
            oldMonth = dob.Month;
            while (dob.CompareTo(Today) >= 0)
            {
                days++;
                dob = dob.AddDays(-1);
                if ((dob.CompareTo(Today) >= 0) && (oldMonth != dob.Month))
                {
                    months++;
                    days = 0;
                    oldMonth = dob.Month;
                }
            }
            dob = dob.AddDays(1);
            days--;

            return months;

        }

        public void AddUsedLog(string UserId, string Message, HttpContext Context, string SysName)
        {
            var CheckLog = DB.SystemLogs.Where(w => w.ActionDate < DateTime.Today.AddMonths(-3)).Count();
            if (CheckLog > 0)
            {
                var Logs = DB.SystemLogs.Where(w => w.ActionDate < DateTime.Today.AddMonths(-3));
                DB.SystemLogs.RemoveRange(Logs);
                DB.SaveChanges();
            }

            var Log = new SystemLogs();
            Log.ActionDate = DateTime.Now;
            Log.IPAddress = GetIP(Context);
            Log.Action = Message;
            Log.SystemName = SysName;
            Log.UserId = UserId;
            DB.SystemLogs.Add(Log);
            DB.SaveChanges();
        }

        public string GetIP(HttpContext Context)
        {
            return Context.Connection.RemoteIpAddress.ToString();
        }

        public PermissionViewModel GetUserPermission(string UserId)
        {
            var RoleId = DB.UserRoles.Where(w => w.UserId == UserId).Select(s => s.RoleId).FirstOrDefault();
            var GetPermission = DB.SystemPermission.Where(w => w.RoleId == RoleId).FirstOrDefault();

            //// add permission
            var Model = new PermissionViewModel();
            Model.Insert = GetPermission.IsInsert;
            Model.Update = GetPermission.IsUpdate;
            Model.Delete = GetPermission.IsDelete;
            return Model;
        }

        public string GetUserRoleName(string UserId)
        {
            string RoleId = DB.UserRoles.Where(w => w.UserId == UserId).Select(s => s.RoleId).FirstOrDefault();
            return DB.Roles.Where(w => w.Id == RoleId).Select(s => s.Name).FirstOrDefault();
        }

        public string GenerateJWTToken(string userName, string UserId)
        {
            try
            {
                // check records in table
                var GetUserToken = DB.SystemUserToken.Where(w => w.UserId == UserId);
                if (GetUserToken.Count() > 0)
                {
                    // delete all tranaction
                    DB.SystemUserToken.RemoveRange(GetUserToken);
                    DB.SaveChanges();
                }

                var key = _configuration.GetValue<string>("JwtConfig:Key");
                var keyBytes = Encoding.ASCII.GetBytes(key);

                var tokentHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor()
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim("UserId", UserId),
                    new Claim(JwtRegisteredClaimNames.Sub, userName),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, "DZVFOX"+ DateTime.Now.Year)
                    }),

                    Expires = DateTime.Now.AddHours(2),
                    NotBefore = DateTime.UtcNow,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };


                var token = tokentHandler.CreateToken(tokenDescriptor);

                // insert new record
                var TokenLog = new SystemUserToken();
                TokenLog.ActiveDate = DateTime.Now;
                TokenLog.TokenKey = tokentHandler.WriteToken(token);
                TokenLog.UserId = UserId;
                DB.SystemUserToken.Add(TokenLog);
                DB.SaveChanges();

                return JsonConvert.SerializeObject(new { valid = true, data = tokentHandler.WriteToken(token) });
            }
            catch (Exception Error)
            {
                return JsonConvert.SerializeObject(new { valid = false, data = Error });
            }

        }

        public string EncriptionData(string plainText)
        {
            string PasswordHash = "P@@Sw0rd";
            string SaltKey = "aes-256-cbc";
            string VIKey = "@1B2c3D4e5F6g7H8";

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
            var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

            byte[] cipherTextBytes;

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();
                    cryptoStream.Close();
                }
                memoryStream.Close();
            }


            return Convert.ToBase64String(cipherTextBytes);
        }

        public string DecryptData(string encryptedText)
        {
            string PasswordHash = "P@@Sw0rd";
            string SaltKey = "aes-256-cbc";
            string VIKey = "@1B2c3D4e5F6g7H8";

            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
            byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.None };

            var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));
            var memoryStream = new MemoryStream(cipherTextBytes);
            var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
        }

        public string DecryptString(string encrString)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();
            byte[] todecode_byte = Convert.FromBase64String(encrString);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string result = new String(decoded_char);
            return result;
        }

        public string EnryptString(string strEncrypted)
        {
            try
            {
                byte[] encData_byte = new byte[strEncrypted.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(strEncrypted);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Encode" + ex.Message);
            }
        }

        public int GetParentOrgByOrgId(Int64 OrgId)
        {
            var GetParentOrg = DB.SystemOrgStructures.Where(w => w.OrgId == OrgId).Select(s => s.ParentId).FirstOrDefault();
            return GetParentOrg;
        }

        #endregion

        public string CheckTimeRish(Int64 AccountBudgetId, int Data)
        {
            string RiskMessage = "";
            var Get = DB.ProjectRisk.Where(w => w.AccountBudgetId == AccountBudgetId).FirstOrDefault();
            if (Get != null)
            {
                if (Data <= Get.LowTiming)
                {
                    RiskMessage = "ต่ำ";
                }
                else if (Data > Get.LowTiming && Data <= Get.MidTiming)
                {
                    RiskMessage = "ปานกลาง";
                }
                else if (Data > Get.MidTiming && Data < Get.HiTiming)
                {
                    RiskMessage = "ปานกลาง";
                }
                else
                {
                    RiskMessage = "สูง";
                }

                //if (Data >= Get.HiTiming)
                //{
                //    RiskMessage = "สูง";
                //}
                //else if (Data >= Get.MidTiming && Data <= Get.HiTiming)
                //{
                //    RiskMessage = "ปานกลาง";
                //}
                //else if (Data <= Get.LowTiming)
                //{
                //    RiskMessage = "ต่ำ";
                //}
            }

            return RiskMessage;
        }

        public string CheckActivityRish(Int64 AccountBudgetId, int Data)
        {
            string RiskMessage = "";
            var Get = DB.ProjectRisk.Where(w => w.AccountBudgetId == AccountBudgetId).FirstOrDefault();
            if (Get != null)
            {
                if (Data >= Get.LowActivity)
                {
                    RiskMessage = "ต่ำ";
                }
                else if (Data < Get.LowActivity && Data >= Get.MidActivity)
                {
                    RiskMessage = "ปานกลาง";
                }
                else if (Data < Get.MidActivity && Data > Get.HiActivity)
                {
                    RiskMessage = "ปานกลาง";
                }
                else
                {
                    RiskMessage = "สูง";
                }

                //if (Data >= Get.LowActivity)
                //{
                //    RiskMessage = "ต่ำ";
                //}
                //else if (Data >= Get.MidActivity && Data <= Get.LowActivity)
                //{
                //    RiskMessage = "ปานกลาง";
                //}
                //else if (Data <= Get.HiActivity)
                //{
                //    RiskMessage = "สูง";
                //}
            }

            return RiskMessage;
        }

        public string GetRoleUser(string UserId)
        {
            var GetRoleId = DB.UserRoles.Where(w => w.UserId == UserId).Select(s => s.RoleId).FirstOrDefault();

            return DB.Roles.Where(w => w.Id == GetRoleId).Select(s => s.Name).FirstOrDefault();
        }

        public string GetCurrentOrgCode()
        {
            var Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            return Config["AppSettings:key"];
        }

        public string GenerateQRCode(string StringURL)
        {
            QRCodeGenerator _qrCode = new QRCodeGenerator();
            QRCodeData _qrCodeData = _qrCode.CreateQrCode(StringURL, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(_qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            return Convert.ToBase64String(BitmapToBytesCode(qrCodeImage));
        }

        private byte[] BitmapToBytesCode(Bitmap qrCodeImage)
        {
            throw new NotImplementedException();
        }

        public void lineNotify(string msg)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                var postData = string.Format("message={0}", msg);
                var data = Encoding.UTF8.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Headers.Add("Authorization", "Bearer " + "Token key จาก Line เอามาไว้ตรงนี้");

                using (var stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<bool> SendSMS(string PhoneNumber, string Message)
        {
            bool Status = false;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string sContentType = "application/json";
                    JObject oJsonObject = new JObject() {
                        { "msisdn", PhoneNumber },
                        { "message", Message },
                        { "sender", "SMS." },
                        { "scheduled_delivery", ""},
                        { "force", "Corporate" },
                        { "shorten_url", false },
                    };

                    var byteArray = Encoding.ASCII.GetBytes("32a1746a78879d126c7f5b704c00f9c1:5ed87e9f8d3fec70211fa57b00fe94ac");

                    string Url = "https://api-v2.thaibulksms.com/sms";
                    client.BaseAddress = new Uri(Url);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    HttpResponseMessage response = await client.PostAsync(Url, new StringContent(oJsonObject.ToString(), Encoding.UTF8, sContentType));
                    if (response.IsSuccessStatusCode)
                    {
                        Status = true;
                    }
                    else
                    {
                        Status = false;
                    }
                }

            }
            catch (Exception)
            {
                return Status;
            }

            return Status;
        }

        public async Task<string> TranslateThToEn(string ThaiWord)
        {
            string Result = "";
            string Url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=th&tl=en&dt=t&q=" + ThaiWord;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(Url);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(Url);
                if (response.IsSuccessStatusCode)
                {
                    var Datas = await response.Content.ReadAsStringAsync();
                    var JsonDatas = JsonConvert.DeserializeObject<List<dynamic>>(Datas);
                    var translationItems = JsonDatas[0];
                    string translation = "";
                    if (translationItems != null)
                    {
                        foreach (object item in translationItems)
                        {
                            IEnumerable translationLineObject = item as IEnumerable;
                            IEnumerator translationLineString = translationLineObject.GetEnumerator();
                            translationLineString.MoveNext();
                            translation += string.Format(" {0}", Convert.ToString(translationLineString.Current));
                        }

                        if (translation.Length > 1) { translation = translation.Substring(1); };
                    }

                    Result = translation.ToLower();
                }
                else
                {
                    Result = "";
                }
            }

            return Result;
        }



        #region Notify

        public async Task<string> SendMail(string MailTo, string Message)
        {

            var Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            string FromMail = "";
            string Password = "";
            string Host = "";
            int Port = 0;

            FromMail = Config["JwtConfig:Mail"];
            Password = Config["JwtConfig:Password"];
            Host = Config["JwtConfig:Host"];
            Port = Convert.ToInt32(Config["JwtConfig:Port"]);

            try
            {
                var message = new MailMessage();
                message.To.Add(new MailAddress(MailTo));
                message.From = new MailAddress(FromMail);
                message.Subject = "Hi :" + MailTo;
                message.Body = Message;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential { UserName = FromMail.ToString(), Password = Password.ToString() };
                    smtp.EnableSsl = true;
                    smtp.Host = Host.ToString();
                    smtp.Port = Port;
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception Error)
            {
                return Error.Message;
            }

            return "Ok";

        }

        public async Task<string> SendMailWithSubject(string MailTo, string subject, string Message)
        {

            var Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            string FromMail = "";
            string Password = "";
            string Host = "";
            int Port = 0;

            FromMail = Config["JwtConfig:Mail"];
            Password = Config["JwtConfig:Password"];
            Host = Config["JwtConfig:Host"];
            Port = Convert.ToInt32(Config["JwtConfig:Port"]);

            try
            {
                var message = new MailMessage();
                message.To.Add(new MailAddress(MailTo));
                message.From = new MailAddress(FromMail);
                message.Subject = subject;
                message.Body = Message;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential { UserName = FromMail.ToString(), Password = Password.ToString() };
                    smtp.EnableSsl = true;
                    smtp.Host = Host.ToString();
                    smtp.Port = Port;
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception Error)
            {
                return Error.Message;
            }

            return "Ok";

        }

        public async Task<string> SendMailMultipleRecipients(string[] MailTo, string subject, string Message)
        {

            var Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            string FromMail = "";
            string Password = "";
            string Host = "";
            int Port = 0;

            FromMail = Config["JwtConfig:Mail"];
            Password = Config["JwtConfig:Password"];
            Host = Config["JwtConfig:Host"];
            Port = Convert.ToInt32(Config["JwtConfig:Port"]);

            try
            {
                var message = new MailMessage();

                foreach(var item in MailTo)
                {
                    message.To.Add(new MailAddress(item));
                }

                message.From = new MailAddress(FromMail);
                message.Subject = subject;
                message.Body = Message;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential { UserName = FromMail.ToString(), Password = Password.ToString() };
                    smtp.EnableSsl = true;
                    smtp.Host = Host.ToString();
                    smtp.Port = Port;
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception Error)
            {
                return Error.Message;
            }

            return "Ok";

        }

        #endregion

    }
}
