using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace JetXmlService
{
    public class Parse
    {
        #region Constructor
        public Parse()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        #endregion



        #region private variable decalartion........

        string _sMID, _sBatchDate, _sTransType, _sVolume, _sBATCHID, _sCardNo, _sParsingDate;

        // transaction parameter variable
        string _sCardType, _sAuthNo, _sAuthAmt, _sdtAuth, _sAuthSourceCode, _sPOSEntryMode;

        #endregion

        #region PRIVATE CONSTANT DECLARATION...........
        

        private const string sp_InsertFromDraft256 = "sp_InsertFromDraft256";
        private const string sp_InsertRiskTransactionDraft256 = "sp_InsertRiskTransactionDraft256";



        private const string cBatchVolume = "@cBatchVolume";
        // -----------TRANSACTION DETAIL CONST PARAMETER ------------
        private const string cMID = "@MID";
        private const string BatchDate = "@dtBatch";
        private const string Amount = "@Amount";
        private const string cBatchID = "@cBatchID";
        private const string nBatchID = "@nBatchID";

        private const string cCardNumber = "@cCardNumber";
        private const string cCardType = "@cCardType";
        private const string cTransType = "@cTransType";
        private const string cAuthNo = "@cAuthNo";
        private const string fltAuthAmt = "@fltAuthAmt";
        private const string dtAuth = "@dtAuth";
        private const string cAuthSourceCode = "@cAuthSourceCode";
        private const string cPOSEntryMode = "@cPOSEntryMode";
        private const string dtParsing = "@dtParsing";
        private const string CURRDATE = "@currdate";



        

        #endregion

        #region PROPERTY..........

        public string sParsingDate
        {
            get
            {
                return _sParsingDate;
            }
            set
            {
                _sParsingDate = value;
            }
        }

        public string sMID
        {
            get
            {
                return _sMID;
            }
            set
            {
                _sMID = value;
            }
        }

        public string sBatchDate
        {
            get
            {
                return _sBatchDate;
            }
            set
            {
                _sBatchDate = value;
            }
        }
        public string sTransType
        {
            get
            {
                return _sTransType;
            }
            set
            {
                _sTransType = value;
            }
        }
        public string sVolume
        {
            get
            {
                return _sVolume;
            }
            set
            {
                _sVolume = value;
            }
        }

        public string sBATCHID
        {
            get
            {
                return _sBATCHID;
            }
            set
            {
                _sBATCHID = value;
            }
        }

        public string sCardNo
        {
            get
            {
                return _sCardNo;
            }
            set
            {
                _sCardNo = value;
            }
        }

        //string _sCardType,_sAuthNo,_sAuthAmt,_sdtAuth,_sAuthSourceCode,_sPOSEntryMode;
        public string sCardType
        {
            get
            {
                return _sCardType;
            }
            set
            {
                _sCardType = value;
            }
        }

        public string sAuthNo
        {
            get
            {
                return _sAuthNo;
            }
            set
            {
                _sAuthNo = value;
            }
        }

        public string sAuthAmt
        {
            get
            {
                return _sAuthAmt;
            }
            set
            {
                _sAuthAmt = value;
            }
        }

        public string sdtAuth
        {
            get
            {
                return _sdtAuth;
            }
            set
            {
                _sdtAuth = value;
            }
        }

        public string sAuthSourceCode
        {
            get
            {
                return _sAuthSourceCode;
            }
            set
            {
                _sAuthSourceCode = value;
            }
        }

        public string sPOSEntryMode
        {
            get
            {
                return _sPOSEntryMode;
            }
            set
            {
                _sPOSEntryMode = value;
            }
        }

        #endregion



        public string InsertBatchData()
        {
            string strBatchID;

            // SET UP PARAMETERS (2 INPUT) 
            SqlParameter[] param = new SqlParameter[5];

            param[0] = new SqlParameter(cMID, SqlDbType.VarChar, 25);
            param[0].Value = _sMID;

            param[1] = new SqlParameter(BatchDate, SqlDbType.VarChar, 25);
            param[1].Value = _sBatchDate;

            param[2] = new SqlParameter(cBatchVolume, SqlDbType.VarChar, 25);
            param[2].Value = _sVolume;

            param[3] = new SqlParameter(nBatchID, SqlDbType.VarChar, 25);
            param[3].Direction = ParameterDirection.Output;

            param[4] = new SqlParameter(dtParsing, SqlDbType.VarChar, 10);
            param[4].Value = _sParsingDate;

            SqlHelper.ExecuteNonQuery(SqlHelper.ConnectionString.ToString(), CommandType.StoredProcedure, sp_InsertFromDraft256, param).ToString();

            strBatchID = param[3].Value.ToString();


            return strBatchID;
        }


        public void InsertTransactionDetail()
        {
            SqlHelper.ExecuteDataset(
                SqlHelper.ConnectionString.ToString(),
                CommandType.StoredProcedure,
                sp_InsertRiskTransactionDraft256,
                //SqlHelper.CreatePerameter(nBatchID,SqlDbType.BigInt,_iBatchID,ParameterDirection.Input,8,false),
                SqlHelper.CreatePerameter(cMID, SqlDbType.VarChar, (_sMID == "" ? null : _sMID), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(BatchDate, SqlDbType.VarChar, (_sBatchDate == "" ? null : _sBatchDate), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(Amount, SqlDbType.VarChar, (_sVolume == "" ? null : _sVolume), ParameterDirection.Input, 30, false),

                SqlHelper.CreatePerameter(cBatchID, SqlDbType.VarChar, (_sBATCHID == "" ? null : _sBATCHID), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(cCardNumber, SqlDbType.VarChar, (_sCardNo == "" ? null : _sCardNo), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(cCardType, SqlDbType.VarChar, (_sCardType == "" ? null : _sCardType), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(cTransType, SqlDbType.VarChar, (_sTransType == "" ? null : _sTransType), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(cAuthNo, SqlDbType.VarChar, (_sAuthNo == "" ? null : _sAuthNo), ParameterDirection.Input, 30, false),

                SqlHelper.CreatePerameter(fltAuthAmt, SqlDbType.VarChar, (_sAuthAmt == "" ? null : _sAuthAmt), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(dtAuth, SqlDbType.VarChar, (_sdtAuth == "" ? null : _sdtAuth), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(cAuthSourceCode, SqlDbType.VarChar, (_sAuthSourceCode == "" ? null : _sAuthSourceCode), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(cPOSEntryMode, SqlDbType.VarChar, (_sPOSEntryMode == "" ? null : _sPOSEntryMode), ParameterDirection.Input, 30, false),
                SqlHelper.CreatePerameter(dtParsing, SqlDbType.VarChar, (_sParsingDate == "" ? null : _sParsingDate), ParameterDirection.Input, 10, false)

                );

        }
    }
}
