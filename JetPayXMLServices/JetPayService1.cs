using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.IO;
using System.Timers;

namespace JetXmlService
{
      
  
    [RunInstaller(true)]
    public partial class JetPayService1 : ServiceBase
    {
        private System.Diagnostics.EventLog EventReport;
        private System.Timers.Timer timer = null;

        public JetPayService1()
        {
            InitializeComponent();
            double interval;
            interval = 5000;
            //interval = Convert.ToDouble(Schedular.ReadConfigFile("@ServiceTime"));
            timer = new Timer(interval);
            timer.Start();
            timer.Elapsed += new ElapsedEventHandler(this.ServiceTimer_Tick);
        }

        protected override void OnStart(string[] args)
        {
           
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Start();
        }

        protected override void OnStop()
        {
            timer.AutoReset = false;
            timer.Enabled = false;
        }

        #region private void ServiceTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        private void ServiceTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.timer.Stop();
            ErrorLog ErLog = new ErrorLog();
            JetImportService FIS;
            FIS = new JetImportService();
           
            string filePath = JetImportService.ReadConfigFile("@FilePath");
            string[] ArrFilesName = JetImportService.ReadConfigFile("@FileName").Split(',');
            string FileExtension = JetImportService.ReadConfigFile("@FileExtension");
            string fileMovePath = JetImportService.ReadConfigFile("@FileMoveFolder");
            string fileBackUpPath = JetImportService.ReadConfigFile("@FileBackUpPath");

            string ArrFilesPaths = JetImportService.ReadConfigFile("@FilePath");

            string _fileName = "";
            DateTime? LastModifiedDateTime = null;
            DateTime ProcessStartDateTime = DateTime.Now;
            string FileType = "";
            int filecount = 1;

            // Artego-Engin log
            int companyId = int.Parse(JetImportService.ReadConfigFile("@CompanyId"));
            int userId = int.Parse(JetImportService.ReadConfigFile("@UserId"));
            int fileTypeId = int.Parse(JetImportService.ReadConfigFile("@FileTypeId"));
            string file_type_id = JetImportService.ReadConfigFile("@EnginfileTypeId");

            double fileSizeInKB = 0;
            int status = 1420001;

            int EngineFileId = 0;
            string orgFileName = "";

