using MyGmailApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.Security.Cryptography;
using System.Web.Configuration;
using System.Text;
using MailKit.Net.Imap;
using System.Net;
using MailKit;
using MimeKit;
using MailKit.Net.Smtp;

namespace MyGmailApp.Controllers
{
    [Authorize]
    public class MailController : Controller
    {
        MyGmailApp.Models.GmailAppDataModelDataContext dataContext = new Models.GmailAppDataModelDataContext();
        Models.GmailConfig userGmailConfig = new Models.GmailConfig();
        public ActionResult Index()
        {
            long noOfEmails = 1;
            UniqueId lastUid = new UniqueId();
            List<Email> emails = new List<Email>();

            try
            {
                userGmailConfig = FetchUserGmailProfile();

                using (ImapClient client = new ImapClient())
                {
                    client.Connect(userGmailConfig.IncomingServerAddress, userGmailConfig.IncomingServerPort, true);
                    client.Authenticate(new NetworkCredential(userGmailConfig.GmailUsername, userGmailConfig.GmailPassword));

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    if (inbox.Count > 0)
                    {
                        int index = Math.Max(inbox.Count - 30, 0);

                        var uids = (from c in inbox.Fetch(index, -1, MessageSummaryItems.UniqueId)
                                    select c.UniqueId).ToList();

                        var messages = inbox.Fetch(uids, MessageSummaryItems.Envelope);

                        messages = messages.OrderByDescending(message => message.Envelope.Date.Value.DateTime).ToList();

                        foreach (var message in messages)
                        {
                            Email tempEmail = new Email()
                            {
                                SerialNo = noOfEmails,
                                Uid = message.UniqueId,
                                FromDisplayName = message.Envelope.From.First().Name,
                                FromEmail = message.Envelope.From.First().ToString(),
                                To = message.Envelope.To.ToString(),
                                Subject = message.NormalizedSubject,
                                TimeReceived = message.Envelope.Date.Value.DateTime,
                                HasAttachment = message.Attachments.Count() > 0 ? true : false
                            };
                            lastUid = tempEmail.Uid;
                            emails.Add(tempEmail);
                            noOfEmails++;
                        }
                    }
                }

                ViewBag.EmailId = userGmailConfig.GmailUsername;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "There was an error in processing your request. Exception: " + ex.Message;
            }
            ViewBag.NoOfEmails = 30;
            ViewBag.PageNumber = 1;

            return View(emails);
        }

        [HttpPost]
        public ActionResult Index(int currentPageNumber, int nextPageNumber)
        {
            int startSerialNumber = ((nextPageNumber - 1) * 30) + 1;
            int endSerialNumber = ((nextPageNumber) * 30);

            List<Email> emails = new List<Email>();

            try
            {

                userGmailConfig = FetchUserGmailProfile();
                using (ImapClient client = new ImapClient())
                {
                    client.Connect(userGmailConfig.IncomingServerAddress, userGmailConfig.IncomingServerPort, true);
                    client.Authenticate(new NetworkCredential(userGmailConfig.GmailUsername, userGmailConfig.GmailPassword));

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    if (inbox.Count > 0)
                    {
                        int pageEndIndex = Math.Max(inbox.Count - startSerialNumber, 0);
                        int pageStartIndex = Math.Max(inbox.Count - endSerialNumber, 0);

                        var messages = inbox.Fetch(pageStartIndex, pageEndIndex, MessageSummaryItems.Envelope);

                        messages = messages.OrderByDescending(message => message.Envelope.Date.Value.DateTime).ToList();

                        foreach (var message in messages)
                        {
                            if (startSerialNumber <= endSerialNumber)
                            {
                                Email tempEmail = new Email()
                                {
                                    SerialNo = startSerialNumber,
                                    Uid = message.UniqueId,
                                    FromDisplayName = message.Envelope.From.First().Name,
                                    FromEmail = message.Envelope.From.First().ToString(),
                                    To = message.Envelope.To.ToString(),
                                    Subject = message.NormalizedSubject,
                                    TimeReceived = message.Envelope.Date.Value.DateTime,
                                    HasAttachment = message.Attachments.Count() > 0 ? true : false
                                };
                                emails.Add(tempEmail);
                                startSerialNumber++;
                            }
                        }
                    }
                }

                ViewBag.EmailId = userGmailConfig.GmailUsername;
                ViewBag.NoOfEmails = endSerialNumber;
                if (currentPageNumber > nextPageNumber)
                {
                    ViewBag.PageNumber = currentPageNumber - 1;
                }
                else
                {
                    ViewBag.PageNumber = currentPageNumber + 1;
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "There was an error in processing your request. Exception: " + ex.Message;
            }

            return View(emails);
        }

        [HttpPost]
        public JsonResult Compose(Email email)
        {
            JsonResult jsonResult = new JsonResult();
            string outputMessage = "";
            try
            {

                userGmailConfig = FetchUserGmailProfile();

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(email.FromEmail, email.FromEmail));

                if (email.ToAsCsv.Contains(','))
                {
                    foreach (var item in email.ToAsCsv.Split(','))
                    {
                        message.To.Add(new MailboxAddress(item, item));
                    }
                }
                else if (email.ToAsCsv.Contains(';'))
                {
                    foreach (var item in email.ToAsCsv.Split(';'))
                    {
                        message.To.Add(new MailboxAddress(item, item));
                    }
                }
                else
                {
                    message.To.Add(new MailboxAddress(email.ToAsCsv, email.ToAsCsv));
                }
                message.Subject = email.Subject;
                message.Body = new TextPart("plain")
                {
                    Text = email.Body
                };

                using (var client = new SmtpClient())
                {
                    try
                    {
                        client.Connect(userGmailConfig.OutgoingServerAddress, userGmailConfig.OutgoingServerPort);
                        client.Authenticate(new NetworkCredential(userGmailConfig.GmailUsername, userGmailConfig.GmailPassword));

                        client.Send(message);
                        client.Disconnect(true);

                        outputMessage = "Your message was sent successfully";
                    }
                    catch (Exception)
                    {
                        outputMessage = "There was an error sending your mail.";
                    }
                }
            }
            catch (Exception ex)
            {
                outputMessage = "There was an error in processing your request. Exception: " + ex.Message;
            }

            jsonResult.Data = new
            {
                message = outputMessage,
            };
            return jsonResult;
        }

