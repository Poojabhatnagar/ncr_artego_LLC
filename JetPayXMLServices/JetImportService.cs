using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.IO;
using System.Collections;
using System.Web;
using System.Net;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Amazon;
using System.IO.Compression;
using Amazon.S3.IO;
using System.Xml;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using Amazon;

namespace JetXmlService
{
    public class JetImportService
    {
        #region SNS 
        class Messages
        {
            public string Message { get; set; }
            public Dictionary<string, MessageAttributeValue> MessageAttributes { get; set; }
            public string MessageStructure { get; set; }
            public string Id { get; set; }
        }
        private readonly AmazonSimpleNotificationServiceClient snsClient;

        public JetImportService()
        {
            snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USEast1); // Use the appropriate AWS region
            Console.WriteLine(snsClient);

        }
        #endregion
        #region Variables Declaration

        /*This key is use for Encrypt the data */
        private const String strKey = "eerhsayagahb";
        public  bool bCatchFlag = true;
        private string sImportTime = "";
        private string sFileName = "";
        private string ERRORFILEPATH = "";
        public Boolean bErrorFileCreateFlag = false;
        private const string CURRDATE = "@currdate";
        private const string CFILETYPE = "@cFileType";
        private string SUCCESSFILEPATH = "";
        public Boolean bSuccessFileCreateFlag = false;
        private ErrorLog ErLog = new ErrorLog();
        private string FileName = "";
        private string FileNamePrefix = "";  
        private DateTime? LastModifiedDateTime=null;
        int counter = 0;
        byte[] Key; 
        static byte[] Vector;
        public static string MonthName = "", sYear = "";
        //string strFileTypeForTrailertemp = "CMF_XML|AIB_CMF_XML|NAB_CMF_XML";
        //string strFileTypeForTrailertempCAN = "CMF|AIB|NAB";

        string strFileTypeForTrailertemp = JetImportService.ReadConfigFile("@FileTypeForTrailertemp");
        string strFileTypeForTrailertempCAN = JetImportService.ReadConfigFile("@FileTypeForTrailertempCAN");

        
        string strFileTypeForTrailer = "";
        private DateTime Global_ProcessStartDateTime;
        public static string GlbERRORFILEPATH="";
        public static string ProcessFileTypes = "";

        public static string MappedMIDItems = "", spMIDNewInfo = "", cReportOutputPath="", cReportOutputMerchantStatusPath="",sFlag = "";

        static string accessKey = ReadConfigFile("@AccessKey");
        static string secretKey = ReadConfigFile("@SecretKey");
        static string bucketName = ReadConfigFile("@BucketName");
        static string regionName = ReadConfigFile("@RegionName");
       

        #endregion

        #region importCashManagement
        // this function is used to parse 6646 type files 

        string sMessage = "-----------" + DateTime.Now.ToString()+ "--------------\n";

