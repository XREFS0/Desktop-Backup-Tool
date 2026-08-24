Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Domain.Entities
    Public Class BackupRun
        Public Property Id As Long
        Public Property JobId As Long
        Public Property JobName As String = String.Empty
        Public Property BackupType As BackupType = BackupType.Full
        Public Property Status As BackupStatus = BackupStatus.Pending
        Public Property StartedAtUtc As DateTime = DateTime.UtcNow
        Public Property CompletedAtUtc As Nullable(Of DateTime)
        Public Property TotalFiles As Integer = 0
        Public Property CopiedFiles As Integer = 0
        Public Property SkippedFiles As Integer = 0
        Public Property FailedFiles As Integer = 0
        Public Property TotalBytes As Long = 0
        Public Property CopiedBytes As Long = 0
        Public Property DestinationPath As String = String.Empty
        Public Property IsCompressed As Boolean = False
        Public Property IsEncrypted As Boolean = False
        Public Property ErrorMessage As String = String.Empty

        Public ReadOnly Property Duration As TimeSpan
            Get
                If CompletedAtUtc.HasValue Then
                    Return CompletedAtUtc.Value.Subtract(StartedAtUtc)
                End If
                Return DateTime.UtcNow.Subtract(StartedAtUtc)
            End Get
        End Property
    End Class
End Namespace
