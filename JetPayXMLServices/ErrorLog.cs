using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace JetXmlService
{
    public class ErrorLog
    {
        #region  Functions

        /// <summary>
        /// Write_Log_Entry : Function Which Creates Log Entry into User Log. Log Entry must has Following 
        /// Information in Specified Manner.
        /// </summary>
        /// <param name="LogName"> DailyFileParsing.exe  </param>
        /// <param name="Source"> File Name,Which is being Parsed. </param>
        /// <param name="SourceFileDate">FDR File date/time</param>
        /// <param name="Status"> Success/Error/Aborted </param>
        /// <param name="UserId">Scheduled Job User Name</param>
        /// <param name="FPDateTime">File Processing date/time</param>
        /// <param name="LogType">Ex. SuccessLog,ErrorLog</param> 
        /// <param name="EvenLogType">EventLogEntryType Enum Values ex. Error,SuccessAudit etc</param>

        public void WriteSuccessLog(string FileName, DateTime? SourceFileDate)
        {
            EventLog log = new EventLog();
            string LogType = "Success Log", Source = "Success", Status = "Success";            
          
            try
            {
                if (!EventLog.SourceExists(Source))
                {
                    EventLog.CreateEventSource(Source,LogType);
                }
                string msg = " LogName \t" + "DailyFileParsing.exe" + "\n\n Source \t\t" + FileName + "\n\n Source File Date \t" + SourceFileDate.ToString() + "\n\n Status \t\t" + Status + "\n\n User \t\t" + ReadConfigFile("@User") + "\n\n File Processing date/time \t" + DateTime.Now.ToString();
                log.Source = Source;
                log.WriteEntry(msg, EventLogEntryType.SuccessAudit);
            }
            catch(Exception ex)
            {
               string str = ex.Message;
            }

        }

        public void WriteErrorLog(string FileName, DateTime? SourceFileDate,string ErrorDesc,string status)
        {
            EventLog log = new EventLog();
            string LogType = "Error Log", Source = status;
           
            try
            {
                if (!EventLog.SourceExists(Source))
                {
                    EventLog.CreateEventSource(Source, LogType);
                }
                string msg = " LogName \t" + "DailyFileParsing.exe"
                             + (SourceFileDate == null ? "" : "\n\n Source \t\t" + FileName + "\n\n Source File Date\t" + SourceFileDate.ToString()) + "\n\n Status \t\t" + status + "\n\n User \t\t" + ReadConfigFile("@User") + "\n\nFile Processing date/time\t" + DateTime.Now.ToString() + "\n\n Details  \t" + ErrorDesc 
                             +  (SourceFileDate == null ? " For File :" + FileName+" \t ":"");
                log.Source = Source;
                log.WriteEntry(msg, EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

        }

        #region FUNCTION public string ReadConfigFile(string ConfigKey)
        /**********************************************************************
		METHOD NAME		 : ReadConfigFile
		CREATED BY		 : RAHUL JAIN
		CREATED ON		 : 06 January 2010
		PURPOSE			 : Reads the configuration file for the Service.
		MODIFIED BY		 : 
		MODIFIED ON		 : 
		MOD. PURPOSE	 : 

		************************************************************************/

        public static string ReadConfigFile(string ConfigKey)
        {

            string sFinalValue = String.Empty;
            try
            {
                //Folder Path.
                string path = "";
                path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                // path = "C:\\epayware\\continental daily";
                path = path.Replace("file:\\", "");
                path = path.Substring(0, path.LastIndexOf("\\"));
                path = path + "\\Config\\config.txt";
                string strFilePath = path;


                //Use Filestream.
                FileStream fs1 = new FileStream(strFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                StreamReader sReader = new StreamReader(fs1);

                while (sReader.Peek() != -1)
                {
                    string strVal = sReader.ReadLine().Trim();
                    if (strVal != "")
                    {

                        if (strVal.Substring(0, 1) == "@")
                        {
                            if (strVal.Substring(0, strVal.IndexOf("=")).Trim() == ConfigKey.Trim())
                            {
                                sFinalValue = strVal.Substring(strVal.IndexOf("=") + 1).Trim();
                                break;

                            }
                        }
                    }
                }

                sReader.Close();
                fs1.Close();
            }
            catch (Exception ex)
            {


            }

            //Return the string.
            return sFinalValue.Trim();
        }

        #endregion

        #endregion
    }
}