        [HttpPost]
        public JsonResult Delete(string csEmailUids)
        {
            JsonResult jsonResult = new JsonResult();

            var outputMessage = "";
            try
            {

                userGmailConfig = FetchUserGmailProfile();

                using (ImapClient client = new ImapClient())
                {
                    client.Connect(userGmailConfig.IncomingServerAddress, userGmailConfig.IncomingServerPort, true);
                    client.Authenticate(new NetworkCredential(userGmailConfig.GmailUsername, userGmailConfig.GmailPassword));

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);

                    var uids = new List<UniqueId>();

                    if (csEmailUids.Contains(','))
                    {
                        foreach (var item in csEmailUids.Split(','))
                        {
                            uids.Add(new UniqueId(Convert.ToUInt32(item)));
                        }
                    }
                    else
                    {
                        uids.Add(new UniqueId(Convert.ToUInt32(csEmailUids)));
                    }

                    client.Inbox.AddFlags(uids, MessageFlags.Deleted, true);

                    if (client.Capabilities.HasFlag(ImapCapabilities.UidPlus))
                    {
                        client.Inbox.Expunge(uids);
                    }
                    else
                    {
                        client.Inbox.Expunge();
                    }
                    outputMessage = "Email(s) deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                outputMessage = "There was an error in processing your request. Exception: " + ex.Message;
            }

            jsonResult.Data = new
            {
                message = outputMessage,
            };
            return jsonResult;
        }

        [HttpPost]
        public JsonResult Read(uint emailUid)
        {
            JsonResult email = new JsonResult();
            var outputMessage = "";
            try
            {
                userGmailConfig = FetchUserGmailProfile();
                using (ImapClient client = new ImapClient())
                {
                    client.Connect(userGmailConfig.IncomingServerAddress, userGmailConfig.IncomingServerPort, true);
                    client.Authenticate(new NetworkCredential(userGmailConfig.GmailUsername, userGmailConfig.GmailPassword));

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    MimeMessage message = inbox.GetMessage(new UniqueId(emailUid));

                    email.Data = new
                    {
                        FromDisplayName = message.From.FirstOrDefault().Name,
                        FromEmail = message.From.FirstOrDefault().ToString(),
                        To = message.To.FirstOrDefault().ToString(),
                        Subject = message.Subject,
                        Body = message.HtmlBody,
                        message = "Email fetched successfully"
                    };
                }
            }
            catch (Exception ex)
            {
                outputMessage = "There was an error in processing your request. Exception: " + ex.Message;
            }

            email.Data = new
            {
                message = outputMessage,
            };
            return email;
        }

        private static string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            //get the byte code of the string

            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            //Get your key from config file to open the lock!
            string key = (string)WebConfigurationManager.AppSettings["SecurityKey"];

            if (useHashing)
            {
                //if hashing was used get the hash code with regards to your key
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                //release any resource held by the MD5CryptoServiceProvider

                hashmd5.Clear();
            }
            else
            {
                //if hashing was not implemented get the byte code of the key
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes. 
            //We choose ECB(Electronic code Book)

            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(
                                 toEncryptArray, 0, toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor                
            tdes.Clear();
            //return the Clear decrypted TEXT
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        private GmailConfig FetchUserGmailProfile()
        {
            if (Session["userGmailConfig"] == null)
            {
                var query = (from c in dataContext.GmailConfigs
                             where (c.Username.ToLower() == User.Identity.Name.ToLower())
                             select c);

                if (query.Count() == 1)
                {
                    userGmailConfig = query.FirstOrDefault();
                    userGmailConfig.GmailPassword = Decrypt(userGmailConfig.GmailPassword, true);
                    Session["userGmailConfig"] = userGmailConfig;
                }
                else
                {
                    ViewBag.Message = "You have not configured your Gmail credentials, please configure the same before proceeding.";
                    Redirect("/Account/ManageGmailProfile");
                }
            }
            else
            {
                userGmailConfig = (Models.GmailConfig)Session["userGmailConfig"];
            }
            return userGmailConfig;
        }
    }
}