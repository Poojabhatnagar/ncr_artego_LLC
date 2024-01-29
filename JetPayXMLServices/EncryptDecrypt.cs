using System;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;

namespace JetXmlService
{
    public static class EncryptDecrypt
    {
        #region Private key

        //Key
        private static string strKey = "aAbBcCdDeEfFgGhHiIjJkKlLmMnNoOpPqQrRsStTuUvVwWxXyYzZ";

        //Initialization Vector
        private static string strIV = "vamaAbBcCdDeEfFgGhHiIjJkKlLmMnNoOpPqQrRsStTuUvVwWxXyYzZvam";

        #endregion

        #region "Encrypt Data"
        //'"EncryptPwd Function's Description"
        //'*********************************************************************************            
        //' Function Name				: EncryptPwd
        //' Purpose						: 
        //' Input						: 
        //' Output						: 
        //' Returns						: -
        //' Created By					: 
        //' Created Date				: 
        //'**********************************************************************************
        public  static string EncryptData(string sPassword)
        {
            TripleDESCryptoServiceProvider des;
            MD5CryptoServiceProvider hashmd5, hashmd6;
            byte[] bytKey, bytIV, bytBuff;

            string strEncrypted = "";
            //create a string to encrypt
            string strOriginal = sPassword;

            try
            {
                //generate an MD5 hash for tke Key. 
                //a hash is a one way encryption meaning once you generate
                //the hash, you cant derive the password back from it.
                hashmd5 = new MD5CryptoServiceProvider();
                bytKey = hashmd5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strKey));
                hashmd5 = null;

                //generate an MD5 hash for tke Vector. 
                //a hash is a one way encryption meaning once you generate
                //the hash, you cant derive the password back from it.
                hashmd6 = new MD5CryptoServiceProvider();
                bytIV = hashmd6.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strIV));
                hashmd6 = null;

                //implement DES3 encryption
                des = new TripleDESCryptoServiceProvider();

                //the key is the secret password hash.
                //des.Key = bytPwdhash,bytIV;

                //the mode is the block cipher mode which is basically the
                //details of how the encryption will work. There are several
                //kinds of ciphers available in DES3 and they all have benefits
                //and drawbacks. Here the Electronic Codebook cipher is used
                //which means that a given bit of text is always encrypted
                //exactly the same when the same password is used.
                des.Mode = CipherMode.ECB; //CBC, CFB


                //----- encrypt an un-encrypted string ------------
                //the original string, which needs encrypted, must be in byte
                //array form to work with the des3 class. everything will because
                //most encryption works at the byte level so you'll find that
                //the class takes in byte arrays and returns byte arrays and
                //you'll be converting those arrays to strings.
                bytBuff = ASCIIEncoding.ASCII.GetBytes(strOriginal);

                //encrypt the byte buffer representation of the original string
                //and base64 encode the encrypted string. the reason the encrypted
                //bytes are being base64 encoded as a string is the encryption will
                //have created some weird characters in there. Base64 encoding
                //provides a platform independent view of the encrypted string 
                //and can be sent as a plain text string to wherever.
                strEncrypted = Convert.ToBase64String(
                    des.CreateEncryptor(bytKey, bytIV).TransformFinalBlock(bytBuff, 0, bytBuff.Length)
                    );

                //cleanup
                des = null;
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message.ToString());   
            }
            return strEncrypted;

        }
        #endregion

        #region "Decrypt Data"
        //'"DecryptPwd Function's Description"
        //'*********************************************************************************            
        //' Function Name				: DecryptPwd
        //' Purpose						: 
        //' Input						: 
        //' Output						: 
        //' Returns						: -
        //' Created By					: 
        //' Created Date				: 
        //'**********************************************************************************
        public static string DecryptData(string strEncrypted)
        {
            TripleDESCryptoServiceProvider des;
            MD5CryptoServiceProvider hashmd5, hashmd6;
            byte[] bytKey, bytIV, bytBuff;
            string strDecrypted = "";
            try
            {
                //generate an MD5 hash for the key. 
                //a hash is a one way encryption meaning once you generate
                //the hash, you cant derive the password back from it.
                hashmd5 = new MD5CryptoServiceProvider();
                bytKey = hashmd5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strKey));
                hashmd5 = null;

                //generate an MD5 hash for tke Vector. 
                //a hash is a one way encryption meaning once you generate
                //the hash, you cant derive the password back from it.
                hashmd6 = new MD5CryptoServiceProvider();
                bytIV = hashmd6.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strIV));
                hashmd6 = null;

                //implement DES3 encryption
                des = new TripleDESCryptoServiceProvider();

                //the key is the secret password hash.
                //des.Key = bytPwdhash;

                //the mode is the block cipher mode which is basically the
                //details of how the encryption will work. There are several
                //kinds of ciphers available in DES3 and they all have benefits
                //and drawbacks. Here the Electronic Codebook cipher is used
                //which means that a given bit of text is always encrypted
                //exactly the same when the same password is used.
                des.Mode = CipherMode.ECB; //CBC, CFB

                //----- decrypt an encrypted string ------------
                //whenever you decrypt a string, you must do everything you
                //did to encrypt the string, but in reverse order. To encrypt,
                //first a normal string was des3 encrypted into a byte array
                //and then base64 encoded for reliable transmission. So, to 
                //decrypt this string, first the base64 encoded string must be 
                //decoded so that just the encrypted byte array remains.
                if (strEncrypted.Trim() != "")
                {
                    bytBuff = Convert.FromBase64String(strEncrypted.Trim());

                    //decrypt DES 3 encrypted byte buffer and return ASCII string
                    strDecrypted = ASCIIEncoding.ASCII.GetString(
                        des.CreateDecryptor(bytKey, bytIV).TransformFinalBlock(bytBuff, 0, bytBuff.Length)
                        );
                    //cleanup
                }
                des = null;
            }
            catch (Exception ex)
            {
                string str;
                str = ex.Message;
                str = ex.Message;
                str = ex.Message;
            }


            return strDecrypted;
        }
        #endregion

    }
}
