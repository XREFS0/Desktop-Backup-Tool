Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Domain.Interfaces
    Public Interface ILogRepository
        Function InsertAsync(log As AppLog) As Task(Of Long)
        Function GetLogsAsync(limit As Integer, Optional minLevel As Nullable(Of LogLevel) = Nothing, Optional searchTerm As String = Nothing) As Task(Of IReadOnlyList(Of AppLog))
        Function ClearLogsAsync() As Task(Of Integer)
    End Interface
End Namespace
