Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.DTOs
    Public Class DashboardStats
        Public Property TotalJobs As Integer = 0
        Public Property EnabledJobs As Integer = 0
        Public Property TotalBackupsRun As Integer = 0
        Public Property TotalBackupSizeBytes As Long = 0
        Public Property LastSuccessfulBackup As Nullable(Of DateTime)
        Public Property LastFailedBackup As Nullable(Of DateTime)
        Public Property IsEngineRunning As Boolean = False
        Public Property ActiveJobName As String = String.Empty
        Public Property RecentRuns As New List(Of BackupRun)
    End Class
End Namespace
