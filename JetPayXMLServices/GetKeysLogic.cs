using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace JetXmlService
{
    public class GetKeysLogic
    {
        #region AddKeyFormation
        /* Key Format Methods*/

        

       
        public static string GetOriginalKey(DataSet ds)
        {
            //Assigning Variables 
            string CustKey1 = "", CustKey2 = "", CustKey3 = "", CustFullKey = "";

            // Getting existing Key from database 
            if (ds.Tables.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    CustKey1 = ds.Tables[0].Rows[0]["CustValueNew"].ToString();
                    CustKey2 = ds.Tables[0].Rows[1]["CustValueNew"].ToString();
                    CustKey3 = ds.Tables[0].Rows[2]["CustValueNew"].ToString();


                    //Append three char in Keys New  random value 2 to 4 indexing
                    CustKey1 = GetFirstChar(CustKey1) + GetLeaveFourtChar(CustKey1);
                    CustKey2 = GetFirstChar(CustKey2) + GetLeaveFourtChar(CustKey2);
                    CustKey3 = GetFirstChar(CustKey3) + GetLeaveFourtChar(CustKey3);
                    CustFullKey = CustKey1 + CustKey2 + CustKey3;
                   
                }
            }
            return CustFullKey;

        }

        private static string GetFirstChar(string keys)
        {
            return keys.Substring(0, 1);

        }
        private static string GetLeaveFourtChar(string keys)
        {
            return keys.Substring(4);
        }
        

       



        #endregion
    }

    
}