        public void JetPay(string strFilePath, ArrayList ArrMoveFileName, ArrayList ArrMainFileName, String fileName, DateTime? LastModifyDate, DateTime ProcessStartDateTime, double fileSizeInKB, string file_type_id, int EngineFileId, string orgFileName)
        {

            int companyId = int.Parse(ReadConfigFile("@CompanyId"));
            int userId = int.Parse(ReadConfigFile("@UserId"));
            int fileTypeId = int.Parse(ReadConfigFile("@FileTypeId"));
            int status = int.Parse(ReadConfigFile("@InProgress"));
            int failedStatus = int.Parse(ReadConfigFile("@Failed"));
            // if (ArrMoveFileName[0].ToUpper().Trim() != orgFileName.ToUpper().Trim())
            //  orgFileName = orgFileName + " [" + ArrMoveFileName[0] + "]";

            SendStepsInfo2Log_SNS(companyId, "300", orgFileName, "3520001", "File Parsing Initiated", fileSizeInKB, file_type_id, EngineFileId, 0);

            long agoimportFileLogId = SendImportLog(companyId, userId, fileTypeId, orgFileName, ArrMoveFileName[0].ToString(), status, fileSizeInKB, EngineFileId);
            if (agoimportFileLogId > 0)
            {
                UpdateImportLogById(agoimportFileLogId, status, "File Parsing Initiated", 0, 0, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
            }
            int totalRecordCount = 0;

            Global_ProcessStartDateTime = ProcessStartDateTime;
            bSuccessFileCreateFlag = false;
            bErrorFileCreateFlag = false;
            int nProcessorID = Convert.ToInt32(JetImportService.ReadConfigFile("@nProcessorID"));
            string FileOutputPath = JetImportService.ReadConfigFile("@FilePathOutPut");
            GenerateOLAParameter.strKey = strKey;               
            string strMainFileName = "";
            string MoveFileName = "";

            string strFileType = JetImportService.ReadConfigFile("@strFileType1");

            strFileTypeForTrailer = strFileTypeForTrailertemp;
            ProcessFileTypes = strFileTypeForTrailertempCAN;

            LastModifiedDateTime = LastModifyDate;
            FileName = fileName;
            int nMonth = 0;
            int nYear = 0;

            for (int i = 0; i < ArrMoveFileName.Count; i++)
            {   
                try
                {
                    sFileName = ArrMainFileName[i].ToString().Trim();
                    strMainFileName = ArrMainFileName[i].ToString().Trim();

                    /* File tracking in database table : FileParsingTrailer*/
                    //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "STARTING FILE PARSING..." + i.ToString(), ProcessStartDateTime, sFileName, ProcessStatus.Processing,null,null);
                    /* end tracking */

                    //SuccessLog("parsing start of file :" + sFileName); 
                   
                    //sMessage = sMessage +  " parsing start of file :" + sFileName + "\n";
                    // this function is used to check file already parsed or not
                    Boolean Flag = true;

                    Boolean IsProcess = false;
                    if (Flag)
                    {
                        string strIsDelete = ReadConfigFile("@IsDelete").ToUpper().Trim();
                        if (strIsDelete == "NO")
                        {
                            sMessage = sMessage + " The File " + strMainFileName + " has already been parsed. Please contact to administrator to Re - import this file.\n";
                            ErrorLog(sMessage);
                            UpdateImportLogById(agoimportFileLogId, 201, "FILE Parsing already Completed", totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                            SendStepsInfo2Log_SNS(companyId, "201", orgFileName, "3520001", "The File " + strMainFileName + " has already been parsed", fileSizeInKB, fileTypeId.ToString(), EngineFileId, Convert.ToInt32(agoimportFileLogId));

                        }
                        else
                        {
                            IsProcess = true;
                        }
                    }
                    else
                    {
                        IsProcess = true;
                    }
                    if (IsProcess)
                    {
                      
                        /* File tracking in database table : FileParsingTrailer*/
                        //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "if records are already there in tables then start to delete", ProcessStartDateTime, sFileName, ProcessStatus.Processing,null,null);
                        DeleteExistingDataFromTempTables();
                        // By refers for terminalid
                      

                        //Trailer
                        DataTable dtTrailerHead = GetTrailerHead();
                        DataTable dtTrailerDetail = GetTrailerDetail();
                        bool isHeaderStart = false;
                        MoveFileName = strFilePath + "\\" + ArrMoveFileName[i].ToString();
                        string SplitXml = JetImportService.ReadConfigFile("@SplitXml");
                        string RecordsPerFile = JetImportService.ReadConfigFile("@RecordsPerFile");
                        string[] strFiles = null;
                        if (SplitXml != null && SplitXml != "" && SplitXml.ToUpper() == "YES")
                        {
                            //XmlDocument xmlDoc = new XmlDocument();
                            //xmlDoc.Load(MoveFileName);
                            //XmlElement root = xmlDoc.DocumentElement;
                            //int totalRows = root.ChildNodes.Count;
                            #region New code
                            XMLFileManager xlml = new XMLFileManager();
                            if (RecordsPerFile != null && RecordsPerFile != "")
                                xlml.SplitXMLFile(MoveFileName, 2, Convert.ToInt32(RecordsPerFile));
                            else
                                xlml.SplitXMLFile(MoveFileName, 2, 10000);

                            string ArrFilesPaths = JetImportService.ReadConfigFile("@FileMoveFolder");

                            string _fileName = "";
                         
                            try
                            {
                                strFiles = Directory.GetFiles(ArrFilesPaths);
                                if (strFiles != null)
                                {
                                    if (strFiles.Length > 0)
                                    {
                                        for (int j = 1; j < strFiles.Length; j++)
                                        {
                                            FileInfo fi = new FileInfo(strFiles[j].ToString());
                                            _fileName = fi.Name;

                                            DataSet ds = new DataSet();                                            
                                            ds.ReadXml(strFilePath + "\\" + _fileName);
                                           //if(ds.Tables.Count > 0)
                                           // {
                                           //     DataTable dataTable = ds.Tables[0];
                                           //     int rowCount = dataTable.Rows.Count;
                                           // }
                                            try
                                            {
                                                DataTable jetPayxmlTemp1 = new DataTable();
                                                DataTable jetPayxmlTemp2 = new DataTable();

                                                try
                                                {
                                                    if (bCatchFlag)
                                                    {
                                                        //SuccessLog("File Read :" + sFileName); //PRAS
                                                        //sMessage = sMessage + "File Read :" + sFileName + "\n";
                                                        //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "reading batches/Transaction from flat file completed successfully.", ProcessStartDateTime, sFileName, ProcessStatus.Processing, null, null);
                                                        #region Bulk Insert
                                                        //SuccessLog("Bulk Insert Start.");
                                                        string a = "";
                                                        
                                                        if (ds.Tables["MerchantProfile"] != null)
                                                        {
                                                            totalRecordCount  = totalRecordCount + ds.Tables["MerchantProfile"].Rows.Count;
                                                            BulkInsertTrailer(ds.Tables["MerchantProfile"], "tblJetMerchantProfileTemp");
                                                        }

                                                        //if (ds.Tables["AFSResidualSchema"] != null)
                                                        //{
                                                        //    BulkInsertTrailer(ds.Tables["AFSResidualSchema"], "tblJetResidualMonthlyDataTemp");
                                                        //}
                                                        if (ds.Tables["AFSResidualSchema"] != null)
                                                        {
                                                            totalRecordCount = totalRecordCount + ds.Tables["AFSResidualSchema"].Rows.Count;
                                                            BulkInsertTrailer(ds.Tables["AFSResidualSchema"], "tblJetResidualMonthlyDataTemp_tmp"); 
                                                            nMonth = Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString());
                                                            nYear = Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString());
                                                        }


                                                        #endregion
                                                    }
                                                }
                                                catch (Exception exBulk)
                                                {
                                                    bCatchFlag = false;
                                                    sMessage = sMessage + " Error in  Bulk Insert : " + exBulk.Message + "\n";
                                                    ErrorLog(sMessage);
                                                    SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + exBulk.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                                    UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + exBulk.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                                    UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), " Error in  Bulk Insert ", 0, exBulk.Message, "Error");
                                                }



                                            }
                                            catch (Exception ex)
                                            {
                                                // ErrorLog("Error in File # " + (i + 1).ToString() + "File Name  " + sFileName + " ");
                                                sMessage = sMessage + " Error in File # " + (i + 1).ToString() + "File Name  " + sFileName + " importCashManagement Function : " + ex.Message + "\n";
                                                ErrorLog(sMessage);
                                                //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Error in files Parsing , not Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + ex.Message, ProcessStartDateTime, sFileName, ProcessStatus.Error, null, null);
                                                bCatchFlag = false;
                                                SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + ex.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                                UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + ex.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                                UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), "Error in  Bulk Insert", 0, ex.Message, "Error");
                                                return;
                                            }
                                            // bCatchFlag = true;

                                        }
                                        try
                                        {
                                            if (bCatchFlag == true)
                                            {


                                                //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Data Trasfering in real " + strFileType, ProcessStartDateTime, sFileName, ProcessStatus.Processing, null, null);
                                                DataTransferIntoRealTables(strFileType, sFileName, MoveFileName, nProcessorID, "spInsertJetPayData"); 

                                                int statusDone = int.Parse(ReadConfigFile("@Completed"));
                                                string activityCode = "201";
                                                string eventType = "3520001";
                                               // int totalRecordCount = 0;

                                                UpdateImportLogById(agoimportFileLogId, statusDone, "FILE Parsing Completed", totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                                SendStepsInfo2Log_SNS(companyId, activityCode, orgFileName, eventType, "File Parsing Completed", fileSizeInKB, fileTypeId.ToString(), EngineFileId, Convert.ToInt32(agoimportFileLogId));


                                            }
                                        }
                                        catch (Exception ExRealTransfer)
                                        {
                                            sMessage = sMessage + " Error in file transfer into real table : " + ExRealTransfer.Message + "\n";
                                            ErrorLog(sMessage);
                                            //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Error in files Parsing , not Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + ExRealTransfer.Message, ProcessStartDateTime, sFileName, ProcessStatus.Error, null, null);
                                            bCatchFlag = false;
                                            SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + ExRealTransfer.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                            UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + ExRealTransfer.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                            
                                            UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), " Error in file transfer into real table ", 0, ExRealTransfer.Message, "Error");

                                            return;
                                        }

                                       
                                        if (bCatchFlag)
                                        {
                                            //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "File # (" + i.ToString() + ") (" + " " + sFileName + " OUT OF / " + ArrMoveFileName.Count.ToString() + ") Parsing Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm"), ProcessStartDateTime, sFileName, ProcessStatus.Success, null, null);
                                            //sMessage = sMessage + "File Parsing Completed for File Name " + sFileName + "[" + (i + 1).ToString() + "]  successfully OUT OF / " + ArrMoveFileName.Count.ToString() + "\n";
                                            SuccessLog("The File " + orgFileName + " has been successfully parsed. ");
                                            string strIsMID = ReadConfigFile("@spGetMIDInfoLLC").ToUpper().Trim();
                                            if (strIsMID != null && strIsMID != "")
                                            {

                                                bool IsReportCsv = false;
                                                int TotalMIDMapped = 0, TotalNewMIDMapped = 0;
                                                string FileOutputPath8 = "", FileOutputPath9 = "";
                                                sFlag = ReadConfigFile("@Flag");
                                                bool isPass = false;
                                                DataSet dsMIDItems = new DataSet();
                                                DataSet dsNewMIDItems = new DataSet();
                                                //dtMIDItems = (DataTable)GetMIDInfoLLC(nProcessorID, strIsMID,Convert.ToInt32(System.DateTime.Now.Year), Convert.ToInt32(System.DateTime.Now.Month));
                                                dsMIDItems = GetMIDInfoLLC(nProcessorID, strIsMID, Convert.ToInt32(nYear), Convert.ToInt32(nMonth.ToString()));
                                                dsNewMIDItems = GetMIDInfoLLCNew(nProcessorID, strIsMID, Convert.ToInt32(nYear.ToString()), Convert.ToInt32(nMonth.ToString()));

                                                #region MIDInfo
                                                if (dsMIDItems.Tables.Count > 0)
                                                {
                                                    if (dsMIDItems.Tables[0].Rows.Count > 0)
                                                    {
                                                        TotalMIDMapped = Convert.ToInt32(dsMIDItems.Tables[0].Rows.Count.ToString());
                                                        FileOutputPath8 = ReadConfigFile("@FilePathOutPut");
                                                        if (FileOutputPath8.Trim() != "")
                                                        {
                                                            // MappedMIDItems = MonthName + " " + sYear + "_MappedMIDItems.csv      
                                                            MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[Convert.ToInt32(nMonth.ToString()) - 1].ToString();// Microsoft.VisualBasic.DateAndTime.MonthName((sMonthY[0]));
                                                            sYear = nYear.ToString();
                                                            //MappedMIDItems = "MER_" + nProcessorID + "_" + dsMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(System.DateTime.Now.Month) < 10 ? Convert.ToString("0" + System.DateTime.Now.Month) : System.DateTime.Now.Month.ToString()) + System.DateTime.Now.Year.ToString() + "_" + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + "_" + dsMIDItems.Tables[1].Rows.Count.ToString() + ".csv";
                                                            /*code commented on 20220307*/
                                                            //MappedMIDItems = "MER_" + nProcessorID + "_" + dsMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(nMonth.ToString()) < 10 ? Convert.ToString(nMonth.ToString()) : nMonth.ToString()) + nYear.ToString() + "_" + Convert.ToString(DateTime.Now.ToString("MMddyyyy").Replace(":", "").Trim() + "" + DateTime.Now.ToString("HH:mm:ss").Replace(":", "").Trim()) + "_" + dsMIDItems.Tables[0].Rows.Count.ToString() + ".csv";
                                                            MappedMIDItems = "MER_" + nProcessorID + "_" + dsMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(nMonth) < 10 ? Convert.ToString("0" + nMonth) : nMonth.ToString()) + nYear.ToString() + "_" + Convert.ToString(DateTime.Now.ToString("MMddyyyy").Replace(":", "").Trim() + "" + DateTime.Now.ToString("HH:mm:ss").Replace(":", "").Trim()) + "_" + dsMIDItems.Tables[0].Rows.Count.ToString() + ".csv";
                                                            FileOutputPath8 = FileOutputPath8 + "\\" + MappedMIDItems;
                                                            cReportOutputPath = FileOutputPath8;
                                                            isPass = GenerateCsvMerchantStatusClosedOpen(dsMIDItems.Tables[0], fileName, FileOutputPath8);
                                                            if (ReadConfigFile("@AccessKey") != null && ReadConfigFile("@AccessKey").ToString() != "")
                                                            {
                                                                #region Uploading File in AWS S3 bucket
                                                                AmazonS3Config config = new AmazonS3Config();
                                                                config.RegionEndpoint = RegionEndpoint.GetBySystemName(regionName);
                                                                //-- Create the client
                                                                AmazonS3Client s3Client = new AmazonS3Client(accessKey, secretKey, config);
                                                                FileStream MainFS = new FileStream(ReadConfigFile("@FilePathOutPut") + "\\" + MappedMIDItems, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                                                StreamReader sReader = new StreamReader(MainFS);

                                                                ErrorLog("File Path: " + ReadConfigFile("@FilePathOutPut") + "\\" + MappedMIDItems);
                                                                ErrorLog("File Mode: "+ FileMode.OpenOrCreate);
                                                                ErrorLog("File Access:" + FileAccess.ReadWrite);

                                                                TransferUtility utility = new TransferUtility(s3Client);
                                                                TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
                                                                //request.BucketName = bucketName + @"/" + ReadConfigFile("@FileUploadFolderName"); //no subdirectory just bucket name                 
                                                                request.BucketName = bucketName; //no subdirectory just bucket name                 
                                                                request.Key = ReadConfigFile("@FileUploadFolderName") + @"/" + MappedMIDItems; //file name up in S3  
                                                                request.InputStream = MainFS;

                                                                Console.WriteLine("Printing Upload Request Details:");
                                                                ErrorLog("BucketName:" + request.BucketName);
                                                                ErrorLog("Key:" + request.Key);
                                                                ErrorLog($"ContentType: " +request.ContentType);

                                                                utility.Upload(request); //commensing the transfer 
                                                                sReader.Close();
                                                                MainFS.Close();
                                                                sReader.Dispose();
                                                                MainFS.Dispose();
                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion

                                                #region MIDInfoNew
                                                if (dsNewMIDItems.Tables.Count > 0)
                                                {
                                                    if (dsNewMIDItems.Tables[0].Rows.Count > 0)
                                                    {
                                                        TotalNewMIDMapped = Convert.ToInt32(dsNewMIDItems.Tables[0].Rows.Count.ToString());
                                                        FileOutputPath9 = ReadConfigFile("@FilePathOutPut");
                                                        if (FileOutputPath9.Trim() != "")
                                                        {

                                                            MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[Convert.ToInt32(nMonth.ToString()) - 1].ToString();// Microsoft.VisualBasic.DateAndTime.MonthName((sMonthY[0]));
                                                            sYear = nYear.ToString();
                                                            spMIDNewInfo = "NEWMER_" + nProcessorID + "_" + dsNewMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(nMonth.ToString()) < 10 ? Convert.ToString(nMonth.ToString()) : nMonth.ToString()) + nYear.ToString() + "_" + Convert.ToString(DateTime.Now.ToString("MMddyyyy").Replace(":", "").Trim() + "" + DateTime.Now.ToString("HH:mm:ss").Replace(":", "").Trim()) + "_" + dsNewMIDItems.Tables[0].Rows.Count.ToString() + ".csv";
                                                            FileOutputPath9 = FileOutputPath9 + "\\" + spMIDNewInfo;
                                                            cReportOutputPath = FileOutputPath9;
                                                            isPass = GenerateCsvMerchantStatusClosedOpen(dsNewMIDItems.Tables[0], fileName, FileOutputPath9);
                                                            try { Mail.sendMailClient(sFileName.ToString(), "File has been parsed successfully", ProcessStatus.Success, ProcessStartDateTime, 0, cReportOutputPath, TotalNewMIDMapped, FileOutputPath9); }
                                                            catch (Exception ex) { }


                                                        }
                                                    }
                                                }
                                                #endregion


                                                try { Mail.sendMail(sFileName.ToString(), "File has been parsed successfully", ProcessStatus.Success, ProcessStartDateTime, MoveFileName, TotalMIDMapped, FileOutputPath8); }
                                                catch (Exception ex) { }
                                                string[] files = Directory.GetFiles(ArrFilesPaths);
                                                foreach (string file in files)
                                                {
                                                    File.Delete(file);

                                                }
                                            }
                                            else
                                            {
                                                try { Mail.sendMail(sFileName.ToString(), "File has been parsed successfully", ProcessStatus.Success, ProcessStartDateTime, MoveFileName, 0, ""); }
                                                catch (Exception ex) { }
                                                string[] files = Directory.GetFiles(ArrFilesPaths);
                                                foreach (string file in files)
                                                {
                                                    File.Delete(file);

                                                }
                                            }

                                            
                                        }
                                        else
                                        {
                                            try { Mail.sendMail(sFileName.ToString(), "Error in file parsing", ProcessStatus.Error, ProcessStartDateTime, MoveFileName, 0, ""); }
                                            catch (Exception ex) { }
                                            string[] files = Directory.GetFiles(ArrFilesPaths);
                                            foreach (string file in files)
                                            {
                                                File.Delete(file);

                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                sMessage = sMessage + " Error in  Bulk Insert : " + ex.Message + "\n";
                                ErrorLog(sMessage);
                                SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + ex.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + ex.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), " Error in  Bulk Insert ", 0, ex.Message, "Error"); 

                            }




                            #endregion
                        }
                        else
                        {
                            #region Old Code
                            DataSet ds = new DataSet();
                            ds.ReadXml(MoveFileName);
                            /* File tracking in database table : FileParsingTrailer*/


                            //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Start reading batches/Transaction from flat file.", ProcessStartDateTime, sFileName, ProcessStatus.Processing, null, null);
                            //sMessage = sMessage + " Start reading batches/Transaction from flat file. :" + sFileName;

                            /* end tracking */


                            try
                            {
                                DataTable jetPayxmlTemp1 = new DataTable();
                                DataTable jetPayxmlTemp2 = new DataTable();

                                try
                                {
                                    if (bCatchFlag)
                                    {
                                        //SuccessLog("File Read :" + sFileName); //PRAS
                                        //sMessage = sMessage + "File Read :" + sFileName + "\n";
                                        //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "reading batches/Transaction from flat file completed successfully.", ProcessStartDateTime, sFileName, ProcessStatus.Processing, null, null);
                                        #region Bulk Insert
                                        //SuccessLog("Bulk Insert Start.");
                                        string a = "";



                                        if (ds.Tables["MerchantProfile"] != null)
                                        {
                                            BulkInsertTrailer(ds.Tables["MerchantProfile"], "tblJetMerchantProfileTemp"); 
                                        }

                                        //if (ds.Tables["AFSResidualSchema"] != null)
                                        //{
                                        //    BulkInsertTrailer(ds.Tables["AFSResidualSchema"], "tblJetResidualMonthlyDataTemp");
                                        //}
                                        if (ds.Tables["AFSResidualSchema"] != null)
                                        {
                                             BulkInsertTrailer(ds.Tables["AFSResidualSchema"], "tblJetResidualMonthlyDataTemp_tmp");
                                        }


                                        #endregion
                                    }
                                }
                                catch (Exception exBulk)
                                {
                                    bCatchFlag = false;
                                    sMessage = sMessage + " Error in  Bulk Insert : " + exBulk.Message + "\n";
                                    ErrorLog(sMessage);
                                    SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + exBulk.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                    UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + exBulk.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                    UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), " Error in  Bulk Insert ", 0, exBulk.Message, "Error");

                                }



                                try
                                {
                                    if (bCatchFlag == true)
                                    {


                                        //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Data Trasfering in real " + strFileType, ProcessStartDateTime, sFileName, ProcessStatus.Processing, null, null);
                                        DataTransferIntoRealTables(strFileType, sFileName, MoveFileName, nProcessorID, "spInsertJetPayData");
                                        int statusDone = int.Parse(ReadConfigFile("@Completed"));
                                        string activityCode = "201";
                                        string eventType = "3520001";
                                        UpdateImportLogById(agoimportFileLogId, statusDone, "FILE Parsing Completed", totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                        SendStepsInfo2Log_SNS(companyId, activityCode, orgFileName, eventType, "File Parsing Completed", fileSizeInKB, fileTypeId.ToString(), EngineFileId, Convert.ToInt32(agoimportFileLogId));


                                    }
                                }
                                catch (Exception ExRealTransfer)
                                {
                                    sMessage = sMessage + " Error in  Bulk Insert : " + ExRealTransfer.Message + "\n";
                                    ErrorLog(sMessage);

                                    //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Error in files Parsing , not Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + ExRealTransfer.Message, ProcessStartDateTime, sFileName, ProcessStatus.Error, null, null);
                                    bCatchFlag = false;
                                    SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + ExRealTransfer.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                    UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + ExRealTransfer.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                    UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), " Error in  Bulk Insert ", 0, ExRealTransfer.Message, "Error");

                                    return;
                                }

                                if (bCatchFlag)
                                {
                                    //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "File # (" + i.ToString() + ") (" + " " + sFileName + " OUT OF / " + ArrMoveFileName.Count.ToString() + ") Parsing Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm"), ProcessStartDateTime, sFileName, ProcessStatus.Success, null, null);
                                    //sMessage = sMessage + "File Parsing Completed for File Name " + sFileName + "[" + (i + 1).ToString() + "]  successfully OUT OF / " + ArrMoveFileName.Count.ToString() + "\n";
                                    SuccessLog(sMessage);
                                    string strIsMID = ReadConfigFile("@spGetMIDInfoLLC").ToUpper().Trim();
                                    if (strIsMID != null && strIsMID != "")
                                    {

                                        bool IsReportCsv = false;
                                        int TotalMIDMapped = 0, TotalNewMIDMapped = 0;
                                        string FileOutputPath8 = "", FileOutputPath9 = "";
                                        sFlag = ReadConfigFile("@Flag");
                                        bool isPass = false;
                                        DataSet dsMIDItems = new DataSet();
                                        DataSet dsNewMIDItems = new DataSet();
                                        //dtMIDItems = (DataTable)GetMIDInfoLLC(nProcessorID, strIsMID,Convert.ToInt32(System.DateTime.Now.Year), Convert.ToInt32(System.DateTime.Now.Month));
                                        dsMIDItems = GetMIDInfoLLC(nProcessorID, strIsMID, Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString()), Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()));
                                        dsNewMIDItems = GetMIDInfoLLCNew(nProcessorID, strIsMID, Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString()), Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()));

                                        #region MIDInfo
                                        if (dsMIDItems.Tables.Count > 0)
                                        {
                                            if (dsMIDItems.Tables[0].Rows.Count > 0)
                                            {
                                                TotalMIDMapped = Convert.ToInt32(dsMIDItems.Tables[0].Rows.Count.ToString());
                                                FileOutputPath8 = ReadConfigFile("@FilePathOutPut");
                                                if (FileOutputPath8.Trim() != "")
                                                {
                                                    // MappedMIDItems = MonthName + " " + sYear + "_MappedMIDItems.csv      
                                                    MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) - 1].ToString();// Microsoft.VisualBasic.DateAndTime.MonthName((sMonthY[0]));
                                                    sYear = ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString();
                                                    //MappedMIDItems = "MER_" + nProcessorID + "_" + dsMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(System.DateTime.Now.Month) < 10 ? Convert.ToString("0" + System.DateTime.Now.Month) : System.DateTime.Now.Month.ToString()) + System.DateTime.Now.Year.ToString() + "_" + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + "_" + dsMIDItems.Tables[1].Rows.Count.ToString() + ".csv";
                                                    MappedMIDItems = "MER_" + nProcessorID + "_" + dsMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) < 10 ? Convert.ToString(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) : ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) + ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString() + "_" + Convert.ToString(DateTime.Now.ToString("MMddyyyy").Replace(":", "").Trim() + "" + DateTime.Now.ToString("HH:mm:ss").Replace(":", "").Trim()) + "_" + dsMIDItems.Tables[0].Rows.Count.ToString() + ".csv";
                                                    FileOutputPath8 = FileOutputPath8 + "\\" + MappedMIDItems;
                                                    cReportOutputPath = FileOutputPath8;
                                                    isPass = GenerateCsvMerchantStatusClosedOpen(dsMIDItems.Tables[0], fileName, FileOutputPath8);
                                                    if (ReadConfigFile("@AccessKey") != null && ReadConfigFile("@AccessKey").ToString() != "")
                                                    {
                                                        #region Uploading File in AWS S3 bucket
                                                        AmazonS3Config config = new AmazonS3Config();
                                                        config.RegionEndpoint = RegionEndpoint.GetBySystemName(regionName);
                                                        //-- Create the client
                                                        AmazonS3Client s3Client = new AmazonS3Client(accessKey, secretKey, config);
                                                        FileStream MainFS = new FileStream(ReadConfigFile("@FilePathOutPut") + "\\" + MappedMIDItems, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                                        StreamReader sReader = new StreamReader(MainFS);
                                                        TransferUtility utility = new TransferUtility(s3Client);
                                                        TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
                                                        request.BucketName = bucketName + @"/" + ReadConfigFile("@FileUploadFolderName"); //no subdirectory just bucket name                 
                                                        request.Key = MappedMIDItems; //file name up in S3  
                                                        request.InputStream = MainFS;
                                                        utility.Upload(request); //commensing the transfer 
                                                        sReader.Close();
                                                        MainFS.Close();
                                                        sReader.Dispose();
                                                        MainFS.Dispose();
                                                        #endregion
                                                    }
                                                }
                                            }
                                        }
                                        #endregion

                                        #region MIDInfoNew
                                        if (dsNewMIDItems.Tables.Count > 0)
                                        {
                                            if (dsNewMIDItems.Tables[0].Rows.Count > 0)
                                            {
                                                TotalNewMIDMapped = Convert.ToInt32(dsNewMIDItems.Tables[0].Rows.Count.ToString());
                                                FileOutputPath9 = ReadConfigFile("@FilePathOutPut");
                                                if (FileOutputPath9.Trim() != "")
                                                {

                                                    MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) - 1].ToString();// Microsoft.VisualBasic.DateAndTime.MonthName((sMonthY[0]));
                                                    sYear = ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString();
                                                    spMIDNewInfo = "NEWMER_" + nProcessorID + "_" + dsNewMIDItems.Tables[1].Rows[0][0].ToString() + "_" + Convert.ToString(Convert.ToInt32(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) < 10 ? Convert.ToString(ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) : ds.Tables["AFSResidualSchema"].Rows[0]["Month"].ToString()) + ds.Tables["AFSResidualSchema"].Rows[0]["Year"].ToString() + "_" + Convert.ToString(DateTime.Now.ToString("MMddyyyy").Replace(":", "").Trim() + "" + DateTime.Now.ToString("HH:mm:ss").Replace(":", "").Trim()) + "_" + dsNewMIDItems.Tables[0].Rows.Count.ToString() + ".csv";
                                                    FileOutputPath9 = FileOutputPath9 + "\\" + spMIDNewInfo;
                                                    cReportOutputPath = FileOutputPath9;
                                                    isPass = GenerateCsvMerchantStatusClosedOpen(dsNewMIDItems.Tables[0], fileName, FileOutputPath9);
                                                    try { Mail.sendMailClient(sFileName.ToString(), "File has been parsed successfully", ProcessStatus.Success, ProcessStartDateTime, 0, cReportOutputPath, TotalNewMIDMapped, FileOutputPath9); }
                                                    catch (Exception ex) { }


                                                }
                                            }
                                        }
                                        #endregion


                                        try { Mail.sendMail(sFileName.ToString(), "File has been parsed successfully", ProcessStatus.Success, ProcessStartDateTime, MoveFileName, TotalMIDMapped, FileOutputPath8); }
                                        catch (Exception ex) { }
                                    }
                                    else
                                    {
                                        try { Mail.sendMail(sFileName.ToString(), "File has been parsed successfully", ProcessStatus.Success, ProcessStartDateTime, MoveFileName, 0, ""); }
                                        catch (Exception ex) { }
                                    }
                                }
                                else
                                {
                                    try { Mail.sendMail(sFileName.ToString(), "Error in file parsing", ProcessStatus.Error, ProcessStartDateTime, MoveFileName, 0, ""); }
                                    catch (Exception ex) { }
                                }
                            }
                            catch (Exception ex)
                            {
                                // ErrorLog("Error in File # " + (i + 1).ToString() + "File Name  " + sFileName + " ");
                                sMessage = sMessage + " Error in File # " + (i + 1).ToString() + "File Name  " + sFileName + " importCashManagement Function : " + ex.Message + "\n";
                                ErrorLog(sMessage);
                                //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Error in files Parsing , not Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + ex.Message, ProcessStartDateTime, sFileName, ProcessStatus.Error, null, null);
                                bCatchFlag = false;
                                SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + ex.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                                UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + ex.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                                UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), " Error in  Bulk Insert ", 0, ex.Message, "Error");

                                return;
                            }
                            #endregion
                        }





                    }                        
                }
                catch (Exception ex)
                {
                    sMessage = sMessage + " Error in importCashManagement Function : " + ex.Message + "\n";
                    ErrorLog(sMessage);
                    SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors while moving file -> ERROR: " + ex.Message, fileSizeInKB, file_type_id, EngineFileId, 0);
                    UpdateLogs2EngineDB(EngineFileId, orgFileName, file_type_id.ToString(), "File Import Error", 0, ex.Message, "Error");
                    UpdateErrorLogById(agoimportFileLogId, failedStatus, "Undefined errors-" + ex.Message, totalRecordCount, totalRecordCount, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);

                    //ErLog.WriteErrorLog(sFileName, LastModifiedDateTime, ex.Message, "Aborted");
                    //InsertInTrailerTable(strFileTypeForTrailer, sFileName, "Error in files Parsing , not Completed successfully at " + DateTime.Now.ToString("MM/dd/yyyy hh:mm") + ex.Message, ProcessStartDateTime, sFileName, ProcessStatus.Error,null,null);
                    bCatchFlag = false;
                    return;
                }
                //SuccessLog("parsing end of file :" + sFileName);
            }
        }
        #endregion
        public class XMLFileManager
        {

            public List<string> SplitXMLFile(string fileName, int startingLevel, int numEntriesPerFile)
            {
                List<string> resultingFilesList = new List<string>();

                XmlReaderSettings readerSettings = new XmlReaderSettings();
                //readerSettings. = DtdProcessing.Parse;
                XmlReader reader = XmlReader.Create(fileName, readerSettings);

                XmlWriter writer = null;
                int fileNum = 1;
                int entryNum = 0;
                bool writerIsOpen = false;
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = true;

                Dictionary<int, XmlNodeItem> higherLevelNodes = new Dictionary<int, XmlNodeItem>();
                int hlnCount = 0;

                string fileIncrementedName = GetIncrementedFileName(fileName, fileNum);
                resultingFilesList.Add(fileIncrementedName);
                writer = XmlWriter.Create(fileIncrementedName, settings);
                writerIsOpen = true;
                writer.WriteStartDocument();

                int treeDepth = 0;

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:

                            treeDepth++;

                            if (treeDepth == startingLevel)
                            {
                                entryNum++;
                                if (entryNum == 1)
                                {
                                    if (fileNum > 1)
                                    {
                                        fileIncrementedName = GetIncrementedFileName(fileName, fileNum);
                                        resultingFilesList.Add(fileIncrementedName);
                                        writer = XmlWriter.Create(fileIncrementedName, settings);
                                        writerIsOpen = true;
                                        writer.WriteStartDocument();
                                        for (int d = 1; d <= higherLevelNodes.Count; d++)
                                        {
                                            XmlNodeItem xni = higherLevelNodes[d];
                                            switch (xni.XmlNodeType)
                                            {
                                                case XmlNodeType.Element:
                                                    writer.WriteStartElement(xni.NodeValue);
                                                    break;
                                                case XmlNodeType.Text:
                                                    writer.WriteString(xni.NodeValue);
                                                    break;
                                                case XmlNodeType.CDATA:
                                                    writer.WriteCData(xni.NodeValue);
                                                    break;
                                                case XmlNodeType.Comment:
                                                    writer.WriteComment(xni.NodeValue);
                                                    break;
                                                case XmlNodeType.EndElement:
                                                    writer.WriteEndElement();
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (writerIsOpen)
                            {
                                writer.WriteStartElement(reader.Name);
                            }

                            if (treeDepth < startingLevel)
                            {
                                hlnCount++;
                                XmlNodeItem xni = new XmlNodeItem();
                                xni.XmlNodeType = XmlNodeType.Element;
                                xni.NodeValue = reader.Name;
                                higherLevelNodes.Add(hlnCount, xni);
                            }

                            break;
                        case XmlNodeType.Text:

                            if (writerIsOpen)
                            {
                                writer.WriteString(reader.Value);
                            }

                            if (treeDepth < startingLevel)
                            {
                                hlnCount++;
                                XmlNodeItem xni = new XmlNodeItem();
                                xni.XmlNodeType = XmlNodeType.Text;
                                xni.NodeValue = reader.Value;
                                higherLevelNodes.Add(hlnCount, xni);
                            }

                            break;
                        case XmlNodeType.CDATA:

                            if (writerIsOpen)
                            {
                                writer.WriteCData(reader.Value);
                            }

                            if (treeDepth < startingLevel)
                            {
                                hlnCount++;
                                XmlNodeItem xni = new XmlNodeItem();
                                xni.XmlNodeType = XmlNodeType.CDATA;
                                xni.NodeValue = reader.Value;
                                higherLevelNodes.Add(hlnCount, xni);
                            }

                            break;
                        case XmlNodeType.Comment:

                            if (writerIsOpen)
                            {
                                writer.WriteComment(reader.Value);
                            }

                            if (treeDepth < startingLevel)
                            {
                                hlnCount++;
                                XmlNodeItem xni = new XmlNodeItem();
                                xni.XmlNodeType = XmlNodeType.Comment;
                                xni.NodeValue = reader.Value;
                                higherLevelNodes.Add(hlnCount, xni);
                            }

                            break;
                        case XmlNodeType.EndElement:

                            if (entryNum == numEntriesPerFile && treeDepth == startingLevel || treeDepth == 1)
                            {
                                if (writerIsOpen)
                                {
                                    fileNum++;
                                    writer.WriteEndDocument();
                                    writer.Close();
                                    writerIsOpen = false;
                                    entryNum = 0;
                                }
                            }
                            else
                            {
                                if (writerIsOpen)
                                {
                                    writer.WriteEndElement();
                                }

                                if (treeDepth < startingLevel)
                                {
                                    hlnCount++;
                                    XmlNodeItem xni = new XmlNodeItem();
                                    xni.XmlNodeType = XmlNodeType.EndElement;
                                    xni.NodeValue = string.Empty;
                                    higherLevelNodes.Add(hlnCount, xni);
                                }
                            }

                            treeDepth--;

                            break;
                    }
                }

                return resultingFilesList;
            }

            private string GetIncrementedFileName(string fileName, int fileNum)
            {
                return fileName.Replace(".xml", "") + "_" + fileNum + ".xml";
            }
        }
        public class XmlNodeItem
        {
            public XmlNodeType XmlNodeType { get; set; }
            public string NodeValue { get; set; }
        }
        private bool GenerateCsvMerchantStatusClosedOpen(DataTable dt, string sFileName, string FileOutputPath)
        {
            bool isGenerated = false;

            if (FileOutputPath != "")
            {
                
                StreamWriter swr = new StreamWriter(FileOutputPath, false);
                try
                {
                    
                    if (dt.Rows.Count > 0)
                    {

                        int iColCount = dt.Columns.Count;
                        for (int i = 0; i < iColCount; i++)
                        {
                            swr.Write(dt.Columns[i]);
                            if (i < iColCount - 1)
                            {
                                swr.Write(",");
                            }
                        }
                        swr.Write(swr.NewLine);

                       
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (isGenerated == false)
                                isGenerated = true;

                            for (int i = 0; i < iColCount; i++)
                            {
                                if (!Convert.IsDBNull(dr[i]))
                                {
                                    swr.Write(dr[i].ToString());
                                }
                                if (i < iColCount - 1)
                                {
                                    swr.Write(",");
                                }
                            }
                            swr.Write(swr.NewLine);
                        }
                        swr.Close();
                    }
                    // }
                }
                catch (Exception exM)
                {
                   
                    string sMessage =  "Error in CSV :" + exM.Message.ToString() + "\n";
                    ErrorLog(sMessage);
                    isGenerated = false;
                }

            }

            return isGenerated;
        }

        #region to Import the Debit file full region


        #region private void BulkInsertDDMerchantItems(DataTable dtInsertTable, string strTableName, string strFlag)
        private void BulkInsertDDMerchantItems(DataTable dtInsertTable, string strTableName, string strFlag)
        {
            try
            {
                SqlConnection sconn = new SqlConnection(ReadConfigFile("@ConnectionString "));
                sconn.Open();
                SqlBulkCopy oSqlBulkCopy = new SqlBulkCopy(sconn);

                // Copying data to destination

                oSqlBulkCopy.DestinationTableName = strTableName;
                oSqlBulkCopy.WriteToServer(dtInsertTable);

                // Closing connection and the others

                oSqlBulkCopy.Close();

                sconn.Close();
                //if (strTableName == "tblBatchTransaction_DD621New" || strTableName == "tblBatchInfo_DD621New")
                //{
                //    deletenulltransBatchtable();
                //}

                if (strTableName == "tblBatchTransaction_DD621New")
                {
                    deletenulltransBatchtable();
                }
            }
            catch (Exception ex)
            {
                sMessage = sMessage + " Error in Bulk Insert Section : " + strFlag + " The Message : " + ex.Message.ToString() + "\n";
                ErrorLog(sMessage);
                
            }
        }
        #endregion

        #region private void deletenulltransBatchtable()
        private void deletenulltransBatchtable()
        {
            SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure,
                        "spdeletenulltransactionFromDD_621New");
        }
        #endregion

        #region private DataTable GetDDMerchantTable()
        private DataTable GetDDMerchantTable()
        {
            DataTable dtDDMerch = new DataTable();
            DataColumn dc1 = new DataColumn("MerchantNumber");
            dtDDMerch.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("MerchantName");
            dtDDMerch.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("City");
            dtDDMerch.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("State");
            dtDDMerch.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("DeviceID");
            dtDDMerch.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("Gateway");
            dtDDMerch.Columns.Add(dc6);
            DataColumn dc7 = new DataColumn("ETCBatch");
            dtDDMerch.Columns.Add(dc7);
            DataColumn dc8 = new DataColumn("SetllementDate");
            dtDDMerch.Columns.Add(dc8);
            DataColumn dc9 = new DataColumn("dtRecord");
            dtDDMerch.Columns.Add(dc9);
            DataColumn dc10 = new DataColumn("dtImport");
            dtDDMerch.Columns.Add(dc10);
            DataColumn dc11 = new DataColumn("cFileName");
            dtDDMerch.Columns.Add(dc11);

            return dtDDMerch;
        }
        #endregion

        #region private DataTable GetDDBatchDetailTable()
        private DataTable GetDDBatchDetailTable()
        {
            DataTable dtDDBatch = new DataTable();
            DataColumn dc1 = new DataColumn("MerchantNumber");
            dtDDBatch.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("TransDate");
            dtDDBatch.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("TransTime");
            dtDDBatch.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("CardHolderNumber");
            dtDDBatch.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("CardType");
            dtDDBatch.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("TransType");
            dtDDBatch.Columns.Add(dc6);
            DataColumn dc7 = new DataColumn("TransAmount");
            dtDDBatch.Columns.Add(dc7);
            DataColumn dc8 = new DataColumn("AuthNum");
            dtDDBatch.Columns.Add(dc8);
            DataColumn dc9 = new DataColumn("RefNum");
            dtDDBatch.Columns.Add(dc9);
            DataColumn dc10 = new DataColumn("FeeAttribute");
            dtDDBatch.Columns.Add(dc10);
            DataColumn dc11 = new DataColumn("TicketFee");
            dtDDBatch.Columns.Add(dc11);
            DataColumn dc12 = new DataColumn("dtRecord");
            dtDDBatch.Columns.Add(dc12);
            DataColumn dc13 = new DataColumn("dtImport");
            dtDDBatch.Columns.Add(dc13);
            DataColumn dc14 = new DataColumn("cFileName");
            dtDDBatch.Columns.Add(dc14);
            DataColumn dc15 = new DataColumn("CardNumber");
            dtDDBatch.Columns.Add(dc15);
            DataColumn dc16 = new DataColumn("DeviceID");
            dtDDBatch.Columns.Add(dc16);

            return dtDDBatch;
        }
        #endregion

        #region private DataTable GetBatchTransaction()
        private DataTable GetBatchTransaction()
        {
            DataTable dtDDBatch = new DataTable();
            DataColumn dc0 = new DataColumn("Batch_No");
            dtDDBatch.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("MID");
            dtDDBatch.Columns.Add(dc1);
            DataColumn dc16 = new DataColumn("DeviceID");
            dtDDBatch.Columns.Add(dc16);
            DataColumn dc2 = new DataColumn("TransDate");
            dtDDBatch.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("TransTime");
            dtDDBatch.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("CardHolderNumber");
            dtDDBatch.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("CardType");
            dtDDBatch.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("TransType");
            dtDDBatch.Columns.Add(dc6);
            DataColumn dc7 = new DataColumn("TransAmount");
            dtDDBatch.Columns.Add(dc7);
            DataColumn dc8 = new DataColumn("AuthNum");
            dtDDBatch.Columns.Add(dc8);
            DataColumn dc9 = new DataColumn("RefNum");
            dtDDBatch.Columns.Add(dc9);
            DataColumn dc10 = new DataColumn("FeeAttribute");
            dtDDBatch.Columns.Add(dc10);
            DataColumn dc11 = new DataColumn("TicketFee");
            dtDDBatch.Columns.Add(dc11);
            DataColumn dc12 = new DataColumn("dtRecord");
            dtDDBatch.Columns.Add(dc12);
            DataColumn dc13 = new DataColumn("dtImport");
            dtDDBatch.Columns.Add(dc13);
            DataColumn dc14 = new DataColumn("cFileName");
            dtDDBatch.Columns.Add(dc14);
            DataColumn dc15 = new DataColumn("FirstFourCrdNO");
            dtDDBatch.Columns.Add(dc15);
            DataColumn dc17 = new DataColumn("LstFourCrdNo");
            dtDDBatch.Columns.Add(dc17);

            return dtDDBatch;
        }
        #endregion

        #region private DataTable GetDDCardDetailTable()
        private DataTable GetDDCardDetailTable()
        {
            DataTable dtDDCard = new DataTable();
            DataColumn dc1 = new DataColumn("MerchantNumber");
            dtDDCard.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("Gateway");
            dtDDCard.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("CardType");
            dtDDCard.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("SalesCount");
            dtDDCard.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("SalesAmount");
            dtDDCard.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("ReturnCount");
            dtDDCard.Columns.Add(dc6);
            DataColumn dc7 = new DataColumn("ReturnAmount");
            dtDDCard.Columns.Add(dc7);
            DataColumn dc8 = new DataColumn("RevCount");
            dtDDCard.Columns.Add(dc8);
            DataColumn dc9 = new DataColumn("RevAmount");
            dtDDCard.Columns.Add(dc9);
            DataColumn dc10 = new DataColumn("dtRecord");
            dtDDCard.Columns.Add(dc10);
            DataColumn dc11 = new DataColumn("dtImport");
            dtDDCard.Columns.Add(dc11);
            DataColumn dc12 = new DataColumn("cFileName");
            dtDDCard.Columns.Add(dc12);
            DataColumn dc13 = new DataColumn("DeviceID");
            dtDDCard.Columns.Add(dc13);

            return dtDDCard;
        }
        #endregion

        #region private DataTable GetMerchBatchInfo()
        private DataTable GetMerchBatchInfo()
        {
            DataTable dtDDBatchInfo = new DataTable();
            DataColumn dc1 = new DataColumn("Batch_No");
            dtDDBatchInfo.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("Batch_Date");
            dtDDBatchInfo.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("MID");
            dtDDBatchInfo.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("DeviceID");
            dtDDBatchInfo.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("cFileName");
            dtDDBatchInfo.Columns.Add(dc5);

            return dtDDBatchInfo;

        }
        #endregion

        #region getbatchtable
        private DataTable GetDDBatchInfoTable()
        {
            DataTable dtDDBatchInfo = new DataTable();
            DataColumn dc1 = new DataColumn("Batch_No");
            dtDDBatchInfo.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("Header_Amt");
            dtDDBatchInfo.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("MID");
            dtDDBatchInfo.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("Batch_Type");
            dtDDBatchInfo.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("fileName");
            dtDDBatchInfo.Columns.Add(dc5);

            return dtDDBatchInfo;

        }
        #endregion

        #region getbatchtransaction table
        private DataTable GetDDBatchTransactionTable()
        {
            DataTable dtDDBatchTransaction = new DataTable();
            DataColumn dc1 = new DataColumn("Batch_No");
            dtDDBatchTransaction.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("CardHolder_No");
            dtDDBatchTransaction.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("MID");
            dtDDBatchTransaction.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("TRN");
            dtDDBatchTransaction.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("TAmt");
            dtDDBatchTransaction.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("Ref_No");
            dtDDBatchTransaction.Columns.Add(dc6);
            DataColumn dc7 = new DataColumn("TransDate");
            dtDDBatchTransaction.Columns.Add(dc7);
            DataColumn dc8 = new DataColumn("TransID");
            dtDDBatchTransaction.Columns.Add(dc8);
            DataColumn dc9 = new DataColumn("fileName");
            dtDDBatchTransaction.Columns.Add(dc9);

            return dtDDBatchTransaction;


        }
        #endregion

        #region private string GetProcessedValue(string strValue)
        private string GetProcessedValue(string strValue)
        {
            string strResult = strValue.Trim();
            if (strResult != "")
            {
                if (strResult.EndsWith("-"))
                {
                    if (Microsoft.VisualBasic.Information.IsNumeric(strResult.Replace("-", "").Trim()))
                    {
                        strResult = "-" + strResult.Replace("-", "").Trim();
                    }
                }
                else if (strResult.EndsWith("/"))
                {
                    if (Microsoft.VisualBasic.Information.IsNumeric(strResult.Replace("/", "").Trim()))
                    {
                        strResult = strResult.Replace("/", "").Trim();
                    }
                }
                else if (strResult == "")
                    strResult = "0";
            }
            else
            {
                strResult = "0";
            }
            return strResult;
        }
        #endregion

        #region Check for SpecialCharacter
        private bool CheckSpecialChar(StringBuilder strvalue)
        {
            bool flag = false;

            for (int i = 0; i < strvalue.Length; i++)
            {
                if (strvalue[i].ToString() == "." || strvalue[i].ToString() == "~" || strvalue[i].ToString() == "�" || strvalue[i].ToString() == "{" || strvalue[i].ToString() == "" || strvalue[i].ToString() == "'?'" || strvalue[i].ToString() == "?" || strvalue[i].ToString() == "*" || strvalue[i].ToString() == "<" || strvalue[i].ToString() == "%" || strvalue[i].ToString() == "@" || strvalue[i].ToString() == "k")
                {
                    flag = true;
                    break;
                }

            }
            return flag;

        }

        #endregion

       

        //DD Import Code End
        #endregion

        #region GetFileGeneratedate
        private string GetFileGeneratedate(string FileName)
        {
            string strDate = "";
            try
            {
                FileInfo fi = new FileInfo(FileName);
                strDate = fi.LastWriteTime.ToString();
            }
            catch (Exception ex)
            {
                sMessage = sMessage + " FileName = " + FileName.ToString();
                ErrorLog(sMessage);
                sMessage = sMessage + " FileName = " + FileName.ToString();
                ErrorLog(sMessage);
                strDate = System.DateTime.Now.ToShortDateString();
            }
            return strDate;
        }
        #endregion

        #region CheckValidDate
        private Boolean CheckValidDate(string strdate)
        {
            Boolean Flag = false;
            if (strdate.Length == 8)
            {
                string[] strCheck = strdate.Split('/');
                if (strCheck.Length == 3)
                {
                    Flag = true;
                }
            }
            return Flag;

        }
        #endregion

        #region GetMerchantDatatable
        private DataTable GetMerchantDatatable()
        {
            DataTable dtMerch = new DataTable();
            DataColumn dc1 = new DataColumn("MerchantNumber");
            dtMerch.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("BATCHNUMBER");
            dtMerch.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("MERCHANTNAME");
            dtMerch.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("batch_date");
            dtMerch.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("File_Name");
            dtMerch.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("TerminalNo");
            dtMerch.Columns.Add(dc6);
            return dtMerch;


        }
        #endregion

        #region GetMerchantDatatableNew
        private DataTable GetBankCardDetail()
        {
            DataTable dtMerch = new DataTable();
            DataColumn DtlAmount = new DataColumn("DtlAmount");
            dtMerch.Columns.Add(DtlAmount);
            DataColumn sSign = new DataColumn("sSign");
            dtMerch.Columns.Add(sSign);
            DataColumn ProductCode = new DataColumn("ProductCode");
            dtMerch.Columns.Add(ProductCode);
            DataColumn CardNumber = new DataColumn("CardNumber");
            dtMerch.Columns.Add(CardNumber);
            DataColumn TransDate = new DataColumn("TransDate");
            dtMerch.Columns.Add(TransDate);
            DataColumn PostDate = new DataColumn("PostDate");
            dtMerch.Columns.Add(PostDate);
            DataColumn Reference = new DataColumn("Reference");
            dtMerch.Columns.Add(Reference);
            DataColumn BatchNumber = new DataColumn("BatchNumber");
            dtMerch.Columns.Add(BatchNumber);
            DataColumn CurrencyAmt = new DataColumn("CurrencyAmt");
            dtMerch.Columns.Add(CurrencyAmt);
            DataColumn CurrencyRate = new DataColumn("CurrencyRate");
            dtMerch.Columns.Add(CurrencyRate);
            DataColumn AdditionalMERID = new DataColumn("AdditionalMERID");
            dtMerch.Columns.Add(AdditionalMERID);
            DataColumn FoodAmt = new DataColumn("FoodAmt");
            dtMerch.Columns.Add(FoodAmt);
            DataColumn TaxAmount = new DataColumn("TaxAmount");
            dtMerch.Columns.Add(TaxAmount);
            DataColumn TipAmount = new DataColumn("TipAmount");
            dtMerch.Columns.Add(TipAmount);
            DataColumn ProvCardNumber = new DataColumn("ProvCardNumber");
            dtMerch.Columns.Add(ProvCardNumber);
            DataColumn MagSwipeInd = new DataColumn("MagSwipeInd");
            dtMerch.Columns.Add(MagSwipeInd);
            DataColumn RejectINd = new DataColumn("RejectINd");
            dtMerch.Columns.Add(RejectINd);
            DataColumn InvoiceNumber = new DataColumn("InvoiceNumber");
            dtMerch.Columns.Add(InvoiceNumber);
            DataColumn sFileType = new DataColumn("sFileType");
            dtMerch.Columns.Add(sFileType);
            DataColumn sFileName = new DataColumn("sFileName");
            dtMerch.Columns.Add(sFileName);

            return dtMerch;


        }


        #region GetMerchantDatatableNew
        private DataTable GetBankCardHeader()
        {
            DataTable dtMerchHead = new DataTable();
            DataColumn MerchantName = new DataColumn("MerchantName");
            dtMerchHead.Columns.Add(MerchantName);
            DataColumn TransDate = new DataColumn("TransDate");
            dtMerchHead.Columns.Add(TransDate);
            DataColumn Amount = new DataColumn("Amount");
            dtMerchHead.Columns.Add(Amount);
            DataColumn Pcode = new DataColumn("Pcode");
            dtMerchHead.Columns.Add(Pcode);
            DataColumn sSign = new DataColumn("sSign");
            dtMerchHead.Columns.Add(sSign);
            DataColumn Currency = new DataColumn("Currency");
            dtMerchHead.Columns.Add(Currency);
            DataColumn TransCount = new DataColumn("TransCount");
            dtMerchHead.Columns.Add(TransCount);
            DataColumn sFileType = new DataColumn("sFileType");
            dtMerchHead.Columns.Add(sFileType);
            DataColumn sFileName = new DataColumn("sFileName");
            dtMerchHead.Columns.Add(sFileName);
            return dtMerchHead;
        }

        private DataTable GetBankCardCtypeDetail()
        {
            DataTable dtMerchCtype = new DataTable();

            DataColumn Amount = new DataColumn("Amount");
            dtMerchCtype.Columns.Add(Amount);
            DataColumn Sign = new DataColumn("Sign");
            dtMerchCtype.Columns.Add(Sign);
            DataColumn PCode = new DataColumn("PCode");
            dtMerchCtype.Columns.Add(PCode);
            DataColumn CardNumber = new DataColumn("CardNumber");
            dtMerchCtype.Columns.Add(CardNumber);
            DataColumn Acqref = new DataColumn("Acqref");
            dtMerchCtype.Columns.Add(Acqref);
            DataColumn Intref = new DataColumn("Intref");
            dtMerchCtype.Columns.Add(Intref);
            DataColumn Saledate = new DataColumn("Saledate");
            dtMerchCtype.Columns.Add(Saledate);
            DataColumn TranDate = new DataColumn("TranDate");
            dtMerchCtype.Columns.Add(TranDate);
            DataColumn ReasonCode = new DataColumn("ReasonCode");
            dtMerchCtype.Columns.Add(ReasonCode);
            DataColumn ReasonDesc = new DataColumn("ReasonDesc");
            dtMerchCtype.Columns.Add(ReasonDesc);
            DataColumn ResolutionCode = new DataColumn("ResolutionCode");
            dtMerchCtype.Columns.Add(ResolutionCode);
            DataColumn ResolutionCode2 = new DataColumn("ResolutionCode2");
            dtMerchCtype.Columns.Add(ResolutionCode2);
            DataColumn ResolutionCode3 = new DataColumn("ResolutionCode3");
            dtMerchCtype.Columns.Add(ResolutionCode3);
            DataColumn AmDeCode = new DataColumn("AmDeCode");
            dtMerchCtype.Columns.Add(AmDeCode);
            DataColumn sFileType = new DataColumn("sFileType");
            dtMerchCtype.Columns.Add(sFileType);
            DataColumn sFileName = new DataColumn("sFileName");
            dtMerchCtype.Columns.Add(sFileName);
            return dtMerchCtype;


        }

        private DataTable GetBankCardTrailer()
        {
            DataTable dtTrailer = new DataTable();

            DataColumn D1Count = new DataColumn("D1Count");
            dtTrailer.Columns.Add(D1Count);

            DataColumn D1Amount = new DataColumn("D1Amount");
            dtTrailer.Columns.Add(D1Amount);

            DataColumn D1Sign = new DataColumn("D1Sign");
            dtTrailer.Columns.Add(D1Sign);

            DataColumn D2Count = new DataColumn("D2Count");
            dtTrailer.Columns.Add(D2Count);

            DataColumn D2Amount = new DataColumn("D2Amount");
            dtTrailer.Columns.Add(D2Amount);

            DataColumn D2Sign = new DataColumn("D2Sign");
            dtTrailer.Columns.Add(D2Sign);

            DataColumn D3Count = new DataColumn("D3Count");
            dtTrailer.Columns.Add(D3Count);

            DataColumn D3Amount = new DataColumn("D3Amount");
            dtTrailer.Columns.Add(D3Amount);

            DataColumn D3Sign = new DataColumn("D3Sign");
            dtTrailer.Columns.Add(D3Sign);

            DataColumn C1Count = new DataColumn("C1Count");
            dtTrailer.Columns.Add(C1Count);

            DataColumn C1Amount = new DataColumn("C1Amount");
            dtTrailer.Columns.Add(C1Amount);

            DataColumn C1Sign = new DataColumn("C1Sign");
            dtTrailer.Columns.Add(C1Sign);

            DataColumn C2Count = new DataColumn("C2Count");
            dtTrailer.Columns.Add(C2Count);

            DataColumn C2Amount = new DataColumn("C2Amount");
            dtTrailer.Columns.Add(C2Amount);

            DataColumn C2Sign = new DataColumn("C2Sign");
            dtTrailer.Columns.Add(C2Sign);


            DataColumn R1Count = new DataColumn("R1Count");
            dtTrailer.Columns.Add(R1Count);

            DataColumn R1Amount = new DataColumn("R1Amount");
            dtTrailer.Columns.Add(R1Amount);

            DataColumn R1Sign = new DataColumn("R1Sign");
            dtTrailer.Columns.Add(R1Sign);

            DataColumn F1Count = new DataColumn("F1Count");
            dtTrailer.Columns.Add(F1Count);

            DataColumn F1Amount = new DataColumn("F1Amount");
            dtTrailer.Columns.Add(F1Amount);

            DataColumn F1Sign = new DataColumn("F1Sign");
            dtTrailer.Columns.Add(F1Sign);

            DataColumn F2Count = new DataColumn("F2Count");
            dtTrailer.Columns.Add(F2Count);

            DataColumn F2Amount = new DataColumn("F2Amount");
            dtTrailer.Columns.Add(F2Amount);

            DataColumn F2Sign = new DataColumn("F2Sign");
            dtTrailer.Columns.Add(F2Sign);

            DataColumn A1Count = new DataColumn("A1Count");
            dtTrailer.Columns.Add(A1Count);

            DataColumn A1Amount = new DataColumn("A1Amount");
            dtTrailer.Columns.Add(A1Amount);

            DataColumn A1Sign = new DataColumn("A1Sign");
            dtTrailer.Columns.Add(A1Sign);

            DataColumn sFileType = new DataColumn("sFileType");
            dtTrailer.Columns.Add(sFileType);


            DataColumn sFileName = new DataColumn("sFileName");
            dtTrailer.Columns.Add(sFileName);

            return dtTrailer;


        }

        #endregion

        #endregion

        #region GetMerchantDatatableForMd027
        private DataTable GetMerchantDatatableForMd027()
        {
            DataTable dtMerch = new DataTable();
            DataColumn dc1 = new DataColumn("BATCHNUMBER");
            dtMerch.Columns.Add(dc1);
            dtMerch.Columns[0].AutoIncrement = true;
            dtMerch.Columns[0].AutoIncrementStep = 1;
            DataColumn dc2 = new DataColumn("MerchantNumber");
            dtMerch.Columns.Add(dc2);
            DataColumn dc3 = new DataColumn("MERCHANTNAME");
            dtMerch.Columns.Add(dc3);
            DataColumn dc4 = new DataColumn("batch_date");
            dtMerch.Columns.Add(dc4);
            DataColumn dc5 = new DataColumn("File_Name");
            dtMerch.Columns.Add(dc5);
            DataColumn dc6 = new DataColumn("TerminalNo");
            dtMerch.Columns.Add(dc6);
            return dtMerch;


        }
        #endregion

        #region GetTrailerHead and Detail
        private DataTable GetTrailerHead()
        {
            DataTable dtTrailerHead = new DataTable();
            DataColumn MerchantNumber = new DataColumn("MerchantNumber");
            dtTrailerHead.Columns.Add(MerchantNumber);
            DataColumn BATCHNUMBER = new DataColumn("BATCHNUMBER");
            dtTrailerHead.Columns.Add(BATCHNUMBER);
            DataColumn MERCHANTNAME = new DataColumn("MERCHANTNAME");
            dtTrailerHead.Columns.Add(MERCHANTNAME);
            DataColumn batch_date = new DataColumn("batch_date");
            dtTrailerHead.Columns.Add(batch_date);
            DataColumn File_Name = new DataColumn("File_Name");
            dtTrailerHead.Columns.Add(File_Name);
            DataColumn AcceptedCNT = new DataColumn("AcceptedCNT");
            dtTrailerHead.Columns.Add(AcceptedCNT);
            DataColumn AcceptedAMT = new DataColumn("AcceptedAMT");
            dtTrailerHead.Columns.Add(AcceptedAMT);

            DataColumn DebitCNT = new DataColumn("DebitCNT");
            dtTrailerHead.Columns.Add(DebitCNT);
            DataColumn DebitAMT = new DataColumn("DebitAMT");
            dtTrailerHead.Columns.Add(DebitAMT);

            DataColumn RejCNT = new DataColumn("RejCNT");
            dtTrailerHead.Columns.Add(RejCNT);
            DataColumn RejAMT = new DataColumn("RejAMT");
            dtTrailerHead.Columns.Add(RejAMT);

            DataColumn TotCNT = new DataColumn("TotCNT");
            dtTrailerHead.Columns.Add(TotCNT);
            DataColumn TotAMT = new DataColumn("TotAMT");
            dtTrailerHead.Columns.Add(TotAMT);


            return dtTrailerHead;


        }

        private DataTable GetTrailerDetail()
        {
            DataTable dtTrailerDetail = new DataTable();
            DataColumn MerchantNumber = new DataColumn("MerchantNumber");
            dtTrailerDetail.Columns.Add(MerchantNumber);
            DataColumn BATCHNUMBER = new DataColumn("BATCHNUMBER");
            dtTrailerDetail.Columns.Add(BATCHNUMBER);
            DataColumn MERCHANTNAME = new DataColumn("MERCHANTNAME");
            dtTrailerDetail.Columns.Add(MERCHANTNAME);
            DataColumn batch_date = new DataColumn("batch_date");
            dtTrailerDetail.Columns.Add(batch_date);
            DataColumn File_Name = new DataColumn("File_Name");
            dtTrailerDetail.Columns.Add(File_Name);

            DataColumn AccSalesCashCnt = new DataColumn("AccSalesCashCnt");
            dtTrailerDetail.Columns.Add(AccSalesCashCnt);
            DataColumn AccSalesCashAmt = new DataColumn("AccSalesCashAmt");
            dtTrailerDetail.Columns.Add(AccSalesCashAmt);

            DataColumn DebtSalesCashCnt = new DataColumn("DebtSalesCashCnt");
            dtTrailerDetail.Columns.Add(DebtSalesCashCnt);
            DataColumn DebtSalesCashAmt = new DataColumn("DebtSalesCashAmt");
            dtTrailerDetail.Columns.Add(DebtSalesCashAmt);

            DataColumn RejSalesCashCnt = new DataColumn("RejSalesCashCnt");
            dtTrailerDetail.Columns.Add(RejSalesCashCnt);
            DataColumn RejSalesCashAmt = new DataColumn("RejSalesCashAmt");
            dtTrailerDetail.Columns.Add(RejSalesCashAmt);
            DataColumn totcnt = new DataColumn("totcnt");
            dtTrailerDetail.Columns.Add(totcnt);
            DataColumn netItem = new DataColumn("netItem");
            dtTrailerDetail.Columns.Add(netItem);
            DataColumn netdeposit = new DataColumn("netdeposit");
            dtTrailerDetail.Columns.Add(netdeposit);



            return dtTrailerDetail;


        }


        #endregion

        #region GetMerchantTransactionTable
        private DataTable GetMerchantTransactionTable()
        {
            DataTable dtItems = new DataTable();
            DataColumn dci1 = new DataColumn("MerchantNumber");
            dtItems.Columns.Add(dci1);
            DataColumn dci2 = new DataColumn("BATCHNUMBER");
            dtItems.Columns.Add(dci2);
            DataColumn dci3 = new DataColumn("CARDHOLDERNumber");
            dtItems.Columns.Add(dci3);
            DataColumn dci4 = new DataColumn("TransDate");
            dtItems.Columns.Add(dci4);
            DataColumn dci5 = new DataColumn("TransType");
            dtItems.Columns.Add(dci5);
            DataColumn dci6 = new DataColumn("Transamount");
            dtItems.Columns.Add(dci6);

            DataColumn dci7 = new DataColumn("AuthCode");
            dtItems.Columns.Add(dci7);
            DataColumn dci8 = new DataColumn("AuthSource");
            dtItems.Columns.Add(dci8);
            DataColumn dci9 = new DataColumn("txnmode");
            dtItems.Columns.Add(dci9);
            DataColumn dci10 = new DataColumn("TransCode");
            dtItems.Columns.Add(dci10);


            DataColumn dci11 = new DataColumn("CardNumber");
            dtItems.Columns.Add(dci11);

            DataColumn dci12 = new DataColumn("RecordType");
            dtItems.Columns.Add(dci12);

            return dtItems;

        }
        #endregion

        #region BulkInsertMerchants
        private void BulkInsertMerchants(DataTable dtmerchant)
        {
            try
            {
                SqlConnection sconn = new SqlConnection(ReadConfigFile("@ConnectionString "));
                sconn.Open();
                SqlBulkCopy oSqlBulkCopy = new SqlBulkCopy(sconn);

                // Copying data to destination

                oSqlBulkCopy.DestinationTableName = "tblMerchantInfo_CD028_temp";
                oSqlBulkCopy.WriteToServer(dtmerchant);

                // Closing connection and the others

                oSqlBulkCopy.Close();

                sconn.Close();
            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in BulkInsertMerchants Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");
            }



        }

        #endregion

        #region BulkInsertMerchantItems
        private void BulkInsertMerchantItems(DataTable dtMerchItems)
        {
            try
            {
                SqlConnection sconn = new SqlConnection(ReadConfigFile("@ConnectionString "));
                sconn.Open();
                SqlBulkCopy oSqlBulkCopy = new SqlBulkCopy(sconn);

                // Copying data to destination

                oSqlBulkCopy.DestinationTableName = "tblMerchantItemInfo_CD028_temp";
                oSqlBulkCopy.WriteToServer(dtMerchItems);

                // Closing connection and the others

                oSqlBulkCopy.Close();

                sconn.Close();

            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in BulkInsertMerchantItems Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");
            }
        }
        #endregion

        #region DataTransferIntoRealTables
        private void DataTransferIntoRealTables(string strFileType, string strFileName, string strMoveFileName, int nProcessorID, string spName)
        {
            SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure, spName,
            SqlHelper.CreatePerameter("@FileType", SqlDbType.VarChar, strFileType, ParameterDirection.Input, 50, false),
            SqlHelper.CreatePerameter("@FileName", SqlDbType.VarChar, strFileName, ParameterDirection.Input, 200, false),
            SqlHelper.CreatePerameter("@FileDate", SqlDbType.VarChar, GetFileGeneratedate(strMoveFileName), ParameterDirection.Input, 50, false),
             SqlHelper.CreatePerameter("@ProcessorID", SqlDbType.VarChar, nProcessorID, ParameterDirection.Input, 50, false)
            );
            
        }


        public DataSet InsertLogDetails2DB(int nid, string sfilename, string filetype, string paramsent, int agoid, string agoresult, string smsg)
        {
            DataSet ds = new DataSet();
            try
            {
                ds = SqlHelper.ExecuteDataset(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure,
                           "spInsertLogDetails2DB",
                          SqlHelper.CreatePerameter("@id", SqlDbType.Int, nid, ParameterDirection.Input, 8, false),
                          SqlHelper.CreatePerameter("@sfilename", SqlDbType.VarChar, sfilename, ParameterDirection.Input, 500, false),
                          SqlHelper.CreatePerameter("@filetype", SqlDbType.VarChar, filetype, ParameterDirection.Input, 20, false),
                          SqlHelper.CreatePerameter("@paramsent", SqlDbType.VarChar, paramsent, ParameterDirection.Input, 1000, false),
                          SqlHelper.CreatePerameter("@agoid", SqlDbType.Int, agoid, ParameterDirection.Input, 8, false),
                          SqlHelper.CreatePerameter("@agoresult", SqlDbType.VarChar, agoresult, ParameterDirection.Input, 8000, false),
                          SqlHelper.CreatePerameter("@smsg", SqlDbType.VarChar, smsg, ParameterDirection.Input, 8000, false)
                           );

            }
            catch (Exception ex)
            {
                ds = new DataSet();
                ErrorLog("Final Insertion : Filename :" + ""+ "Error in DataTransferIntoRealTables Function : " + ex.Message);

            }
            return ds;

        }

        #endregion

        #region DataTransferIntoMerchantTotals
        private void DataTransferIntoMerchantTotals(string strMoveFileName, string spName)
        {
            SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure, spName,
            SqlHelper.CreatePerameter("@FileDate", SqlDbType.VarChar, GetFileGeneratedate(strMoveFileName), ParameterDirection.Input, 50, false));
        }
        #endregion

        #region DeleteExistingDataFromTempTables
        private void DeleteExistingDataFromTempTables()
        {
            try
            {
                SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure, "spDeleteJetPayTemp");
            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in DeleteExistingDataFromTempTables Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");
            }
        }

        #endregion

     

        string GetDateMMDDYYYY(string str)
        {
            try
            {
                return str.Substring(0, 2) + "/" + str.Substring(2, 2) + "/20" + str.Substring(4, 2);
            }
            catch(Exception ex)
            {
                return str;
            }
        }

        #region GetJulianDate()..........

        private string GetJulianDate(string sDate)
        {
            string strJuilanDate = String.Empty;
            try
            {
                DateTime dt = new DateTime();



                //Set the current year for Julian Date
                string strCurrentYear = "01/01" + "/" + DateTime.Now.Year;
                dt = Convert.ToDateTime(strCurrentYear);

                //Now return the Juilan date
                strJuilanDate = dt.AddDays((Convert.ToInt32(sDate)) - 1).ToString("MM/dd/yyyy");
            }
            catch (Exception ex)
            {
                sMessage = sMessage + " Error in GetJulianDate Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;

            }
            return strJuilanDate;

        }


        /// <summary>
        /// this function changes the date in julian date form.
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        private string GetJulianDate(string sDate, int year, int TranMonth)
        {
            string strJuilanDate = String.Empty;
            try
            {
                DateTime dt = new DateTime();

                if (Convert.ToInt32(sDate) > 350 && TranMonth==1)
                {
                    year = year - 1;
                }

                //Set the current year for Julian Date
                string strCurrentYear = "01/01" + "/" + year;
                dt = Convert.ToDateTime(strCurrentYear);

                //Now return the Juilan date
                strJuilanDate = dt.AddDays((Convert.ToInt32(sDate)) - 1).ToString("MM/dd/yyyy");
            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in GetJulianDate Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;

            }
            return strJuilanDate;

        }

        #endregion

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

        #region FUNCTION private void GenerateErrorFile()
        private void GenerateErrorFile()
        {
            try
            {
                if (sImportTime == "")
                {
                    //                    sImportTime = DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Year.ToString().PadLeft(4, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0');
                    sImportTime = DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Year.ToString().PadLeft(4, '0');
                }

                string ErrorFilePath = ReadConfigFile("@ErrorLogFolder") + "\\" + strFileTypeForTrailer + "_" + sFileName.Split('.')[0] + "_" + sImportTime + ".txt";
                ERRORFILEPATH = ErrorFilePath;

                //if (System.IO.File.Exists(ErrorFilePath))
                //{
                //    System.IO.File.Delete(ErrorFilePath);
                //}

                if (!System.IO.File.Exists(ErrorFilePath))
                {
                    FileStream FS;
                    FS = System.IO.File.Create(ErrorFilePath);
                    FS.Dispose();
                }

                bErrorFileCreateFlag = true;
            }
            catch (Exception ex)
            {


            }

        }
        #endregion

        #region FUNCTION private void ErrorLog(string Ex)
        public void ErrorLog(string Ex)
        {
            try
            {
                if (bErrorFileCreateFlag == false)
                {
                    //Following function will execute one time.
                    GenerateErrorFile();
                }
                GlbERRORFILEPATH = ERRORFILEPATH;
                FileStream errorFS = new FileStream(ERRORFILEPATH, FileMode.Append, FileAccess.Write);
                StreamWriter sr = new StreamWriter(errorFS);

                sr.WriteLine();
                //if (strErrorFolderName.Trim() != strActualFolderName.Trim())
                //{
                //    strErrorFolderName = strActualFolderName.Trim();
                //    sr.Write("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                //    sr.WriteLine();
                //    sr.WriteLine();
                //    sr.Write("File Name : " + strActualFolderName);
                //    sr.WriteLine();
                //    sr.WriteLine();
                //    sr.Write("Move File Name : " + strMoveFolderName);
                //    sr.WriteLine();
                //    sr.WriteLine();
                //    sr.Write("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                //    sr.WriteLine();
                //    sr.WriteLine();
                //}
                sr.Write("-----------------------------" + DateTime.Now + "-------------------------------------------------");
                sr.WriteLine();
                sr.WriteLine();
                sr.Write("Error During Import of file : '" + sFileName + "'     Exception :  " + Ex);
                sr.WriteLine();
                sr.WriteLine();
                sr.Write("------------------------------------------------------------------------------");
                sr.Close();
                errorFS.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region FUNCTION private void GenerateSuccessFile()
        private void GenerateSuccessFile()
        {
            try
            {
                if (sImportTime == "")
                {
                    //sImportTime = DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Year.ToString().PadLeft(4, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0');
                    sImportTime = DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Year.ToString().PadLeft(4, '0');
                }

                string SuccessFilePath = ReadConfigFile("@SuccessLogFolder") + "\\" + strFileTypeForTrailer + "_" + sFileName.Split('.')[0] + "_" + sImportTime + ".txt";
                SUCCESSFILEPATH = SuccessFilePath;

                //if (System.IO.File.Exists(ErrorFilePath))
                //{
                //    System.IO.File.Delete(ErrorFilePath);
                //}

                if (!System.IO.File.Exists(SuccessFilePath))
                {
                    FileStream FS;
                    FS = System.IO.File.Create(SuccessFilePath);
                    FS.Dispose();
                }

                bSuccessFileCreateFlag = true;
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        #region FUNCTION private void SuccessLog(string Mess)
        public  void SuccessLog(string Mess)
        {
            try
            {
                if (bSuccessFileCreateFlag == false)
                {
                    //Following function will execute one time.
                    GenerateSuccessFile();
                }

                FileStream successFS = new FileStream(SUCCESSFILEPATH, FileMode.Append, FileAccess.Write);
                StreamWriter sr = new StreamWriter(successFS);

                sr.WriteLine();

                sr.Write("-----------------------------"+DateTime.Now+"-------------------------------------------------");
                sr.WriteLine();
                sr.WriteLine();
                sr.Write(Mess);
                sr.WriteLine();
                sr.WriteLine();
                sr.Write("------------------------------------------------------------------------------");
                sr.Close();
                successFS.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Function Isnumeric

        //By Manoj
        public bool Isnumeric(String strvalue)
        {
            bool flag = false;
            string strsub = "";
            int count = 0;
            for (int i = 0; i < strvalue.Length; i++)
            {
                strsub = strvalue.Substring(i, 1);
                for (int j = 0; j <= 9; j++)
                {
                    if (strsub == j.ToString())
                    {
                        count++;

                    }

                }

            }
            if (count == strvalue.Length)
            {
                flag = true;
            }

            return flag;

        }
        #endregion

        #region Trailing:- InsertInTrailerTable()

        public void InsertInTrailerTable(string FileType, string filename, string Message, DateTime ProcessStartDateTime, string MoveFileName, ProcessStatus ProStatus, string ColumnName, DateTime? UpdatedValueTime)
        {
            try
            {
                SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure,
                               "spTrailerFileParsed",
                              SqlHelper.CreatePerameter("@FileType", SqlDbType.VarChar, FileType, ParameterDirection.Input, 8000, false),
                              SqlHelper.CreatePerameter("@FileName", SqlDbType.VarChar, filename, ParameterDirection.Input, 8000, false),
                              SqlHelper.CreatePerameter("@Message", SqlDbType.VarChar, Message, ParameterDirection.Input, 8000, false),
                              SqlHelper.CreatePerameter("@ProcessStartDateTime", SqlDbType.DateTime , ProcessStartDateTime , ParameterDirection.Input, 50, false),
                              SqlHelper.CreatePerameter("@MoveFileName", SqlDbType.VarChar, MoveFileName, ParameterDirection.Input, 1000, false),
                              SqlHelper.CreatePerameter("@ProcessStatus", SqlDbType.VarChar, ProStatus.ToString(), ParameterDirection.Input, 50, false),
                              SqlHelper.CreatePerameter("@UpdateColumn", SqlDbType.VarChar, ColumnName, ParameterDirection.Input, 100, false),
                              SqlHelper.CreatePerameter("@UpdateValue", SqlDbType.DateTime, UpdatedValueTime, ParameterDirection.Input, 50, false),
                              SqlHelper.CreatePerameter("@UpdateonDate", SqlDbType.DateTime, ProcessStartDateTime, ParameterDirection.Input, 50, false)
                              
                              ) ;
            }
            catch (Exception ex)
            {
                sMessage = sMessage + " Error in CheckIsFileParsed Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");

            }
        }



        #endregion

        #region BulkInsertTrailer BY PRASHANT 20101116
        private void BulkInsertTrailer(DataTable DTS, string tableName)
        {
            try
            {
                SqlConnection sconn = new SqlConnection(ReadConfigFile("@ConnectionString"));
                int BatchCount = 0;
                if(ReadConfigFile("@BatchCount")!=null)
                    if (ReadConfigFile("@BatchCount").Trim() != "")
                        BatchCount = int.Parse(ReadConfigFile("@BatchCount"));
                sconn.Open();
                SqlBulkCopy oSqlBulkCopy = new SqlBulkCopy(sconn);

                // Copying data to destination
                oSqlBulkCopy.BulkCopyTimeout = 0;
                oSqlBulkCopy.BatchSize = BatchCount;
                oSqlBulkCopy.DestinationTableName = tableName;
                oSqlBulkCopy.WriteToServer(DTS);

                // Closing connection and the others

                oSqlBulkCopy.Close();

                sconn.Close();
            }
            catch (Exception ex)
            {
                sMessage = sMessage + " Error in BulkInsertMerchants Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");

            }
        }

        #endregion

        #region DeleteTrailerByFile function delete Existing Records in Temp Tables by Passing Parameter FileName
        private void DeleteTrailerByFile(string FileName)
        {
            try
            {
                SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure,
                    "spDeleteTrailerByFile",
                     SqlHelper.CreatePerameter("@FileName", SqlDbType.VarChar, FileName, ParameterDirection.Input, 8000, false));
            }
            catch (Exception ex)
            {
                sMessage = sMessage +  "Error in DeleteExistingDataFromTempTables Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");
            }
        }
        #endregion


        #region DeleteTrailerByFile function delete Existing Records in Temp Tables by Passing Parameter FileName
        private DataTable GetTableStructure(string TableName)
        {
            DataTable dtstruct = new DataTable();
            try
            {
                dtstruct= SqlHelper.ExecuteDataset(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure,
                    "spGetTableStructure",
                     SqlHelper.CreatePerameter("@TableName", SqlDbType.VarChar, TableName, ParameterDirection.Input, 100, false)).Tables[0];
            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in DeleteExistingDataFromTempTables Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;
                //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");
            }
           
                return dtstruct;
           
        }
        #endregion

        //#region GetMIDInfoLLC 
        //private DataTable GetMIDInfoLLC(int ProcessorID, string spName, int nYear, int nMonth)
        //{
        //    DataTable dtstruct = new DataTable();
        //    try
        //    {
        //        dtstruct = SqlHelper.ExecuteDataset(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure,
        //            spName,
        //              SqlHelper.CreatePerameter("@nMonth", SqlDbType.Int, nMonth, ParameterDirection.Input, 4, false),
        //                   SqlHelper.CreatePerameter("@nYear", SqlDbType.Int, nYear, ParameterDirection.Input, 4, false),
        //                    SqlHelper.CreatePerameter("@nProcessorID", SqlDbType.Int, ProcessorID, ParameterDirection.Input, 4, false)

        //             ).Tables[0];
        //    }
        //    catch (Exception ex)
        //    {
        //        sMessage = sMessage + "Error in DeleteExistingDataFromTempTables Function : " + ex.Message + "\n";
        //        ErrorLog(sMessage);
        //        throw ex;
        //        //ErLog.WriteErrorLog(FileName, LastModifiedDateTime, ex.Message, "Aborted");
        //    }

        //    return dtstruct;

        //}
        //#endregion

        #region public DataSet GetMIDInfoLLC(int ProcessorID, string spName, int nYear, int nMonth)
        public DataSet GetMIDInfoLLC(int ProcessorID, string spName, int nYear, int nMonth)
        {
            DataSet ds = new DataSet();
            try
            {
                ds = SqlHelper.ExecuteDataset(SqlHelper.ConnectionString.ToString(),
                            CommandType.StoredProcedure,
                            spName,
                            SqlHelper.CreatePerameter("@nMonth", SqlDbType.Int, nMonth, ParameterDirection.Input, 4, false),
                           SqlHelper.CreatePerameter("@nYear", SqlDbType.Int, nYear, ParameterDirection.Input, 4, false),
                            SqlHelper.CreatePerameter("@nProcessorID", SqlDbType.Int, ProcessorID, ParameterDirection.Input, 4, false)
                            );
            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in DeleteExistingDataFromTempTables Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;

            }

            return ds;
        }
        #endregion

        #region public DataSet GetMIDInfoLLCNew(int ProcessorID, string spName, int nYear, int nMonth)
        public DataSet GetMIDInfoLLCNew(int ProcessorID, string spName, int nYear, int nMonth)
        {
            DataSet ds = new DataSet();
            try
            {
                ds = SqlHelper.ExecuteDataset(SqlHelper.ConnectionString.ToString(),
                            CommandType.StoredProcedure,
                            spName,
                            SqlHelper.CreatePerameter("@nMonth", SqlDbType.Int, nMonth, ParameterDirection.Input, 4, false),
                           SqlHelper.CreatePerameter("@nYear", SqlDbType.Int, nYear, ParameterDirection.Input, 4, false),
                            SqlHelper.CreatePerameter("@nProcessorID", SqlDbType.Int, ProcessorID, ParameterDirection.Input, 4, false)
                            );
            }
            catch (Exception ex)
            {
                sMessage = sMessage + "Error in GetMIDInfoLLCNew Function : " + ex.Message + "\n";
                ErrorLog(sMessage);
                throw ex;

            }

            return ds;
        }
        #endregion

        #region FUNCTION public string ReadTemplate(string ConfigKey)
        public static string ReadTemplate()
        {
            string sFinalValue = String.Empty;
            try
            {
                string path = "";
                path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                path = path.Replace("file:\\", "");
                path = path.Substring(0, path.LastIndexOf("\\"));
                path = path + "\\Config\\MailTemplate.htm";
                string strFilePath = path;

                FileStream fs1 = new FileStream(strFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                StreamReader sReader = new StreamReader(fs1);
                string strVal = "";
                while (sReader.Peek() != -1)
                {
                    strVal = strVal + sReader.ReadLine().Trim();

                }

                sFinalValue = strVal;
                sReader.Close();
                fs1.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Return the string.
            return sFinalValue.Trim();
        }
        #endregion
        public static string ReadTemplate2()
        {
            string sFinalValue = String.Empty;
            try
            {
                string path = "";
                path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                path = path.Replace("file:\\", "");
                path = path.Substring(0, path.LastIndexOf("\\"));
                path = path + "\\Config\\MailTemplate2.htm";
                string strFilePath = path;

                FileStream fs1 = new FileStream(strFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                StreamReader sReader = new StreamReader(fs1);
                string strVal = "";
                while (sReader.Peek() != -1)
                {
                    strVal = strVal + sReader.ReadLine().Trim();

                }

                sFinalValue = strVal;
                sReader.Close();
                fs1.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Return the string.
            return sFinalValue.Trim();
        }
        public bool GetValidateConnection()
        {
            try
            {
                string strSql = "select top 1 1 from sys.objects";
                string TestConnection = SqlHelper.ExecuteScalar(SqlHelper.ConnectionString.ToString(), CommandType.Text, strSql).ToString();
                return true;
            }
            catch 
            {
                return false;
            }
            

        }

        #region Artego 

        /*   id      name                       description
            1420001  InProgress              Import File InProgress Status
            1420002  Partially Completed     Import File Partially Completed Status
            1420003  Completed               Import File Completed Status
            1420004  Failed                  Import File Failed Status
            1420005  Generated Successful    File Generation Status
            1420006  Generation Failed       File Generation Failed Status */

        #region public void SendStepsInfo2Log()
        public int SendStepsInfo2Log_SNS(int companyId, string activityCode, string fileName, string eventType, string message, double fileSizeInKB, string filetypeid, int enginelogid, int agoid)
        {
            Dictionary<string, string> req = null;
            string sflagmsg = "";
            string agomsg = "Success";

            string reqJson = "file_name=" + fileName + "|| event_type=" + eventType + "|| event_time=" + DateTime.Now.ToString() +
                "|| activity_code=" + activityCode + "|| message=" + message + "|| company_id=" + companyId.ToString() + "|| file_size=" + fileSizeInKB.ToString();

            try
            {
                if (ReadConfigFile("@AccessKey") != null && ReadConfigFile("@SecretKey").ToString() != "")
                {
                    Console.WriteLine("Adding add_file_monitoring_audit_log to SNS");
                    string snsTopicArn = ReadConfigFile("@snsTopicArn");

                    #region
                    if (!string.IsNullOrEmpty(snsTopicArn))
                    {
                        sflagmsg = "Calling sns:";

                        req = new Dictionary<string, string>
                        {
                            { "file_name", fileName  },
                            { "event_type", eventType },
                            { "event_time", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")},
                            { "activity_code", activityCode  },
                            { "message", message },
                            { "company_id",companyId.ToString() },
                            { "file_size",fileSizeInKB.ToString() }
                        };

                        reqJson = JsonConvert.SerializeObject(req);

                        var messageAttributes = new Dictionary<string, MessageAttributeValue>
                        {
                            { "company_id", new MessageAttributeValue { DataType = "Number", StringValue = req["company_id"] } }
                        };

                        MessageAttributeValue messageAttribute = new MessageAttributeValue
                        {
                            DataType = "Number",
                            StringValue = companyId.ToString()
                        };

                        var messages = new List<Messages>();
                        var messageId = DateTime.Now.ToString("yyyyMMddHHmmssfffffff") ?? "1";
                        var snsMessage = new Messages
                        {
                            Id = messageId,
                            MessageAttributes = messageAttributes,
                            MessageStructure = "json",
                            Message = Newtonsoft.Json.JsonConvert.SerializeObject(req)
                        };

                        messages.Add(snsMessage);
                        Console.WriteLine("Messages: " + Newtonsoft.Json.JsonConvert.SerializeObject(messages));
                        Console.WriteLine($"Adding fm audit log request to SNS topic {snsTopicArn}");

                        PublishRequest publishRequest = new PublishRequest
                        {
                            TopicArn = snsTopicArn,
                            Message = messages[0].Message,
                            MessageAttributes = new Dictionary<string, MessageAttributeValue>
                            {
                                { "company_id", messageAttribute }
                            }

                        };
                        PublishResponse publishResponse = snsClient.Publish(publishRequest);
                    }
                    else
                    {
                        sflagmsg = "No sns topic defined";
                        Console.WriteLine("Invalid SNS Topic");
                    }
                    #endregion
                }
                else
                {
                    sflagmsg = "Access key blank in config, unable to call s3";
                }
            }
            catch (Exception ex)
            {
                ErrorLog("Error occurred while adding fm_audit_log_sns_topic." +  "Error in SendStepsInfo2Log_SNS Function : " + ex.Message);

                Console.WriteLine($"Error occurred while adding fm_audit_log_sns_topic. Error: {ex}");

                sflagmsg = ex.Message;

                agomsg = "Error in ago insert";
            }
            finally
            {
                DataSet dsLog = new DataSet();

                dsLog = UpdateLogs2EngineDB(enginelogid, fileName, filetypeid, reqJson, agoid, agomsg, sflagmsg);

                if (dsLog.Tables.Count > 0)
                {
                    enginelogid = Convert.ToInt32(dsLog.Tables[0].Rows[0][0].ToString());
                }
            }

            return enginelogid;
        }
        #endregion

        //Insert Data in import_file_log table.
        public long SendImportLog(int companyId, int userId, int fileTypeId, string orgFileName, string newFileName, int status, double fileSizeInKB, int EngineFileId)
        {
            int agoid = 0;
            string agoresult = "Suucess";
            string sflagmessage = "Success";
            string reqJson = "p_company_id=" + companyId.ToString() + "|| p_user_id=" + userId.ToString() + "|| p_file_type_id=" + fileTypeId.ToString() +
                "|| p_original_file_name=" + orgFileName + "|| p_new_file_name=" + newFileName + "|| p_status=" + status.ToString();

            string artegoConnectionString = ReadConfigFile("@ConnectionStringArtego");
            string addImportFileLog = ReadConfigFile("@AddImportFileLog");
            long importFileLogId = 0;
            try
            {
                if (artegoConnectionString != null)
                {
                    using (MySqlConnection connection = new MySqlConnection(artegoConnectionString))
                    {
                        using (MySqlCommand command = new MySqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = addImportFileLog;

                            MySqlParameter[] pms = new MySqlParameter[6];
                            pms[0] = new MySqlParameter("p_company_id", MySqlDbType.Int32) { Value = companyId };
                            pms[1] = new MySqlParameter("p_user_id", MySqlDbType.Int32) { Value = userId };
                            pms[2] = new MySqlParameter("p_file_type_id", MySqlDbType.Int32) { Value = fileTypeId };
                            pms[3] = new MySqlParameter("p_original_file_name", MySqlDbType.VarChar) { Value = orgFileName };
                            pms[4] = new MySqlParameter("p_new_file_name", MySqlDbType.VarChar) { Value = newFileName };
                            pms[5] = new MySqlParameter("p_status", MySqlDbType.Int32) { Value = status };

                            command.Parameters.AddRange(pms);
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                            // reqJson = pms.ToString();
                        }
                        // Fetching Last Inserted ID 
                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM import_file_log WHERE original_file_name ='" + orgFileName + "' ORDER BY id DESC LIMIT 1", connection))
                        {
                            connection.Open();
                            using (MySqlDataReader reader = selectCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    // Access the columns of the current row using the reader                                
                                    importFileLogId = reader.GetInt64(reader.GetOrdinal("id"));
                                }
                            }
                            connection.Close();
                            agoid = Convert.ToInt32(importFileLogId);
                        }
                    }

                }
                else
                {
                    sflagmessage = "Artego connection string not defined";
                }
                //return importFileLogId;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error calling Artego stored procedure: " + ex.Message);
                ErrorLog("Error calling Artego stored procedure: Filename :" + addImportFileLog + "." +  "Error in SendImportLog Function : "+ ex.Message);
                sflagmessage = ex.Message;
                agoresult = "";
                int failedStatus = int.Parse(ReadConfigFile("@Failed"));
                SendStepsInfo2Log_SNS(companyId, "107", orgFileName, "3520001", "Undefined errors -> ERROR: " + ex.Message, fileSizeInKB, fileTypeId.ToString(), EngineFileId, agoid);
                UpdateErrorLogById(importFileLogId, failedStatus, "Undefined errors", 0, 0, orgFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);
                return importFileLogId;
            }
            finally
            {
                UpdateLogs2EngineDB(EngineFileId, orgFileName, fileTypeId.ToString(), reqJson, Convert.ToInt32(importFileLogId), agoresult, sflagmessage);
            }
            return importFileLogId;
        }

        // Update log as per the fileTypeId ID in import_file_log table.
        public void UpdateImportLogById(long importFileLogId, int status, string message, int totalcount, int successcount, string strFileName, int companyId, int userId, int fileTypeId, double fileSizeInKB, int EngineFileId)
        {
            string artegoConnectionString = ReadConfigFile("@ConnectionStringArtego");
            string updateImportFileLog = ReadConfigFile("@UpdateImportFileLog");
            string agoresult = "Suucess";
            string sflagmessage = "Success";
            string reqJson = "p_import_file_log_id=" + importFileLogId.ToString() + "|| p_message=" + message.ToString() + "|| p_totalcount=" + totalcount.ToString() +
                "|| p_successcount=" + successcount + "|| p_status=" + status.ToString();
            try
            {
                if (artegoConnectionString != null)
                {
                    using (MySqlConnection connection = new MySqlConnection(artegoConnectionString))
                    {
                        using (MySqlCommand command = new MySqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = updateImportFileLog;

                            MySqlParameter[] pms = new MySqlParameter[5];
                            pms[0] = new MySqlParameter("p_import_file_log_id", MySqlDbType.Int32) { Value = importFileLogId };
                            pms[1] = new MySqlParameter("p_status", MySqlDbType.Int32) { Value = status };
                            pms[2] = new MySqlParameter("p_message", MySqlDbType.VarChar) { Value = message };
                            pms[3] = new MySqlParameter("p_totalcount", MySqlDbType.Int32) { Value = totalcount };
                            pms[4] = new MySqlParameter("p_successcount", MySqlDbType.Int32) { Value = successcount };

                            command.Parameters.AddRange(pms);
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                            // reqJson = pms.ToString();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error calling Artego stored procedure: " + ex.Message);
                ErrorLog("Error calling Artego stored procedure: Status :" + status + "." + "Error in UpdateImportLogById Function : " + ex.Message);
                sflagmessage = ex.Message;
                int failedStatus = int.Parse(ReadConfigFile("@Failed"));
                SendStepsInfo2Log_SNS(companyId, "107", strFileName, status.ToString(), "Undefined errors -> ERROR: " + ex.Message, fileSizeInKB, fileTypeId.ToString(), EngineFileId, Convert.ToInt32(importFileLogId));
                UpdateErrorLogById(importFileLogId, failedStatus, "Undefined errors", 0, 0, strFileName, companyId, userId, fileTypeId, fileSizeInKB, EngineFileId);

            }
            finally
            {
                UpdateLogs2EngineDB(EngineFileId, strFileName, fileTypeId.ToString(), reqJson, Convert.ToInt32(importFileLogId), agoresult, sflagmessage);
            }
        }

        public void UpdateErrorLogById(long importFileLogId, int status, string message, int totalcount, int successcount, string strFileName, int companyId, int userId, int fileTypeId, double fileSizeInKB, int EngineFileId)
        {
            string artegoConnectionString = ReadConfigFile("@ConnectionStringArtego");
            string updateImportFileLog = ReadConfigFile("@UpdateImportFileLog");
            string agoresult = "Suucess";
            string sflagmessage = "Success";
            string reqJson = "p_import_file_log_id=" + importFileLogId.ToString() + "|| p_message=" + message.ToString() + "|| p_totalcount=" + totalcount.ToString() +
                "|| p_successcount=" + successcount + "|| p_status=" + status.ToString();
            try
            {
                if (artegoConnectionString != null)
                {
                    using (MySqlConnection connection = new MySqlConnection(artegoConnectionString))
                    {
                        using (MySqlCommand command = new MySqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = updateImportFileLog;

                            MySqlParameter[] pms = new MySqlParameter[5];
                            pms[0] = new MySqlParameter("p_import_file_log_id", MySqlDbType.Int32) { Value = importFileLogId };
                            pms[1] = new MySqlParameter("p_status", MySqlDbType.Int32) { Value = status };
                            pms[2] = new MySqlParameter("p_message", MySqlDbType.VarChar) { Value = message };
                            pms[3] = new MySqlParameter("p_totalcount", MySqlDbType.Int32) { Value = totalcount };
                            pms[4] = new MySqlParameter("p_successcount", MySqlDbType.Int32) { Value = successcount };

                            command.Parameters.AddRange(pms);
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                            //reqJson = pms.ToString();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error calling Artego stored procedure: " + ex.Message);
                ErrorLog("Error calling Artego stored procedure: Status :" + status+"." + "Error in UpdateErrorLogById Function : " + ex.Message);
            }
            finally
            {
                UpdateLogs2EngineDB(EngineFileId, strFileName, fileTypeId.ToString(), reqJson, Convert.ToInt32(importFileLogId), agoresult, sflagmessage);
            }
        }

        public DataSet UpdateLogs2EngineDB(int id, string sfilename, string filetype, string paramsent, int agoid, string agoresult, string smsg)
        {
          //  CommonFunctions objcomm = new CommonFunctions();
            DataSet dsLog = new DataSet();

            dsLog = InsertLogDetails2DB(id, sfilename, filetype, paramsent, agoid, agoresult, smsg);

            return dsLog;
        }

        #endregion
    }

    public enum ProcessStatus
    {
        Success=1,
        Processing=2,
        Aborted=3,
        Error=4 
    }
}
