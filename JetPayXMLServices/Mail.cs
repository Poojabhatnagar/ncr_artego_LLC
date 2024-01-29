using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Data;

namespace JetXmlService
{
    public class Mail
    {

        private static string GetFormattedSize(long sSize)
        {
            string sFilesize = "";
            if (sSize > 0 && sSize <= 1024)
            {
                sFilesize = sSize.ToString("F2") + " bytes";
            }
            else if (sSize > 1024 && sSize <= Math.Pow(1024,2))
            {
                sFilesize = (sSize/1024).ToString("F2") + " KB";
            }
            else if (sSize > Math.Pow(1024, 2) && sSize <= Math.Pow(1024, 3))
            {
                sFilesize = (sSize / Math.Pow(1024, 2)).ToString("F2") + " MB";
            }
            else if (sSize > Math.Pow(1024, 3) && sSize <= Math.Pow(1024, 4))
            {
                sFilesize = (sSize / Math.Pow(1024, 3)).ToString("F2") + " GB";
            }
            else if (sSize > Math.Pow(1024, 4) && sSize <= Math.Pow(1024, 5))
            {
                sFilesize = (sSize / Math.Pow(1024, 4)).ToString("F2") + " TB";
            }

            return sFilesize;


        }

        public static void sendMail(string sFilesname, string Message, ProcessStatus sStatus, DateTime dDatetime,string FilePath, Int32 TotalMIDMapped, string FileAttachMIDitems)
        {

            System.Globalization.DateTimeFormatInfo mfi = new
            System.Globalization.DateTimeFormatInfo();
            string strCurrentMonthName = mfi.GetMonthName(DateTime.Now.Month).ToString();

            string MailFrom = JetImportService.ReadConfigFile("@MailFrom");
            string MailTo = JetImportService.ReadConfigFile("@Mailto");
            string sDomain = JetImportService.ReadConfigFile("@MailDomain");
            string isSendMail = JetImportService.ReadConfigFile("@ISMailSend");
            //string MailSubject = JetImportService.ReadConfigFile("@MailSubject") + " " +  DateTime.Today.ToString("MM-dd-yyyy");
            string MailSubject = JetImportService.ReadConfigFile("@MailSubject") + " " + JetImportService.MonthName + " " + JetImportService.sYear;
            string InnerContent = "";
            InnerContent = "Hi All, <br/> " + " File has been parsed successfully for " + JetImportService.MonthName + " " + JetImportService.sYear + " with output " + JetImportService.MappedMIDItems;

            bool EnableMail_SSL = JetImportService.ReadConfigFile("@EnableMail_SSL").Trim() == "YES" ? true : false;
            int MailPort = Convert.ToInt16(JetImportService.ReadConfigFile("@MailPortNo"));
            string MailPass = JetImportService.ReadConfigFile("@MailPass");
            
            string FileTypeForTrailertempCAN = JetImportService.ReadConfigFile("@FileTypeForTrailertempCAN");
            string ArrFilesPathsAndTypes = FileTypeForTrailertempCAN;
            double MailTimeZone = 0.0;
            System.IO.FileInfo fi = new System.IO.FileInfo(FilePath);
            string nfilesize = GetFormattedSize(fi.Length);
            
            dDatetime = dDatetime.ToUniversalTime();
            try { MailTimeZone = Convert.ToDouble(JetImportService.ReadConfigFile("@MailTimeZone") == "" ? "0.00" : JetImportService.ReadConfigFile("@MailTimeZone")); }
            catch { }
            dDatetime = dDatetime.AddHours(MailTimeZone);
            
            DateTime Completedatetime = DateTime.Now;
            Completedatetime = Completedatetime.ToUniversalTime();
            Completedatetime = Completedatetime.AddHours(MailTimeZone);

            string sCompletedatetime = "";
            if (sStatus == ProcessStatus.Error || sStatus == ProcessStatus.Processing)
            {
                sCompletedatetime = "";
            }
            else
            {
                sCompletedatetime = Completedatetime.ToString();
            }

            if (isSendMail.ToUpper() == "YES")
            {

                if (sDomain.Trim() != "")
                {
                    try
                    {

                        


                        string smsg = JetImportService.ReadTemplate();
                        
                        MailMessage message = new MailMessage();
                        var client = new SmtpClient(sDomain, MailPort)
                        {
                            EnableSsl = EnableMail_SSL,
                            Credentials = new System.Net.NetworkCredential(MailFrom, MailPass),
                            

                        };
                        if (TotalMIDMapped == 0)
                        {
                            TotalMIDMapped = 0;
                            FileAttachMIDitems = "";
                        }
                        if (TotalMIDMapped > 0)
                        {
                            smsg = smsg.Replace("@@BodyText", InnerContent);
                            smsg = smsg.Replace("@@TotalMIDMapped", TotalMIDMapped.ToString());
                            smsg = smsg.Replace("@@FileAttachMIDitems", JetImportService.MappedMIDItems.Replace("'", " ").Trim());
                            message.Attachments.Add(new Attachment(FileAttachMIDitems));
                        }
                        message.Body = smsg;
                        message.IsBodyHtml = true;
                        message.Subject = MailSubject;
                        message.From = new MailAddress(MailFrom);
                        //@Mailto
                        string[] smails = @MailTo.Split(',');
                        for(int i=0;i<smails.Length;i++)
                        {
                            message.To.Add(smails[i].ToString());
                        }
                        #region Commented
                        if (sStatus == ProcessStatus.Success)
                        {
                            client.Send(message);
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {

                        JetImportService err = new JetImportService();
                        if (sStatus == ProcessStatus.Success)
                           err.ErrorLog("File Parsed successfully but unable to send status-->mail due to " + ex.Message);
                        else
                           err.ErrorLog("File Parsing Error and also unable to send status-->mail due to " + ex.Message);


                    }
                }
            }

        }

        #region sending mail with attachments
        public static void sendMailClient(string sFilesname, string Message, ProcessStatus sStatus, DateTime dDatetime, int nPos, string FilePath, Int32 TotalNewMIDMapped, string FileAttachNewMIDitems)
        {

          
            System.Globalization.DateTimeFormatInfo mfi = new
            System.Globalization.DateTimeFormatInfo();
            string strCurrentMonthName = mfi.GetMonthName(DateTime.Now.Month).ToString();

            string MailFrom = JetImportService.ReadConfigFile("@MailFrom");
            string MailTo = JetImportService.ReadConfigFile("@MailClientto");
            string sDomain = JetImportService.ReadConfigFile("@MailDomain");
            string isSendMail = JetImportService.ReadConfigFile("@ISMailSend");
            string Clientname = JetImportService.ReadConfigFile("@Clientname");
            string MailSubject = "";
            if (JetImportService.cReportOutputMerchantStatusPath != "")
                MailSubject = "Jetpay -" + JetImportService.MonthName + " " + JetImportService.sYear + " Processed " + DateTime.Today.ToString("MM-dd-yyyy");
            else
                MailSubject = Clientname + " " + JetImportService.ReadConfigFile("@MailSubjectClient") + " " + JetImportService.MonthName + " " + JetImportService.sYear;

            bool EnableMail_SSL = JetImportService.ReadConfigFile("@EnableMail_SSL").Trim() == "YES" ? true : false;
            int MailPort = Convert.ToInt16(JetImportService.ReadConfigFile("@MailPortNo"));
            string MailPass = JetImportService.ReadConfigFile("@MailPass");          
            double MailTimeZone = 0.0;
            string nfilesize = "";
            if (FilePath != "")
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(FilePath);
                nfilesize = GetFormattedSize(fi.Length);
            }

            dDatetime = dDatetime.ToUniversalTime();
            try { MailTimeZone = Convert.ToDouble(JetImportService.ReadConfigFile("@MailTimeZone") == "" ? "0.00" : JetImportService.ReadConfigFile("@MailTimeZone")); }
            catch { }
            dDatetime = dDatetime.AddHours(MailTimeZone);

            DateTime Completedatetime = DateTime.Now;
            Completedatetime = Completedatetime.ToUniversalTime();
            Completedatetime = Completedatetime.AddHours(MailTimeZone);

            string sCompletedatetime = "";
            if (sStatus == ProcessStatus.Error || sStatus == ProcessStatus.Processing)
            {
                sCompletedatetime = "";
            }
            else
            {
                sCompletedatetime = Completedatetime.ToString();
            }

            if (isSendMail.ToUpper() == "YES")
            {

                if (sDomain.Trim() != "")
                {
                    try
                    {

                        string InnerContent = "";
                       
                        InnerContent = JetImportService.MonthName + " " + JetImportService.sYear + Clientname + " " + "  file processing report";

                        string smsg = JetImportService.ReadTemplate2();                      

                        smsg = smsg.Replace("@@sFilesname", sFilesname);
                        smsg = smsg.Replace("@@BodyText", InnerContent);                       
                        smsg = smsg.Replace("@@sSize", nfilesize);
                        smsg = smsg.Replace("@@dDatetime", dDatetime.ToString());
                        smsg = smsg.Replace("@@Completedatetime", sCompletedatetime);                    
                        smsg = smsg.Replace("@@TotalNewMIDMapped", Convert.ToString(TotalNewMIDMapped));
                        smsg = smsg.Replace("@@FileAttachNewMIDitems", JetImportService.spMIDNewInfo.Replace("'", " ").Trim());
                        MailMessage message = new MailMessage();
                        var client = new SmtpClient(sDomain, MailPort)
                        {
                            EnableSsl = EnableMail_SSL,
                            Credentials = new System.Net.NetworkCredential(MailFrom, MailPass),

                        };
                        message.Body = smsg;
                        message.IsBodyHtml = true;
                        message.Subject = MailSubject;
                        message.From = new MailAddress(MailFrom);
                        string[] smails = @MailTo.Split(',');
                        for (int i = 0; i < smails.Length; i++)
                        {
                            message.To.Add(smails[i].ToString());
                        }
                        if (sStatus == ProcessStatus.Success)
                        {
                          
                            if (TotalNewMIDMapped == 0)
                            {
                                TotalNewMIDMapped = 0;
                                FileAttachNewMIDitems = "";
                            }
                     
                            if (TotalNewMIDMapped > 0)
                                message.Attachments.Add(new Attachment(FileAttachNewMIDitems));
                        }
                        client.Send(message);

                    }
                    catch (Exception ex)
                    {

                        JetImportService err = new JetImportService();
                        if (sStatus == ProcessStatus.Success)
                            err.ErrorLog("File Parsed successfully but unable to send status-->mail due to " + ex.Message);
                        else
                            err.ErrorLog("File Parsing Error and also unable to send status-->mail due to " + ex.Message);

                    }
                }
            }

        }

        #endregion

    }
}
