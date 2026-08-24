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
    Public Class AdvancedBackupEngineTests
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
            _tempRoot = Path.Combine(Path.GetTempPath(), $"AdvEngineTest_{Guid.NewGuid():N}")
            _sourceDir = Path.Combine(_tempRoot, "Source")
            _destDir = Path.Combine(_tempRoot, "Dest")
            _dbPath = Path.Combine(_tempRoot, "test_adv_engine.db")

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
                If Directory.Exists(_tempRoot) Then Directory.Delete(_tempRoot, True)
            Catch
            End Try
        End Sub

        <TestMethod>
        Public Async Function RunBackupAsync_WithExclusions_SkipsExcludedDirsAndExts() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "keep.txt"), "Keep this")
            File.WriteAllText(Path.Combine(_sourceDir, "ignore.tmp"), "Ignore this")

            Dim nodeDir = Path.Combine(_sourceDir, "node_modules")
            Directory.CreateDirectory(nodeDir)
            File.WriteAllText(Path.Combine(nodeDir, "package.json"), "{}")

            Dim job As New BackupJob With {
                .Name = "Exclusion Test Job",
                .SourcePath = _sourceDir,
                .DestinationPath = _destDir,
                .BackupType = BackupType.Full,
                .ExcludedDirectories = "node_modules;bin;obj",
                .ExcludedExtensions = ".tmp;.log;.bak"
            }
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run = Await _engine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run.Status)
            Assert.AreEqual(1, run.CopiedFiles)

            Dim files = Await _fileRepo.GetFilesByRunIdAsync(run.Id)
            Assert.AreEqual(1, files.Count)
            Assert.AreEqual("keep.txt", files(0).RelativePath)
        End Function

        <TestMethod>
        Public Async Function VerificationService_DetectsTamperedFile() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "authentic.txt"), "Authentic content")

            Dim job As New BackupJob With {
                .Name = "Tamper Test Job",
                .SourcePath = _sourceDir,
                .DestinationPath = _destDir,
                .BackupType = BackupType.Full
            }
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run = Await _engine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run.Status)

            Dim backupFilePath = Path.Combine(run.DestinationPath, "authentic.txt")
            File.WriteAllText(backupFilePath, "Tampered modified content")

            Dim verification = Await _verificationService.VerifyRunAsync(run.Id, CancellationToken.None)
            Assert.IsFalse(verification.IsSuccess)
            Assert.AreEqual(1, verification.CorruptedFiles)
            Assert.AreEqual(1, verification.FailedFiles)
        End Function
    End Class
End Namespace
