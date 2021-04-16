using System;
using System.Text;
using NaCl;

namespace PusherClient
{
    internal class ChannelDataDecrypter : IChannelDataDecrypter
    {
        public string DecryptData(byte[] decryptionKey, EncryptedChannelData encryptedData)
        {
            string decryptedText = null;
            byte[] cipher = null;
            byte[] nonce = null;
            if (encryptedData != null)
            {
                if (encryptedData.ciphertext != null)
                {
                    cipher = Convert.FromBase64String(encryptedData.ciphertext);
                }

                if (encryptedData.nonce != null)
                {
                    nonce = Convert.FromBase64String(encryptedData.nonce);
                }
            }

            if (cipher != null && nonce != null)
            {
                using (XSalsa20Poly1305 secretBox = new XSalsa20Poly1305(decryptionKey))
                {
                    byte[] decryptedBytes = new byte[cipher.Length - XSalsa20Poly1305.TagLength];
                    if (secretBox.TryDecrypt(decryptedBytes, cipher, nonce))
                    {
                        decryptedText = Encoding.UTF8.GetString(decryptedBytes);
                    }
                    else
                    {
                        throw new ChannelDecryptionException("Decryption failed for channel.");
                    }
                }
            }
            else
            {
                throw new ChannelDecryptionException("Insufficient data received; requires encrypted data with 'ciphertext' and 'nonce'.");
            }

            return decryptedText;
        }
    }
}
