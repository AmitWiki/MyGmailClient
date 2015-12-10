using MailKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace MyGmailApp.Models
{
    public class Email
    {
        public long SerialNo { get; set; }
        public UniqueId Uid { get; set; }
        public string FromEmail { get; set; }
        public string FromDisplayName { get; set; }

        public string Body { get; set; }

        public string To { get; set; }

        public string ToAsCsv { get; set; }
        public string Subject { get; set; }

        public string Status { get; set; }

        public DateTime? TimeReceived { get; set; }

        public bool HasAttachment { get; set; }
    }

    public class Categories
    {

    }
}