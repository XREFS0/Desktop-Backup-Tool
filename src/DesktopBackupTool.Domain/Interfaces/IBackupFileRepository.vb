Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Domain.Interfaces
    Public Interface IBackupFileRepository
        Function InsertBatchAsync(files As IEnumerable(Of BackupFileRecord)) As Task(Of Integer)
        Function GetFilesByRunIdAsync(runId As Long) As Task(Of IReadOnlyList(Of BackupFileRecord))
        Function GetLatestFileRecordsForJobAsync(jobId As Long) As Task(Of IReadOnlyDictionary(Of String, BackupFileRecord))
        Function DeleteByRunIdAsync(runId As Long) As Task(Of Integer)
    End Interface
End Namespace
