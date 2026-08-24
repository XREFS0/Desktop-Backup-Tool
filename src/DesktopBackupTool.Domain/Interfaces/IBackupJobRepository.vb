Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Domain.Interfaces
    Public Interface IBackupJobRepository
        Function GetAllAsync() As Task(Of IReadOnlyList(Of BackupJob))
        Function GetByIdAsync(id As Long) As Task(Of BackupJob)
        Function GetEnabledJobsAsync() As Task(Of IReadOnlyList(Of BackupJob))
        Function InsertAsync(job As BackupJob) As Task(Of Long)
        Function UpdateAsync(job As BackupJob) As Task(Of Boolean)
        Function DeleteAsync(id As Long) As Task(Of Boolean)
        Function UpdateLastRunStatusAsync(id As Long, lastRunUtc As DateTime, status As Enums.BackupStatus) As Task(Of Boolean)
        Function SetEnabledAsync(id As Long, isEnabled As Boolean) As Task(Of Boolean)
    End Interface
End Namespace
