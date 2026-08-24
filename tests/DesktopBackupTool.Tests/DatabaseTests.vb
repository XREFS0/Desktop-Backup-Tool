Imports System.IO
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Infrastructure.Database
Imports DesktopBackupTool.Infrastructure.Repositories
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class DatabaseTests
        Private _dbPath As String
        Private _context As DatabaseContext
        Private _jobRepo As BackupJobRepository
        Private _runRepo As BackupRunRepository
        Private _fileRepo As BackupFileRepository
        Private _logRepo As LogRepository
        Private _settingsRepo As SettingsRepository

        <TestInitialize>
        Public Async Function SetupAsync() As Task
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid():N}.db")
            _context = New DatabaseContext(_dbPath)
            Await _context.InitializeDatabaseAsync()

            _jobRepo = New BackupJobRepository(_context)
            _runRepo = New BackupRunRepository(_context)
            _fileRepo = New BackupFileRepository(_context)
            _logRepo = New LogRepository(_context)
            _settingsRepo = New SettingsRepository(_context)
        End Function

        <TestCleanup>
        Public Sub Cleanup()
            If File.Exists(_dbPath) Then
                Try
                    File.Delete(_dbPath)
                Catch
                End Try
            End If
        End Sub

        <TestMethod>
        Public Async Function BackupJobCrud_OperatesCorrectly() As Task
            Dim job As New BackupJob With {
                .Name = "Work Documents",
                .SourcePath = "C:\Docs",
                .DestinationPath = "D:\Backups",
                .BackupType = BackupType.Incremental,
                .ScheduleType = ScheduleType.Daily,
                .ScheduleTime = "22:00",
                .RetentionCount = 7,
                .IsEnabled = True
            }

            Dim id = Await _jobRepo.InsertAsync(job)
            Assert.IsTrue(id > 0)

            Dim fetched = Await _jobRepo.GetByIdAsync(id)
            Assert.IsNotNull(fetched)
            Assert.AreEqual("Work Documents", fetched.Name)
            Assert.AreEqual(BackupType.Incremental, fetched.BackupType)
            Assert.AreEqual(7, fetched.RetentionCount)

            fetched.Name = "Updated Documents Job"
            Dim updateOk = Await _jobRepo.UpdateAsync(fetched)
            Assert.IsTrue(updateOk)

            Dim updated = Await _jobRepo.GetByIdAsync(id)
            Assert.AreEqual("Updated Documents Job", updated.Name)

            Dim deleteOk = Await _jobRepo.DeleteAsync(id)
            Assert.IsTrue(deleteOk)

            Dim deleted = Await _jobRepo.GetByIdAsync(id)
            Assert.IsNull(deleted)
        End Function

        <TestMethod>
        Public Async Function BackupRunAndFiles_CascadeDeletion_OperatesCorrectly() As Task
            Dim job As New BackupJob With {.Name = "Test Job", .SourcePath = "C:\A", .DestinationPath = "C:\B"}
            Dim jobId = Await _jobRepo.InsertAsync(job)

            Dim run As New BackupRun With {
                .JobId = jobId,
                .JobName = "Test Job",
                .Status = BackupStatus.Completed,
                .StartedAtUtc = DateTime.UtcNow,
                .CompletedAtUtc = DateTime.UtcNow.AddMinutes(2),
                .TotalFiles = 2,
                .CopiedFiles = 2,
                .TotalBytes = 1024,
                .CopiedBytes = 1024,
                .DestinationPath = "C:\B\Run_1"
            }
            Dim runId = Await _runRepo.InsertAsync(run)

            Dim files As New List(Of BackupFileRecord) From {
                New BackupFileRecord With {.RunId = runId, .JobId = jobId, .RelativePath = "doc1.txt", .FileSizeBytes = 512, .Sha256Hash = "hash1", .LastModifiedUtc = DateTime.UtcNow},
                New BackupFileRecord With {.RunId = runId, .JobId = jobId, .RelativePath = "doc2.txt", .FileSizeBytes = 512, .Sha256Hash = "hash2", .LastModifiedUtc = DateTime.UtcNow}
            }
            Dim count = Await _fileRepo.InsertBatchAsync(files)
            Assert.AreEqual(2, count)

            Dim runFiles = Await _fileRepo.GetFilesByRunIdAsync(runId)
            Assert.AreEqual(2, runFiles.Count)

            Await _runRepo.DeleteAsync(runId)
            Dim runFilesAfter = Await _fileRepo.GetFilesByRunIdAsync(runId)
            Assert.AreEqual(0, runFilesAfter.Count)
        End Function

        <TestMethod>
        Public Async Function SettingsRepository_GetAndSet_OperatesCorrectly() As Task
            Await _settingsRepo.SetValueAsync("Theme", "Dark")
            Dim val = Await _settingsRepo.GetValueAsync("Theme", "Light")
            Assert.AreEqual("Dark", val)

            Dim missing = Await _settingsRepo.GetValueAsync("NonExistent", "DefaultVal")
            Assert.AreEqual("DefaultVal", missing)
        End Function

        <TestMethod>
        Public Async Function LogRepository_InsertAndFilter_OperatesCorrectly() As Task
            Await _logRepo.InsertAsync(New AppLog With {.Level = LogLevel.Information, .Message = "Info message", .Context = "Tests"})
            Await _logRepo.InsertAsync(New AppLog With {.Level = LogLevel.Error, .Message = "Error message", .Context = "Tests"})

            Dim allLogs = Await _logRepo.GetLogsAsync(10)
            Assert.AreEqual(2, allLogs.Count)

            Dim errorLogs = Await _logRepo.GetLogsAsync(10, LogLevel.Error)
            Assert.AreEqual(1, errorLogs.Count)
            Assert.AreEqual("Error message", errorLogs(0).Message)
        End Function
    End Class
End Namespace
