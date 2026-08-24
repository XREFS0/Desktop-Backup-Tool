Namespace DesktopBackupTool.Application.DTOs
    Public Class BackupProgressReport
        Public Property CurrentFile As String = String.Empty
        Public Property ProcessedFiles As Integer = 0
        Public Property TotalFiles As Integer = 0
        Public Property ProcessedBytes As Long = 0
        Public Property TotalBytes As Long = 0
        Public Property Percentage As Integer = 0
        Public Property StatusMessage As String = String.Empty
        Public Property BytesPerSecond As Long = 0
    End Class
End Namespace
