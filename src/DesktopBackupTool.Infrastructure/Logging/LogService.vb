Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Infrastructure.Logging
    Public Class LogService
        Implements ILogService

        Private ReadOnly _logRepository As ILogRepository

        Public Sub New(logRepository As ILogRepository)
            _logRepository = logRepository
        End Sub

        Public Async Function LogInfoAsync(message As String, Optional context As String = "") As Task Implements ILogService.LogInfoAsync
            Dim log As New AppLog() With {
                .TimestampUtc = DateTime.UtcNow,
                .Level = LogLevel.Information,
                .Message = message,
                .Context = context,
                .ExceptionDetails = String.Empty
            }
            Await _logRepository.InsertAsync(log).ConfigureAwait(False)
        End Function

        Public Async Function LogWarningAsync(message As String, Optional context As String = "") As Task Implements ILogService.LogWarningAsync
            Dim log As New AppLog() With {
                .TimestampUtc = DateTime.UtcNow,
                .Level = LogLevel.Warning,
                .Message = message,
                .Context = context,
                .ExceptionDetails = String.Empty
            }
            Await _logRepository.InsertAsync(log).ConfigureAwait(False)
        End Function

        Public Async Function LogErrorAsync(message As String, Optional ex As Exception = Nothing, Optional context As String = "") As Task Implements ILogService.LogErrorAsync
            Dim log As New AppLog() With {
                .TimestampUtc = DateTime.UtcNow,
                .Level = LogLevel.Error,
                .Message = message,
                .Context = context,
                .ExceptionDetails = If(ex IsNot Nothing, ex.ToString(), String.Empty)
            }
            Await _logRepository.InsertAsync(log).ConfigureAwait(False)
        End Function

        Public Async Function GetLogsAsync(limit As Integer, Optional minLevel As Nullable(Of LogLevel) = Nothing, Optional search As String = Nothing) As Task(Of IReadOnlyList(Of AppLog)) Implements ILogService.GetLogsAsync
            Return Await _logRepository.GetLogsAsync(limit, minLevel, search).ConfigureAwait(False)
        End Function

        Public Async Function ClearLogsAsync() As Task(Of Integer) Implements ILogService.ClearLogsAsync
            Return Await _logRepository.ClearLogsAsync().ConfigureAwait(False)
        End Function
    End Class
End Namespace
