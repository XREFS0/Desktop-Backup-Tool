Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IRestoreEngine
        Function RestoreAsync(request As RestoreRequest, Optional progress As IProgress(Of RestoreProgressReport) = Nothing, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of Boolean)
        Function GetPreviewFilesAsync(runId As Long) As Task(Of IReadOnlyList(Of BackupFileRecord))
    End Interface
End Namespace
