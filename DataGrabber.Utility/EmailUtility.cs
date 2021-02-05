using DataGrabber.LogWriter;
using DataGrabber.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Utility
{
    public sealed class EmailUtility
    {

        private EmailUtility() { }

        public static EmailUtility Instance { get; } = new EmailUtility();


        public DbResponse SendEmail(MailInformation info)
        {
            MailMessage mail = new MailMessage();
            SmtpClient mailer = new SmtpClient();
            Stream htmlStream = null;
            var response = new DbResponse();

            try
            {

                if (!string.IsNullOrEmpty(info.AttachmentPath))
                {
                    // Add attachment if any
                    AddAttachmentForMail(info, mail);
                }

                // set to mail address
                mail.From = new MailAddress(ConfigFields.MailFromAddress, ConfigFields.MailDisplayName);

                // set recipients for mail
                SetDefaultRecipientsForMail(ref mail);
                SetRecipientsForMail(info, ref mail);

                // set subject
                mail.Subject = ConfigFields.MailSubject;

                mail.IsBodyHtml = true;
                mail.Body = GenerateBody(info);

                // send mail via SMTP server
                bool isMailSent = SendEmailViaSMTPServer(mail, mailer);

                response = new DbResponse(isMailSent);

            }
            catch (Exception ex) //Module failed to load
            {
                response = new DbResponse(false, ex.Message);
                Logger.Write("error occurred in SendEmail() - EmailUtility; Message: " + ex.Message);
            }

            finally
            {
                if (mail != null) mail.Dispose();
                if (htmlStream != null) htmlStream.Dispose();
            }

            return response;
        }


        private string GenerateBody(MailInformation details)
        {
            string body = string.Empty;
            try
            {

                string Emailtemplate = ConfigFields.EmailTemplatePath;

                using (StreamReader reader = new StreamReader(Emailtemplate))
                {
                    body = reader.ReadToEnd();
                }

                // Replace components in HTML
                body = body.Replace("{{ConfigName}}", details.ConfigName);
                body = body.Replace("{{StartedOn}}", details.StartedOn);
                body = body.Replace("{{EndedOn}}", details.EndedOn);
                body = body.Replace("{{RunTime}}", details.RunTime);
                body = body.Replace("{{year}}", DateTime.Now.Year.ToString());
                
            }
            catch (Exception ex) //Module failed to load
            {
                Logger.Write("error occurred in SendMail() - GenerateBody()" + ex.Message);

            }
            return body;
        }

        private void SetDefaultRecipientsForMail(ref MailMessage mail)
        {
            try
            {
                string[] ToId = ConfigFields.MailToAddress.Trim().Split(';');
                foreach (string ToEmail in ToId)
                {
                    if (ToEmail.Length > 3)
                    {
                        mail.To.Add(new MailAddress(ToEmail));
                    }
                }

                string[] CCId = ConfigFields.MailCCAddress.Trim().Split(';');
                foreach (string ToEmail in CCId)
                {
                    if (ToEmail.Length > 3)
                    {
                        mail.CC.Add(new MailAddress(ToEmail));
                    }
                }

                string[] BCCId = ConfigFields.MailBCCAddress.Trim().Split(';');
                foreach (string ToEmail in BCCId)
                {
                    if (ToEmail.Length > 3)
                    {
                        mail.Bcc.Add(new MailAddress(ToEmail));
                    }
                }
            }
            catch (Exception ex) //Module failed to load
            {
                Logger.Write("error occurred in SetRecipientsForMail() - " + ex.Message);
            }
            finally
            {

            }

        }
        
        private void SetRecipientsForMail(MailInformation info, ref MailMessage mail)
        {
            try
            {
                string[] ToId = info.MailToAddress.Trim().Split(';');
                foreach (string ToEmail in ToId)
                {
                    if (ToEmail.Length > 3)
                    {
                        mail.To.Add(new MailAddress(ToEmail));
                    }
                }

                string[] CCId = info.MailCCAddress.Trim().Split(';');
                foreach (string ToEmail in CCId)
                {
                    if (ToEmail.Length > 3)
                    {
                        mail.CC.Add(new MailAddress(ToEmail));
                    }
                }

                string[] BCCId = info.MailBCCAddress.Trim().Split(';');
                foreach (string ToEmail in BCCId)
                {
                    if (ToEmail.Length > 3)
                    {
                        mail.Bcc.Add(new MailAddress(ToEmail));
                    }
                }
            }
            catch (Exception ex) //Module failed to load
            {
                Logger.Write("error occurred in SetRecipientsForMail() - " + ex.Message);
            }
            finally
            {

            }

        }
        
        
        private bool SendEmailViaSMTPServer(MailMessage mail, SmtpClient mailer)
        {
            bool result = false;
            try
            {
                AlternateView HTMLVIEW = AlternateView.CreateAlternateViewFromString(mail.Body, null, "text/html");
                mail.AlternateViews.Add(HTMLVIEW);

                if (string.IsNullOrEmpty(ConfigFields.Environment) || ConfigFields.Environment == "DEV")
                {
                    mailer = new SmtpClient("mail1.corporate.ingrammicro.com")
                    {
                        Credentials = CredentialCache.DefaultNetworkCredentials
                    };
                    mailer.Send(mail);
                    result = true;
                }
                else // PROD
                {
                    mailer.Host = ConfigFields.EmailHostServer;
                    mailer.Port = ConfigFields.PortNumber;
                    mailer.Credentials = new System.Net.NetworkCredential(ConfigFields.HostUserName, ConfigFields.HostPassword);// need to provide credentials
                    mailer.EnableSsl = ConfigFields.EnableSsl; // need to be configured
                    mailer.Send(mail);
                    result = true;


                }
            }
            catch (Exception ex)
            {
                result = false;
                Logger.Write("error occurred in SendMail() - SendEmailViaSMTPServer() - " + ex.Message);
            }
            finally
            {
                if (mailer != null)
                {
                    mailer.Dispose();
                }
            }

            return result;
        }

        private void AddAttachmentForMail(MailInformation info, MailMessage mail)
        {
            try
            {
                // Specify the file to be attached and sent.
                string file = info.AttachmentPath;

                // Create  the file attachment for this e-mail message.
                Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);
                // Add time stamp information for the file.
                ContentDisposition disposition = data.ContentDisposition;
                disposition.CreationDate = System.IO.File.GetCreationTime(file);
                disposition.ModificationDate = System.IO.File.GetLastWriteTime(file);
                disposition.ReadDate = System.IO.File.GetLastAccessTime(file);
                // Add the file attachment to this e-mail message.
                mail.Attachments.Add(data);
            }

            catch (Exception ex) //Module failed to load
            {
                Logger.Write($"error occurred in SendMail() - AddAttachmentForMail(). Message: {ex.Message}");

            }

        }


    }
}
