Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Domain.Interfaces
    Public Interface IBackupRunRepository
        Function GetAllAsync() As Task(Of IReadOnlyList(Of BackupRun))
        Function GetByIdAsync(id As Long) As Task(Of BackupRun)
        Function GetRunsForJobAsync(jobId As Long) As Task(Of IReadOnlyList(Of BackupRun))
        Function GetRecentRunsAsync(limit As Integer) As Task(Of IReadOnlyList(Of BackupRun))
        Function InsertAsync(run As BackupRun) As Task(Of Long)
        Function UpdateAsync(run As BackupRun) As Task(Of Boolean)
        Function DeleteAsync(id As Long) As Task(Of Boolean)
        Function GetOldRunsForRetentionAsync(jobId As Long, keepCount As Integer) As Task(Of IReadOnlyList(Of BackupRun))
        Function GetLatestSuccessfulRunAsync(jobId As Long) As Task(Of BackupRun)
    End Interface
End Namespace
