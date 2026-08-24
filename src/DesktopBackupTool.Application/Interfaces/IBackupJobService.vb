Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IBackupJobService
        Function GetAllJobsAsync() As Task(Of IReadOnlyList(Of BackupJob))
        Function GetJobByIdAsync(id As Long) As Task(Of BackupJob)
        Function SaveJobAsync(job As BackupJob) As Task(Of JobValidationResult)
        Function DeleteJobAsync(id As Long) As Task(Of Boolean)
        Function DuplicateJobAsync(id As Long) As Task(Of BackupJob)
        Function ToggleEnabledAsync(id As Long) As Task(Of Boolean)
        Function GetDashboardStatsAsync() As Task(Of DashboardStats)
    End Interface
End Namespace
