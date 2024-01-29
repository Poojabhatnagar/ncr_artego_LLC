using System;
using System.Security.Cryptography;
using System.Text;
using System.Security;

    /// <summary>
    /// Summary description for GenerateOLAParameter
    /// </summary>
    public class GenerateOLAParameter
    {
        #region Key Variables
        public static string strKey;

        private static byte[] bytKey, bytIV;
        #endregion

        #region Encrypt Data
        public static byte[] EncryptData(string strMerchantAppCode, string strUserAppCode)
        {
            InitKey();

            TripleDESCryptoServiceProvider des;
            byte[] bytBuff;
            byte[] bytEncrypted = ASCIIEncoding.ASCII.GetBytes("");
            string strOriginal = strMerchantAppCode + "@@@" + DateTime.Now.Ticks.ToString() + "@@@" + strUserAppCode;

            try
            {
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
                bytEncrypted = 
                    des.CreateEncryptor(bytKey, bytIV).TransformFinalBlock(bytBuff, 0, bytBuff.Length);


                //cleanup
                des = null;
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message.ToString());   
            }
            return bytEncrypted;

        }

        public static byte[] EncryptData(string strMyData)
        {
            InitKey();

            TripleDESCryptoServiceProvider des;
            byte[] bytBuff;
            byte[] bytEncrypted = ASCIIEncoding.ASCII.GetBytes("");
            string strOriginal = strMyData;
            // strMerchantAppCode + "@@@" + DateTime.Now.Ticks.ToString() + "@@@" + strUserAppCode;

            try
            {
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
                bytEncrypted =
                    des.CreateEncryptor(bytKey, bytIV).TransformFinalBlock(bytBuff, 0, bytBuff.Length);


                //cleanup
                des = null;
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message.ToString());   
            }
            return bytEncrypted;

        }


        #endregion

        #region Decrypt Data
        public static string DecryptData(byte[] bytEncrypted)
        {
            InitKey();

            TripleDESCryptoServiceProvider des;
            string strDecrypted = "";

            try
            {
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

                //decrypt DES 3 encrypted byte buffer and return ASCII string
                    strDecrypted = ASCIIEncoding.ASCII.GetString(
                        des.CreateDecryptor(bytKey, bytIV).TransformFinalBlock(bytEncrypted, 0, bytEncrypted.Length)
                        );
                
                des = null;
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message.ToString());
            }

            return strDecrypted;
        }
        #endregion

        #region FUNCTION private static bool InitKey()
        private static bool InitKey()
        {
            try
            {
                //generate an MD5 hash for tke Key.
                //a hash is a one way encryption meaning once you generate
                //the hash, you cant derive the password back from it.

                int i;
                Byte[] m_Key = new Byte[16];
                Byte[] m_IV = new Byte[16];

                // Convert Key to byte array
                byte[] bp = new byte[strKey.Length];

                //Hash the key using MD5
                MD5CryptoServiceProvider objMD5 = new MD5CryptoServiceProvider();

                byte[] bpHash = objMD5.ComputeHash(bp);

                for (i = 0; i < 8; i++)
                    m_Key[i] = bpHash[i];

                for (i = 8; i < 16; i++)
                    m_IV[i - 8] = bpHash[i];


                bytKey = m_Key;
                bytIV = m_IV;

                return true;
            }
            catch (Exception)
            {
                //Error Performing Operations
                return false;
            }
        }
        #endregion
    }
