Imports System.IO
Imports System.Threading
Imports DesktopBackupTool.Application.DTOs
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
    Public Class RestoreEngineTests
        Private _tempRoot As String
        Private _sourceDir As String
        Private _backupDir As String
        Private _restoreDir As String
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
        Private _backupEngine As BackupEngine
        Private _restoreEngine As RestoreEngine

        <TestInitialize>
        Public Async Function SetupAsync() As Task
            _tempRoot = Path.Combine(Path.GetTempPath(), $"RestoreTest_{Guid.NewGuid():N}")
            _sourceDir = Path.Combine(_tempRoot, "Source")
            _backupDir = Path.Combine(_tempRoot, "Backup")
            _restoreDir = Path.Combine(_tempRoot, "Restore")
            _dbPath = Path.Combine(_tempRoot, "test_restore.db")

            Directory.CreateDirectory(_sourceDir)
            Directory.CreateDirectory(_backupDir)
            Directory.CreateDirectory(_restoreDir)

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

            _backupEngine = New BackupEngine(_jobRepo, _runRepo, _fileRepo, _checksumService, _compressionService, _encryptionService, _retentionService, _logService)
            _restoreEngine = New RestoreEngine(_jobRepo, _runRepo, _fileRepo, _compressionService, _encryptionService, _logService)
        End Function

        <TestCleanup>
        Public Sub Cleanup()
            Try
                If Directory.Exists(_tempRoot) Then Directory.Delete(_tempRoot, True)
            Catch
            End Try
        End Sub

        <TestMethod>
        Public Async Function RestoreAsync_FullDirectoryBackup_RestoresAllFiles() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "document1.txt"), "Important Content 1")
            File.WriteAllText(Path.Combine(_sourceDir, "document2.txt"), "Important Content 2")

            Dim job As New BackupJob With {.Name = "Test Restore Job", .SourcePath = _sourceDir, .DestinationPath = _backupDir, .BackupType = BackupType.Full}
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run = Await _backupEngine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run.Status)

            Dim request As New RestoreRequest With {
                .RunId = run.Id,
                .DestinationDirectory = _restoreDir,
                .OverwriteMode = OverwriteMode.Overwrite,
                .SelectedRelativePaths = New List(Of String) From {"document1.txt", "document2.txt"}
            }

            Dim result = Await _restoreEngine.RestoreAsync(request, Nothing, CancellationToken.None)
            Assert.IsTrue(result)

            Assert.IsTrue(File.Exists(Path.Combine(_restoreDir, "document1.txt")))
            Assert.IsTrue(File.Exists(Path.Combine(_restoreDir, "document2.txt")))
            Assert.AreEqual("Important Content 1", File.ReadAllText(Path.Combine(_restoreDir, "document1.txt")))
            Assert.AreEqual("Important Content 2", File.ReadAllText(Path.Combine(_restoreDir, "document2.txt")))
        End Function

        <TestMethod>
        Public Async Function RestoreAsync_EncryptedCompressedBackup_DecryptsAndRestores() As Task
            File.WriteAllText(Path.Combine(_sourceDir, "secret.txt"), "Classified Restore Data")
            Dim password = "MySecurePassword123!"
            Dim salt = _encryptionService.GenerateSalt()
            Dim hash = _encryptionService.HashPassword(password, salt)

            Dim job As New BackupJob With {
                .Name = "Encrypted Restore Job",
                .SourcePath = _sourceDir,
                .DestinationPath = _backupDir,
                .BackupType = BackupType.Full,
                .CompressionLevel = CompressionLevelOption.Fast,
                .EnableEncryption = True,
                .EncryptionSalt = salt,
                .EncryptionPasswordHash = hash
            }
            job.Id = Await _jobRepo.InsertAsync(job)

            Dim run = Await _backupEngine.RunBackupAsync(job, Nothing, CancellationToken.None)
            Assert.AreEqual(BackupStatus.Completed, run.Status)

            Dim request As New RestoreRequest With {
                .RunId = run.Id,
                .DestinationDirectory = _restoreDir,
                .DecryptionPassword = password,
                .OverwriteMode = OverwriteMode.Overwrite
            }

            Dim result = Await _restoreEngine.RestoreAsync(request, Nothing, CancellationToken.None)
            Assert.IsTrue(result)

            Dim restoredFile = Path.Combine(_restoreDir, "secret.txt")
            Assert.IsTrue(File.Exists(restoredFile))
            Assert.AreEqual("Classified Restore Data", File.ReadAllText(restoredFile))
        End Function
    End Class
End Namespace
