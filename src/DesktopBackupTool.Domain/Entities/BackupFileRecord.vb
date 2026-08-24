Namespace DesktopBackupTool.Domain.Entities
    Public Class BackupFileRecord
        Public Property Id As Long
        Public Property RunId As Long
        Public Property JobId As Long
        Public Property RelativePath As String = String.Empty
        Public Property FileSizeBytes As Long = 0
        Public Property LastModifiedUtc As DateTime = DateTime.UtcNow
        Public Property Sha256Hash As String = String.Empty
        Public Property IsDirectory As Boolean = False
        Public Property CreatedAtUtc As DateTime = DateTime.UtcNow
    End Class
End Namespace
