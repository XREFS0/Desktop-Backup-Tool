Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Domain.Entities
    Public Class AppLog
        Public Property Id As Long
        Public Property TimestampUtc As DateTime = DateTime.UtcNow
        Public Property Level As LogLevel = LogLevel.Information
        Public Property Message As String = String.Empty
        Public Property Context As String = String.Empty
        Public Property ExceptionDetails As String = String.Empty
    End Class
End Namespace
