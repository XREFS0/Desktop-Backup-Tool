Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Domain.Entities
    Public Class BackupJob
        Public Property Id As Long
        Public Property Name As String = String.Empty
        Public Property SourcePath As String = String.Empty
        Public Property DestinationPath As String = String.Empty
        Public Property BackupType As BackupType = BackupType.Full
        Public Property ScheduleType As ScheduleType = ScheduleType.Manual
        Public Property ScheduleTime As String = "00:00"
        Public Property ScheduleDaysOfWeek As String = String.Empty
        Public Property ScheduleIntervalHours As Integer = 24
        Public Property IsEnabled As Boolean = True
        Public Property CompressionLevel As CompressionLevelOption = CompressionLevelOption.None
        Public Property EnableEncryption As Boolean = False
        Public Property EncryptionPasswordHash As String = String.Empty
        Public Property EncryptionSalt As String = String.Empty
        Public Property RetentionCount As Integer = 5
        Public Property ExcludedDirectories As String = String.Empty
        Public Property ExcludedExtensions As String = String.Empty
        Public Property FileFilterPatterns As String = "*.*"
        Public Property LastRunUtc As Nullable(Of DateTime)
        Public Property LastStatus As Nullable(Of BackupStatus)
        Public Property CreatedAtUtc As DateTime = DateTime.UtcNow
        Public Property UpdatedAtUtc As DateTime = DateTime.UtcNow

        Public Function Clone() As BackupJob
            Return New BackupJob With {
                .Id = 0,
                .Name = Me.Name & " - Copy",
                .SourcePath = Me.SourcePath,
                .DestinationPath = Me.DestinationPath,
                .BackupType = Me.BackupType,
                .ScheduleType = Me.ScheduleType,
                .ScheduleTime = Me.ScheduleTime,
                .ScheduleDaysOfWeek = Me.ScheduleDaysOfWeek,
                .ScheduleIntervalHours = Me.ScheduleIntervalHours,
                .IsEnabled = Me.IsEnabled,
                .CompressionLevel = Me.CompressionLevel,
                .EnableEncryption = Me.EnableEncryption,
                .EncryptionPasswordHash = Me.EncryptionPasswordHash,
                .EncryptionSalt = Me.EncryptionSalt,
                .RetentionCount = Me.RetentionCount,
                .ExcludedDirectories = Me.ExcludedDirectories,
                .ExcludedExtensions = Me.ExcludedExtensions,
                .FileFilterPatterns = Me.FileFilterPatterns,
                .LastRunUtc = Nothing,
                .LastStatus = Nothing,
                .CreatedAtUtc = DateTime.UtcNow,
                .UpdatedAtUtc = DateTime.UtcNow
            }
        End Function
    End Class
End Namespace
