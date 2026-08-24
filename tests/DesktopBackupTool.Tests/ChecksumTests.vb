Imports System.IO
Imports System.Text
Imports DesktopBackupTool.Infrastructure.FileSystem
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class ChecksumTests
        <TestMethod>
        Public Async Function ComputeSha256Async_ValidFile_ReturnsExpectedHash() As Task
            Dim service As New Sha256ChecksumService()
            Dim tempFile = Path.GetTempFileName()

            Try
                Dim utf8NoBom = New UTF8Encoding(False)
                Dim content = "Hello World Antigravity Desktop Backup Tool"
                Await File.WriteAllTextAsync(tempFile, content, utf8NoBom)

                Dim hash = Await service.ComputeSha256Async(tempFile)

                Assert.IsFalse(String.IsNullOrEmpty(hash))
                Assert.AreEqual(64, hash.Length)

                Using sha = System.Security.Cryptography.SHA256.Create()
                    Dim expectedBytes = sha.ComputeHash(utf8NoBom.GetBytes(content))
                    Dim expectedHash = BitConverter.ToString(expectedBytes).Replace("-", "").ToLowerInvariant()
                    Assert.AreEqual(expectedHash, hash)
                End Using
            Finally
                If File.Exists(tempFile) Then
                    File.Delete(tempFile)
                End If
            End Try
        End Function

        <TestMethod>
        Public Sub ComputeSha256_Stream_ReturnsMatchingHash()
            Dim service As New Sha256ChecksumService()
            Dim bytes = Encoding.UTF8.GetBytes("Test Stream Content for Checksum")

            Using ms As New MemoryStream(bytes)
                Dim hash = service.ComputeSha256(ms)
                Assert.IsFalse(String.IsNullOrEmpty(hash))
                Assert.AreEqual(64, hash.Length)
            End Using
        End Sub
    End Class
End Namespace
