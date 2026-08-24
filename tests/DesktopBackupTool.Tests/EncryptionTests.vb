Imports System.IO
Imports System.Text
Imports System.Threading
Imports DesktopBackupTool.Infrastructure.Encryption
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class EncryptionTests
        <TestMethod>
        Public Async Function EncryptAndDecryptFile_ValidPassword_RoundtripsSuccessfully() As Task
            Dim service As New AesEncryptionService()
            Dim plainFile = Path.GetTempFileName()
            Dim encFile = Path.Combine(Path.GetTempPath(), $"TestEnc_{Guid.NewGuid():N}.enc")
            Dim decFile = Path.Combine(Path.GetTempPath(), $"TestDec_{Guid.NewGuid():N}.txt")
            Dim originalText = "Production grade AES-256 encrypted payload content."
            Dim password = "SuperSecretPassword123!"

            Try
                Await File.WriteAllTextAsync(plainFile, originalText, Encoding.UTF8)

                Await service.EncryptFileAsync(plainFile, encFile, password, CancellationToken.None)
                Assert.IsTrue(File.Exists(encFile))
                Assert.AreNotEqual(0, New FileInfo(encFile).Length)

                Await service.DecryptFileAsync(encFile, decFile, password, CancellationToken.None)
                Assert.IsTrue(File.Exists(decFile))

                Dim decryptedText = Await File.ReadAllTextAsync(decFile, Encoding.UTF8)
                Assert.AreEqual(originalText, decryptedText)
            Finally
                If File.Exists(plainFile) Then File.Delete(plainFile)
                If File.Exists(encFile) Then File.Delete(encFile)
                If File.Exists(decFile) Then File.Delete(decFile)
            End Try
        End Function

        <TestMethod>
        Public Async Function DecryptFile_WrongPassword_ThrowsUnauthorizedAccessException() As Task
            Dim service As New AesEncryptionService()
            Dim plainFile = Path.GetTempFileName()
            Dim encFile = Path.Combine(Path.GetTempPath(), $"TestEnc_{Guid.NewGuid():N}.enc")
            Dim decFile = Path.Combine(Path.GetTempPath(), $"TestDec_{Guid.NewGuid():N}.txt")

            Try
                Await File.WriteAllTextAsync(plainFile, "Secret data", Encoding.UTF8)
                Await service.EncryptFileAsync(plainFile, encFile, "CorrectPassword123", CancellationToken.None)

                Dim exceptionThrown = False
                Try
                    Await service.DecryptFileAsync(encFile, decFile, "WrongPassword456", CancellationToken.None)
                Catch ex As UnauthorizedAccessException
                    exceptionThrown = True
                End Try

                Assert.IsTrue(exceptionThrown)
            Finally
                If File.Exists(plainFile) Then File.Delete(plainFile)
                If File.Exists(encFile) Then File.Delete(encFile)
                If File.Exists(decFile) Then File.Delete(decFile)
            End Try
        End Function

        <TestMethod>
        Public Sub PasswordHashingAndVerification_ValidatesCorrectly()
            Dim service As New AesEncryptionService()
            Dim salt = service.GenerateSalt()
            Dim password = "MySecurePassword"

            Dim hash = service.HashPassword(password, salt)
            Assert.IsTrue(service.VerifyPassword(password, salt, hash))
            Assert.IsFalse(service.VerifyPassword("WrongPassword", salt, hash))
        End Sub
    End Class
End Namespace
