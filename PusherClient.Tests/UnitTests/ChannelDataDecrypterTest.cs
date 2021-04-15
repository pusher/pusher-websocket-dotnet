using NUnit.Framework;
using PusherClient.Tests.AcceptanceTests;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class ChannelDataDecrypterTest
    {
        private static readonly byte[] EncryptionMasterKey = EventEmitterTest.GenerateEncryptionMasterKey();

        [Test]
        public void ChannelDataDecrypterWillRaiseErrorWhenEncryptedDataIsNull()
        {
            // Arrange
            ChannelDecryptionException error = null;
            ChannelDataDecrypter decrypter = new ChannelDataDecrypter();
            string keyText = System.Convert.ToBase64String(EncryptionMasterKey);
            Assert.IsNotNull(keyText);

            // Act
            try
            {
                decrypter.DecryptData(EncryptionMasterKey, null);
            }
            catch(ChannelDecryptionException exception)
            {
                error = exception;
            }

            // Assert
            Assert.IsNotNull(error, $"Expected a {nameof(ChannelDecryptionException)} exception");
        }

        [Test]
        public void ChannelDataDecrypterWillRaiseErrorWhenEncryptedDataPropertiesAreNotProvided()
        {
            // Arrange
            ChannelDecryptionException error = null;
            ChannelDataDecrypter decrypter = new ChannelDataDecrypter();
            EncryptedChannelData data = new EncryptedChannelData();

            // Act
            try
            {
                decrypter.DecryptData(EncryptionMasterKey, data);
            }
            catch (ChannelDecryptionException exception)
            {
                error = exception;
            }

            // Assert
            Assert.IsNotNull(error, $"Expected a {nameof(ChannelDecryptionException)} exception");
        }
    }
}
