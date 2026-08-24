Namespace DesktopBackupTool.Domain.Entities
    Public Class AppSettings
        Public Property Id As Long
        Public Property SettingKey As String = String.Empty
        Public Property SettingValue As String = String.Empty
        Public Property UpdatedAtUtc As DateTime = DateTime.UtcNow
    End Class
End Namespace
