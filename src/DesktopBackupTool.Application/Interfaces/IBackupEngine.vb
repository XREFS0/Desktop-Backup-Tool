Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IBackupEngine
        Function RunBackupAsync(job As BackupJob, Optional progress As IProgress(Of BackupProgressReport) = Nothing, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of BackupRun)
        ReadOnly Property IsRunning As Boolean
        ReadOnly Property ActiveJobId As Nullable(Of Long)
    End Interface
End Namespace
