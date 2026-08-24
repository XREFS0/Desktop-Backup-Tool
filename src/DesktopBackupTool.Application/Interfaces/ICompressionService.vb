Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface ICompressionService
        Function CompressDirectoryAsync(sourceDirectory As String, destinationZipPath As String, level As CompressionLevelOption, progress As IProgress(Of DTOs.BackupProgressReport), cancellationToken As Threading.CancellationToken) As Task
        Function ExtractArchiveAsync(zipPath As String, targetDirectory As String, selectedFiles As IEnumerable(Of String), overwriteMode As OverwriteMode, progress As IProgress(Of DTOs.RestoreProgressReport), cancellationToken As Threading.CancellationToken) As Task
        Function ListArchiveFiles(zipPath As String) As IReadOnlyList(Of String)
    End Interface
End Namespace
