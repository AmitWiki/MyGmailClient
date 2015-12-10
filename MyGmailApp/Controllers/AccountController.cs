using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;

namespace MyGmailApp.Controllers
{
    public class AccountController : Controller
    {
        MyGmailApp.Models.GmailAppDataModelDataContext dataContext = new Models.GmailAppDataModelDataContext();

        // GET: Account
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            password = CalculateMD5Hash(password);

            var query = from c in dataContext.Logins
                        where (c.Username == username.ToLower() && c.Password == password)
                        select c;

            if (query.Count() != 0)
            {
                HttpContext.GetOwinContext().Authentication
                  .SignOut(DefaultAuthenticationTypes.ExternalCookie);

                Claim loginClaim = new Claim(ClaimTypes.Name, username);
                Claim[] claims = new Claim[] { loginClaim };
                ClaimsIdentity claimsIdentity =
                  new ClaimsIdentity(claims,
                    DefaultAuthenticationTypes.ApplicationCookie);

                HttpContext.GetOwinContext().Authentication.SignIn(new AuthenticationProperties() { IsPersistent = false }, claimsIdentity);

                return Redirect("/Home");
            }
            else
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return Redirect("/");
            }
        }

        [Authorize]
        public ActionResult LogOff()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return Redirect("/Home");
        }

        public ActionResult Register()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Models.Login userInfo)
        {
            if (ModelState.IsValid)
            {
                userInfo.UserRole = "RegisteredUser";
                userInfo.Active = "1";
                userInfo.Password = CalculateMD5Hash(userInfo.Password);
                dataContext.Logins.InsertOnSubmit(userInfo);
                dataContext.SubmitChanges();
                ModelState.Clear();
                userInfo = null;
                ViewBag.Message = "Registration Done Successfully!";
            }
            return View();
        }

        private string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ManageUserProfile(Models.Login userInfo)
        {
            if (Request.IsAuthenticated && User.Identity.Name != "" && ModelState.IsValid)
            {
                userInfo.Username = User.Identity.Name;
                userInfo.Password = CalculateMD5Hash(userInfo.Password);
                var existingUser = from c in dataContext.Logins
                                   where (c.Username.ToLower() == userInfo.Username.ToLower() && c.Password == userInfo.Password)
                                   select c;
                if (existingUser.Count() != 0)
                {
                    existingUser.FirstOrDefault().FullName = userInfo.FullName;
                    existingUser.FirstOrDefault().Password = CalculateMD5Hash(userInfo.NewPassword);

                    dataContext.SubmitChanges();
                }
                else
                {
                    ViewBag.Message = "Username or password is incorrect!";
                }
            }
            else
            {
                ViewBag.Message = "Please login to continue!";
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ManageGmailProfile(Models.GmailConfig userInfo)
        {
            if (Request.IsAuthenticated && User.Identity.Name != "" && ModelState.IsValid)
            {
                userInfo.Username = User.Identity.Name;
                userInfo.GmailPassword = Encrypt(userInfo.GmailPassword, true);
                var existingUser = from c in dataContext.GmailConfigs
                                   where (c.Username.ToLower() == userInfo.Username.ToLower())
                                   select c;
                if (existingUser.Count() != 0)
                {
                    Models.GmailConfig existingConfig = existingUser.FirstOrDefault();

                    existingConfig.GmailUsername = userInfo.GmailUsername;
                    existingConfig.GmailPassword = userInfo.GmailPassword;
                    existingConfig.IncomingServerAddress = userInfo.IncomingServerAddress;
                    existingConfig.IncomingServerPort = userInfo.IncomingServerPort;
                    existingConfig.OutgoingServerAddress = userInfo.OutgoingServerAddress;
                    existingConfig.OutgoingServerPort = userInfo.OutgoingServerPort;
                    existingConfig.UseSSL = userInfo.UseSSL;
                    
                    dataContext.SubmitChanges();
                }
                else
                {
                    dataContext.GmailConfigs.InsertOnSubmit(userInfo);
                    dataContext.SubmitChanges();
                }
            }
            else
            {
                ViewBag.Message = "Please login to continue!";
            }
            return View();
        }
        
        [Authorize]
        public ActionResult ManageUserProfile()
        {
            var query = (from c in dataContext.Logins
                         where (c.Username.ToLower() == User.Identity.Name.ToLower())
                         select c);

            Models.Login existingUser = new Models.Login();

            if (query.Count() == 1)
            {
                existingUser = query.FirstOrDefault();
                existingUser.Password = "";
            }

            return View(existingUser);
        }

        [Authorize]
        public ActionResult ManageGmailProfile()
        {
            var query = (from c in dataContext.GmailConfigs
                         where (c.Username.ToLower() == User.Identity.Name.ToLower())
                         select c);

            Models.GmailConfig existingUser = new Models.GmailConfig();

            if (query.Count() == 1)
            {
                existingUser = query.FirstOrDefault();
                existingUser.GmailPassword = "";
            }
            return View(existingUser);
        }

        private static string Encrypt(string toEncrypt, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            
            // Get the key from config file

            string key = (string)WebConfigurationManager.AppSettings["SecurityKey"];
            //System.Windows.Forms.MessageBox.Show(key);
            //If hashing use get hashcode regards to your key
            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                //Always release the resources and flush data
                // of the Cryptographic service provide. Best Practice

                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
              cTransform.TransformFinalBlock(toEncryptArray, 0,
              toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();
            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        
    }
}