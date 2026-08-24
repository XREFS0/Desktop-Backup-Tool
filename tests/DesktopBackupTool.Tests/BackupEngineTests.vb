Imports System.IO
Imports System.Threading
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Infrastructure.Backup
Imports DesktopBackupTool.Infrastructure.Compression
Imports DesktopBackupTool.Infrastructure.Database
Imports DesktopBackupTool.Infrastructure.Encryption
Imports DesktopBackupTool.Infrastructure.FileSystem
Imports DesktopBackupTool.Infrastructure.Logging
Imports DesktopBackupTool.Infrastructure.Repositories
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class BackupEngineTests
        Private _tempRoot As String
        Private _sourceDir As String
        Private _destDir As String
        Private _dbPath As String

        Private _context As DatabaseContext
        Private _jobRepo As BackupJobRepository
        Private _runRepo As BackupRunRepository
        Private _fileRepo As BackupFileRepository
        Private _logRepo As LogRepository
        Private _checksumService As Sha256ChecksumService
        Private _compressionService As ZipCompressionService
        Private _encryptionService As AesEncryptionService
        Private _logService As LogService
        Private _retentionService As RetentionService
        Private _engine As BackupEngine
        Private _verificationService As VerificationService

        <TestInitialize>
        Public Async Function SetupAsync() As Task
            _tempRoot = Path.Combine(Path.GetTempPath(), $"EngineTest_{Guid.NewGuid():N}")
            _sourceDir = Path.Combine(_tempRoot, "Source")
            _destDir = Path.Combine(_tempRoot, "Dest")
            _dbPath = Path.Combine(_tempRoot, "test_engine.db")

            Directory.CreateDirectory(_sourceDir)
            Directory.CreateDirectory(_destDir)

            _context = New DatabaseContext(_dbPath)
            Await _context.InitializeDatabaseAsync()

            _jobRepo = New BackupJobRepository(_context)
            _runRepo = New BackupRunRepository(_context)
            _fileRepo = New BackupFileRepository(_context)
            _logRepo = New LogRepository(_context)

            _checksumService = New Sha256ChecksumService()
            _compressionService = New ZipCompressionService()
            _encryptionService = New AesEncryptionService()
            _logService = New LogService(_logRepo)
            _retentionService = New RetentionService(_runRepo, _logService)

            _engine = New BackupEngine(_jobRepo, _runRepo, _fileRepo, _checksumService, _compressionService, _encryptionService, _retentionService, _logService)
            _verificationService = New VerificationService(_runRepo, _fileRepo, _checksumService, _logService)
        End Function

        <TestCleanup>
        Public Sub Cleanup()
            Try
                If Directory.Exists(_tempRoot) Then
                    Directory.Delete(_tempRoot, True)
                End If
            Catch
            End Try
        End Sub

        <TestMethod>
        Public Async Function RunBackupAsync_FullBackup_CopiesAllFilesAndVerifies() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "file1.txt"), "Content 1")
            Directory.CreateDirectory(Path.Combine(_sourceDir, "SubDir"))
            File.WriteAllText(Path.Combine(_sourceDir, "SubDir", "file2.txt"), "Content 2")

            Dim job As New BackupJob With {
                .Name = "Full Test Job",
                .SourcePath = _sourceDir,
                .DestinationPath = _destDir,
                .BackupType = BackupType.Full,
                .CompressionLevel = CompressionLevelOption.None,
                .EnableEncryption = False,
                .RetentionCount = 5
            }
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run = Await _engine.RunBackupAsync(job, Nothing, CancellationToken.None)

            Assert.AreEqual(BackupStatus.Completed, run.Status)
            Assert.AreEqual(2, run.TotalFiles)
            Assert.AreEqual(2, run.CopiedFiles)
            Assert.AreEqual(0, run.SkippedFiles)
            Assert.AreEqual(0, run.FailedFiles)
            Assert.IsTrue(Directory.Exists(run.DestinationPath))

            Dim verification = Await _verificationService.VerifyRunAsync(run.Id, CancellationToken.None)
            Assert.IsTrue(verification.IsSuccess)
            Assert.AreEqual(2, verification.VerifiedFiles)
            Assert.AreEqual(0, verification.FailedFiles)
        End Function

        <TestMethod>
        Public Async Function RunBackupAsync_IncrementalBackup_SkipsUnmodifiedFiles() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "unchanged.txt"), "Unchanged Content")
            File.WriteAllText(Path.Combine(_sourceDir, "toModify.txt"), "Initial Content")

            Dim job As New BackupJob With {
                .Name = "Incremental Test Job",
                .SourcePath = _sourceDir,
                .DestinationPath = _destDir,
                .BackupType = BackupType.Full,
                .CompressionLevel = CompressionLevelOption.None,
                .RetentionCount = 5
            }
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run1 = Await _engine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run1.Status)
            Assert.AreEqual(2, run1.CopiedFiles)

            job.BackupType = BackupType.Incremental
            Await _jobRepo.UpdateAsync(job)

            File.WriteAllText(Path.Combine(_sourceDir, "toModify.txt"), "Modified Content Here!")
            File.WriteAllText(Path.Combine(_sourceDir, "brandNew.txt"), "New File Content")

            Dim run2 = Await _engine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run2.Status)
            Assert.AreEqual(3, run2.TotalFiles)
            Assert.AreEqual(2, run2.CopiedFiles)
            Assert.AreEqual(1, run2.SkippedFiles)
            Assert.AreEqual(0, run2.FailedFiles)
        End Function

        <TestMethod>
        Public Async Function RunBackupAsync_CompressedAndEncrypted_GeneratesProtectedArchive() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "secure.txt"), "Confidential data for encryption")

            Dim password = "StrongPassword789!"
            Dim salt = _encryptionService.GenerateSalt()
            Dim hash = _encryptionService.HashPassword(password, salt)

            Dim job As New BackupJob With {
                .Name = "Secure Encrypted Job",
                .SourcePath = _sourceDir,
                .DestinationPath = _destDir,
                .BackupType = BackupType.Full,
                .CompressionLevel = CompressionLevelOption.Optimal,
                .EnableEncryption = True,
                .EncryptionSalt = salt,
                .EncryptionPasswordHash = hash,
                .RetentionCount = 3
            }
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run = Await _engine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run.Status)
            Assert.IsTrue(File.Exists(run.DestinationPath))
            Assert.IsTrue(run.DestinationPath.EndsWith(".enc"))

            Dim verification = Await _verificationService.VerifyRunAsync(run.Id, CancellationToken.None)
            Assert.IsTrue(verification.IsSuccess)
        End Function
    End Class
End Namespace
