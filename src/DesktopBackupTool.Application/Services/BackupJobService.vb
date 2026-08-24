Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Application.Services
    Public Class BackupJobService
        Implements IBackupJobService

        Private ReadOnly _jobRepository As IBackupJobRepository
        Private ReadOnly _runRepository As IBackupRunRepository
        Private ReadOnly _validator As IJobValidator
        Private ReadOnly _logService As ILogService

        Public Sub New(jobRepository As IBackupJobRepository, runRepository As IBackupRunRepository, validator As IJobValidator, logService As ILogService)
            _jobRepository = jobRepository
            _runRepository = runRepository
            _validator = validator
            _logService = logService
        End Sub

        Public Async Function GetAllJobsAsync() As Task(Of IReadOnlyList(Of BackupJob)) Implements IBackupJobService.GetAllJobsAsync
            Return Await _jobRepository.GetAllAsync().ConfigureAwait(False)
        End Function

        Public Async Function GetJobByIdAsync(id As Long) As Task(Of BackupJob) Implements IBackupJobService.GetJobByIdAsync
            Return Await _jobRepository.GetByIdAsync(id).ConfigureAwait(False)
        End Function

        Public Async Function SaveJobAsync(job As BackupJob) As Task(Of JobValidationResult) Implements IBackupJobService.SaveJobAsync
            Dim validation = _validator.Validate(job)
            If Not validation.IsValid Then
                Return validation
            End If

            job.UpdatedAtUtc = DateTime.UtcNow

            If job.Id = 0 Then
                job.CreatedAtUtc = DateTime.UtcNow
                Dim newId = Await _jobRepository.InsertAsync(job).ConfigureAwait(False)
                job.Id = newId
                Await _logService.LogInfoAsync($"Created backup job '{job.Name}' (ID: {job.Id})", "BackupJobService").ConfigureAwait(False)
            Else
                Await _jobRepository.UpdateAsync(job).ConfigureAwait(False)
                Await _logService.LogInfoAsync($"Updated backup job '{job.Name}' (ID: {job.Id})", "BackupJobService").ConfigureAwait(False)
            End If

            Return validation
        End Function

        Public Async Function DeleteJobAsync(id As Long) As Task(Of Boolean) Implements IBackupJobService.DeleteJobAsync
            Dim job = Await _jobRepository.GetByIdAsync(id).ConfigureAwait(False)
            Dim jobName = If(job IsNot Nothing, job.Name, id.ToString())
            Dim result = Await _jobRepository.DeleteAsync(id).ConfigureAwait(False)
            If result Then
                Await _logService.LogInfoAsync($"Deleted backup job '{jobName}' (ID: {id})", "BackupJobService").ConfigureAwait(False)
            End If
            Return result
        End Function

        Public Async Function DuplicateJobAsync(id As Long) As Task(Of BackupJob) Implements IBackupJobService.DuplicateJobAsync
            Dim original = Await _jobRepository.GetByIdAsync(id).ConfigureAwait(False)
            If original Is Nothing Then
                Return Nothing
            End If

            Dim cloned = original.Clone()
            Dim newId = Await _jobRepository.InsertAsync(cloned).ConfigureAwait(False)
            cloned.Id = newId
            Await _logService.LogInfoAsync($"Duplicated backup job '{original.Name}' to '{cloned.Name}' (ID: {cloned.Id})", "BackupJobService").ConfigureAwait(False)
            Return cloned
        End Function

        Public Async Function ToggleEnabledAsync(id As Long) As Task(Of Boolean) Implements IBackupJobService.ToggleEnabledAsync
            Dim job = Await _jobRepository.GetByIdAsync(id).ConfigureAwait(False)
            If job Is Nothing Then
                Return False
            End If

            Dim newState = Not job.IsEnabled
            Dim result = Await _jobRepository.SetEnabledAsync(id, newState).ConfigureAwait(False)
            If result Then
                Await _logService.LogInfoAsync($"Set job '{job.Name}' enabled state to {newState}", "BackupJobService").ConfigureAwait(False)
            End If
            Return result
        End Function

        Public Async Function GetDashboardStatsAsync() As Task(Of DashboardStats) Implements IBackupJobService.GetDashboardStatsAsync
            Dim allJobs = Await _jobRepository.GetAllAsync().ConfigureAwait(False)
            Dim recentRuns = Await _runRepository.GetRecentRunsAsync(10).ConfigureAwait(False)

            Dim stats As New DashboardStats()
            stats.TotalJobs = allJobs.Count
            stats.EnabledJobs = allJobs.Where(Function(j) j.IsEnabled).Count()
            stats.RecentRuns.AddRange(recentRuns)

            Dim allRuns = Await _runRepository.GetAllAsync().ConfigureAwait(False)
            stats.TotalBackupsRun = allRuns.Count

            Dim successfulRuns = allRuns.Where(Function(r) r.Status = Domain.Enums.BackupStatus.Completed OrElse r.Status = Domain.Enums.BackupStatus.CompletedWithErrors).ToList()
            Dim failedRuns = allRuns.Where(Function(r) r.Status = Domain.Enums.BackupStatus.Failed).ToList()

            If successfulRuns.Count > 0 Then
                stats.LastSuccessfulBackup = successfulRuns.Max(Function(r) r.StartedAtUtc)
                stats.TotalBackupSizeBytes = successfulRuns.Sum(Function(r) r.CopiedBytes)
            End If

            If failedRuns.Count > 0 Then
                stats.LastFailedBackup = failedRuns.Max(Function(r) r.StartedAtUtc)
            End If

            Return stats
        End Function
    End Class
End Namespace
