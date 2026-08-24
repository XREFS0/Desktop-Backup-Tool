Imports System.IO
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Infrastructure.Backup
Imports DesktopBackupTool.Infrastructure.Database
Imports DesktopBackupTool.Infrastructure.Logging
Imports DesktopBackupTool.Infrastructure.Repositories
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class RetentionTests
        Private _tempRoot As String
        Private _dbPath As String
        Private _context As DatabaseContext
        Private _jobRepo As BackupJobRepository
        Private _runRepo As BackupRunRepository
        Private _logRepo As LogRepository
        Private _logService As LogService
        Private _retentionService As RetentionService

        <TestInitialize>
        Public Async Function SetupAsync() As Task
            _tempRoot = Path.Combine(Path.GetTempPath(), $"RetentionTest_{Guid.NewGuid():N}")
            _dbPath = Path.Combine(_tempRoot, "test_retention.db")
            Directory.CreateDirectory(_tempRoot)

            _context = New DatabaseContext(_dbPath)
            Await _context.InitializeDatabaseAsync()

            _jobRepo = New BackupJobRepository(_context)
            _runRepo = New BackupRunRepository(_context)
            _logRepo = New LogRepository(_context)
            _logService = New LogService(_logRepo)
            _retentionService = New RetentionService(_runRepo, _logService)
        End Function

        <TestCleanup>
        Public Sub Cleanup()
            Try
                If Directory.Exists(_tempRoot) Then Directory.Delete(_tempRoot, True)
            Catch
            End Try
        End Sub

        <TestMethod>
        Public Async Function ApplyRetentionPolicy_PrunesOldRunsBeyondLimit() As Task
            Dim job As New BackupJob With {.Name = "Retention Test Job", .SourcePath = "C:\A", .DestinationPath = "C:\B", .RetentionCount = 2}
            job.Id = Await _jobRepo.InsertAsync(job)

            For i As Integer = 1 To 4
                Dim dummyFolder = Path.Combine(_tempRoot, $"Run_{i}")
                Directory.CreateDirectory(dummyFolder)

                Dim run As New BackupRun With {
                    .JobId = job.Id,
                    .JobName = job.Name,
                    .Status = BackupStatus.Completed,
                    .StartedAtUtc = DateTime.UtcNow.AddMinutes(i),
                    .CompletedAtUtc = DateTime.UtcNow.AddMinutes(i + 1),
                    .DestinationPath = dummyFolder
                }
                Await _runRepo.InsertAsync(run)
            Next

            Dim allBefore = Await _runRepo.GetRunsForJobAsync(job.Id)
            Assert.AreEqual(4, allBefore.Count)

            Dim prunedCount = Await _retentionService.ApplyRetentionPolicyAsync(job)
            Assert.AreEqual(2, prunedCount)

            Dim allAfter = Await _runRepo.GetRunsForJobAsync(job.Id)
            Assert.AreEqual(2, allAfter.Count)
        End Function
    End Class
End Namespace