            for (int j = 0; j < ArrFilesName.Length; j++)
            {
                // getting all files of one type and moving these files into move folder and 
                // calling parsing fuction to transfer data into tables in db

                #region ValidateConnection
                /* Checking HandShaking wit
                 * h Database */
                if (!FIS.GetValidateConnection())
                {
                    FIS.ErrorLog("Database not Connected / OR Database connection string is not proper");
                    break;
                }
                /*-----------------------------------*/
                #endregion

                string[] fileEntries = Directory.GetFiles(ArrFilesPaths);
                foreach (string fileName in fileEntries)
                {
                    orgFileName = Path.GetFileName(fileName);
                    string fileWithoutExt = Path.GetFileNameWithoutExtension(orgFileName);
                    EngineFileId = FIS.SendStepsInfo2Log_SNS(companyId, "308", orgFileName, "3520002", "File Downloaded to Local Server Folder", fileSizeInKB, file_type_id, 0, 0);

                    #region 
                    string[] fNameParts = fileWithoutExt.Split('-');
                    string dateFormat = "yyyyMMdd";
                    int month = 0;
                    int year = 0;
                    string lmonth = "";
                    if (DateTime.TryParseExact(fNameParts[3], dateFormat, null, System.Globalization.DateTimeStyles.None, out DateTime fDate))
                    {
                        month = fDate.Month;
                        lmonth =  month < 10 ? "0" + month : month.ToString();
                        year = fDate.Year;                       
                    }
                    string monthYear = year.ToString() + lmonth;

                    string newFileName = "AFSProfitabilitySchema_" + monthYear + ".xml";
                    string newFilePath = Path.Combine(Path.GetDirectoryName(fileName), newFileName);
                    // Rename the file                    
                    try
                    {
                        File.Move(fileName, newFilePath);                        
                       // FIS.SendStepsInfo2Log_SNS(companyId, "206", orgFileName, "3550005", "File name changed successfully", fileSizeInKB, file_type_id, EngineFileId, 0);
                       // FIS.UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), "File Renaming", 0, "File name changed successfully", "Success");
                    }
                    catch (Exception ex)
                    {
                        FIS.ErrorLog(ex.Message);
                        //   FIS.SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3550005", "Undefined errors while file renaming -> ERROR: " + ex.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                        //  FIS.UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), "File Renaming", 0, ex.Message, "Error");
                    }
                    #endregion
                }

                string filename = Convert.ToString(ArrFilesName[j]).Trim(); 
                _fileName = filename;

                ArrayList ArrMoveFileName = new ArrayList();
                string[] strFiles = null;
                try
                {
                    strFiles = Directory.GetFiles(ArrFilesPaths, filename + FileExtension);                
                }
                catch (Exception ex)
                {
                    if (filecount == ArrFilesName.Length)
                    {
                        FIS.ErrorLog(ex.Message);
                        //ErLog.WriteErrorLog("", null, ex.Message + "..1..", "Error");
                    }
                    filecount = filecount + 1;
                    continue;
                }
                ArrayList ArrMainFileName = new ArrayList();
                if (strFiles != null)
                {
                    if (strFiles.Length > 0)
                    {
                        string strMoveFileName = "";
                        for (int i = 0; i < strFiles.Length; i++)
                        {
                            #region Iteration
                            FileInfo fi = new FileInfo(strFiles[i].ToString());
                            _fileName = fi.Name;
                            LastModifiedDateTime = fi.LastWriteTime;
                            strMoveFileName = fi.Name.ToString();
                            strMoveFileName = strMoveFileName.Substring(0, strMoveFileName.ToUpper().IndexOf(FileExtension.ToUpper()));
                            string strAppend = System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString().PadLeft(2, '0') + System.DateTime.Now.Day.ToString().PadLeft(2, '0') +
                            System.DateTime.Now.TimeOfDay.Hours.ToString() + System.DateTime.Now.TimeOfDay.Minutes.ToString() + System.DateTime.Now.TimeOfDay.Seconds.ToString() + System.DateTime.Now.TimeOfDay.Milliseconds.ToString();
                            strMoveFileName = strMoveFileName + "_" + strAppend + FileExtension;
                            long fileSizeInBytes = fi.Length;
                            fileSizeInKB = fileSizeInBytes;
                            try
                            {
                                try
                                {
                                    string msg = "File " + _fileName + " start moving into filemovefolder with name " + strMoveFileName + " at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm");
                                    //FIS.InsertInTrailerTable(FileType, _fileName, msg, ProcessStartDateTime, strMoveFileName, ProcessStatus.Processing);
                                }
                                catch (Exception Ex)
                                {
                                    //ErLog.WriteErrorLog(_fileName, LastModifiedDateTime, Ex.Message + "..2..", "Error");
                                    FIS.ErrorLog(Ex.Message);
                                }

                                // File is Moving Now from filefolder to filemovefolder

                                ProcessStartDateTime = DateTime.Now;
                                JetImportService.ProcessFileTypes = "";

                                File.Move(strFiles[i].ToString(), fileMovePath + "\\" + strMoveFileName);
                                string mailFileName = fileMovePath + "\\" + strMoveFileName;
                                try { Mail.sendMail(fi.Name.ToString(), "File has been moved", ProcessStatus.Processing, ProcessStartDateTime, mailFileName, 0, ""); }
                                catch (Exception ex) { }

                                try
                                {
                                    string msg = "File " + _fileName + " has been moved successfully into filemovefolder with name " + strMoveFileName + " at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm");
                                    //FIS.InsertInTrailerTable(FileType, _fileName, msg, ProcessStartDateTime, strMoveFileName, ProcessStatus.Processing);
                                }
                                catch (Exception Ex)
                                {
                                    //ErLog.WriteErrorLog(_fileName, LastModifiedDateTime, Ex.Message + "..3..", "Error");
                                    FIS.ErrorLog(Ex.Message);
                                }

                                if (fileBackUpPath != "")
                                {
                                    File.Copy(fileMovePath + "\\" + strMoveFileName, fileBackUpPath + "\\" + strMoveFileName);
                                }

                                ArrMoveFileName.Add(strMoveFileName);
                                ArrMainFileName.Add(fi.Name.ToString());
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.ToUpper().Trim() != "The process cannot access the file because it is being used by another process.".ToUpper().Trim())
                                {
                                    FIS.ErrorLog(ex.Message);
                                    //ErLog.WriteErrorLog(_fileName, LastModifiedDateTime, ex.Message + "..4..", "Aborted");
                                    return;
                                }
                            }
                            #endregion
                        }
                        try
                        {
                            #region select import code to be executed
                            FIS.bSuccessFileCreateFlag = false;
                            FIS.bErrorFileCreateFlag = false;
                            FIS.JetPay(fileMovePath, ArrMoveFileName, ArrMainFileName, filename, LastModifiedDateTime, ProcessStartDateTime, fileSizeInKB, file_type_id, EngineFileId, orgFileName);
                            //FIS.SuccessLog("File parsed successfully");
                            if (ArrMoveFileName.Count == ArrMainFileName.Count)
                            {
                                for (int cntFile = 0; cntFile < ArrMoveFileName.Count; cntFile++)
                                {
                                    string strFileToDele = ArrMoveFileName[cntFile].ToString().Trim();
                                    //FIS = null;
                                    FileInfo objFileInfo = new FileInfo(fileMovePath + "\\" + strFileToDele);
                                    if (objFileInfo.Exists == true)
                                    {
                                        objFileInfo.Delete();
                                    }
                                }
                            }
                            #endregion
                            //if (FIS.bCatchFlag)
                            //{
                            //   // ErLog.WriteSuccessLog(_fileName, LastModifiedDateTime);
                            //}
                            //else
                            //{
                            //}

                            // update success status into trailer table 

                            try
                            {
                                if (FIS.bCatchFlag)
                                {
                                    //string msg = "All Files Parsing Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm");
                                    //FIS.InsertInTrailerTable(FileType, _fileName, msg, ProcessStartDateTime, strMoveFileName, ProcessStatus.Success);
                                    //FIS.SuccessLog(msg);

                                }
                                else
                                {
                                    // string msg = "some Files " + _fileName + " Parsing Failed at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm");
                                    //FIS.InsertInTrailerTable(FileType, _fileName, msg, ProcessStartDateTime, strMoveFileName, ProcessStatus.Aborted);
                                    //FIS.SuccessLog(msg);
                                }
                            }
                            catch (Exception Ex)
                            {
                                //ErLog.WriteErrorLog(_fileName, LastModifiedDateTime, Ex.Message + "..5..", "Error");
                                FIS.ErrorLog(Ex.Message);
                            }

                        }
                        catch (Exception ex)
                        {
                            //CCSImportService Fis = new CCSImportService();
                            FIS.ErrorLog(ex.Message);
                            FileInfo objFileInfo = new FileInfo(fileMovePath + "\\" + strMoveFileName);
                            if (objFileInfo.Exists == true)
                            {
                                objFileInfo.Delete();
                            }

                            FIS.SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors while moving file -> ERROR: " + ex.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                            FIS.UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), "File Import Error", 0, ex.Message, "Error");

                        }

                    }
                //  }



            }
            }

            

            this.timer.Start();
        }
        #endregion
    }
}
