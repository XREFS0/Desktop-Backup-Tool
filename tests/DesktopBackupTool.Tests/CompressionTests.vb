Imports System.IO
Imports System.Threading
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Infrastructure.Compression
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class CompressionTests
        <TestMethod>
        Public Async Function CompressAndExtractDirectory_PreservesStructureAndContent() As Task
            Dim service As New ZipCompressionService()
            Dim sourceDir = Path.Combine(Path.GetTempPath(), $"ZipSrc_{Guid.NewGuid():N}")
            Dim targetDir = Path.Combine(Path.GetTempPath(), $"ZipTgt_{Guid.NewGuid():N}")
            Dim zipFile = Path.Combine(Path.GetTempPath(), $"ZipArchive_{Guid.NewGuid():N}.zip")

            Try
                Directory.CreateDirectory(sourceDir)
                Directory.CreateDirectory(Path.Combine(sourceDir, "SubFolder"))
                File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "Content 1")
                File.WriteAllText(Path.Combine(sourceDir, "SubFolder", "file2.txt"), "Content 2 in subfolder")

                Await service.CompressDirectoryAsync(sourceDir, zipFile, CompressionLevelOption.Fast, Nothing, CancellationToken.None)
                Assert.IsTrue(File.Exists(zipFile))

                Dim filesInArchive = service.ListArchiveFiles(zipFile)
                Assert.AreEqual(2, filesInArchive.Count)

                Await service.ExtractArchiveAsync(zipFile, targetDir, Nothing, OverwriteMode.Overwrite, Nothing, CancellationToken.None)
                Assert.IsTrue(File.Exists(Path.Combine(targetDir, "file1.txt")))
                Assert.IsTrue(File.Exists(Path.Combine(targetDir, "SubFolder", "file2.txt")))
                Assert.AreEqual("Content 1", File.ReadAllText(Path.Combine(targetDir, "file1.txt")))
                Assert.AreEqual("Content 2 in subfolder", File.ReadAllText(Path.Combine(targetDir, "SubFolder", "file2.txt")))
            Finally
                If Directory.Exists(sourceDir) Then Directory.Delete(sourceDir, True)
                If Directory.Exists(targetDir) Then Directory.Delete(targetDir, True)
                If File.Exists(zipFile) Then File.Delete(zipFile)
            End Try
        End Function
    End Class
End Namespace
