Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface ILogService
        Function LogInfoAsync(message As String, Optional context As String = "") As Task
        Function LogWarningAsync(message As String, Optional context As String = "") As Task
        Function LogErrorAsync(message As String, Optional ex As Exception = Nothing, Optional context As String = "") As Task
        Function GetLogsAsync(limit As Integer, Optional minLevel As Nullable(Of LogLevel) = Nothing, Optional search As String = Nothing) As Task(Of IReadOnlyList(Of AppLog))
        Function ClearLogsAsync() As Task(Of Integer)
    End Interface
End Namespace
