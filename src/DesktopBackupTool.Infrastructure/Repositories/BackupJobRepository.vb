Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces
Imports DesktopBackupTool.Infrastructure.Database
Imports Microsoft.Data.Sqlite

Namespace DesktopBackupTool.Infrastructure.Repositories
    Public Class BackupJobRepository
        Implements IBackupJobRepository

        Private ReadOnly _dbContext As DatabaseContext

        Public Sub New(dbContext As DatabaseContext)
            _dbContext = dbContext
        End Sub

        Public Async Function GetAllAsync() As Task(Of IReadOnlyList(Of BackupJob)) Implements IBackupJobRepository.GetAllAsync
            Dim list As New List(Of BackupJob)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, SourcePath, DestinationPath, BackupType, ScheduleType, ScheduleTime, ScheduleDaysOfWeek, ScheduleIntervalHours, IsEnabled, CompressionLevel, EnableEncryption, EncryptionPasswordHash, EncryptionSalt, RetentionCount, ExcludedDirectories, ExcludedExtensions, FileFilterPatterns, LastRunUtc, LastStatus, CreatedAtUtc, UpdatedAtUtc FROM BackupJobs ORDER BY Name ASC;"
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadJob(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of BackupJob) Implements IBackupJobRepository.GetByIdAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, SourcePath, DestinationPath, BackupType, ScheduleType, ScheduleTime, ScheduleDaysOfWeek, ScheduleIntervalHours, IsEnabled, CompressionLevel, EnableEncryption, EncryptionPasswordHash, EncryptionSalt, RetentionCount, ExcludedDirectories, ExcludedExtensions, FileFilterPatterns, LastRunUtc, LastStatus, CreatedAtUtc, UpdatedAtUtc FROM BackupJobs WHERE Id = @Id LIMIT 1;"
                    cmd.Parameters.AddWithValue("@Id", id)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return ReadJob(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function GetEnabledJobsAsync() As Task(Of IReadOnlyList(Of BackupJob)) Implements IBackupJobRepository.GetEnabledJobsAsync
            Dim list As New List(Of BackupJob)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, SourcePath, DestinationPath, BackupType, ScheduleType, ScheduleTime, ScheduleDaysOfWeek, ScheduleIntervalHours, IsEnabled, CompressionLevel, EnableEncryption, EncryptionPasswordHash, EncryptionSalt, RetentionCount, ExcludedDirectories, ExcludedExtensions, FileFilterPatterns, LastRunUtc, LastStatus, CreatedAtUtc, UpdatedAtUtc FROM BackupJobs WHERE IsEnabled = 1 ORDER BY Id ASC;"
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadJob(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function InsertAsync(job As BackupJob) As Task(Of Long) Implements IBackupJobRepository.InsertAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
INSERT INTO BackupJobs (Name, SourcePath, DestinationPath, BackupType, ScheduleType, ScheduleTime, ScheduleDaysOfWeek, ScheduleIntervalHours, IsEnabled, CompressionLevel, EnableEncryption, EncryptionPasswordHash, EncryptionSalt, RetentionCount, ExcludedDirectories, ExcludedExtensions, FileFilterPatterns, LastRunUtc, LastStatus, CreatedAtUtc, UpdatedAtUtc)
VALUES (@Name, @SourcePath, @DestinationPath, @BackupType, @ScheduleType, @ScheduleTime, @ScheduleDaysOfWeek, @ScheduleIntervalHours, @IsEnabled, @CompressionLevel, @EnableEncryption, @EncryptionPasswordHash, @EncryptionSalt, @RetentionCount, @ExcludedDirectories, @ExcludedExtensions, @FileFilterPatterns, @LastRunUtc, @LastStatus, @CreatedAtUtc, @UpdatedAtUtc);
SELECT last_insert_rowid();"
                    BindParameters(cmd, job)
                    Dim result = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt64(result)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(job As BackupJob) As Task(Of Boolean) Implements IBackupJobRepository.UpdateAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
UPDATE BackupJobs SET
    Name = @Name,
    SourcePath = @SourcePath,
    DestinationPath = @DestinationPath,
    BackupType = @BackupType,
    ScheduleType = @ScheduleType,
    ScheduleTime = @ScheduleTime,
    ScheduleDaysOfWeek = @ScheduleDaysOfWeek,
    ScheduleIntervalHours = @ScheduleIntervalHours,
    IsEnabled = @IsEnabled,
    CompressionLevel = @CompressionLevel,
    EnableEncryption = @EnableEncryption,
    EncryptionPasswordHash = @EncryptionPasswordHash,
    EncryptionSalt = @EncryptionSalt,
    RetentionCount = @RetentionCount,
    ExcludedDirectories = @ExcludedDirectories,
    ExcludedExtensions = @ExcludedExtensions,
    FileFilterPatterns = @FileFilterPatterns,
    LastRunUtc = @LastRunUtc,
    LastStatus = @LastStatus,
    UpdatedAtUtc = @UpdatedAtUtc
WHERE Id = @Id;"
                    cmd.Parameters.AddWithValue("@Id", job.Id)
                    BindParameters(cmd, job)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task(Of Boolean) Implements IBackupJobRepository.DeleteAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM BackupJobs WHERE Id = @Id;"
                    cmd.Parameters.AddWithValue("@Id", id)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function UpdateLastRunStatusAsync(id As Long, lastRunUtc As DateTime, status As BackupStatus) As Task(Of Boolean) Implements IBackupJobRepository.UpdateLastRunStatusAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "UPDATE BackupJobs SET LastRunUtc = @LastRunUtc, LastStatus = @LastStatus, UpdatedAtUtc = @UpdatedAtUtc WHERE Id = @Id;"
                    cmd.Parameters.AddWithValue("@Id", id)
                    cmd.Parameters.AddWithValue("@LastRunUtc", lastRunUtc.ToString("o"))
                    cmd.Parameters.AddWithValue("@LastStatus", CInt(status))
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("o"))
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function SetEnabledAsync(id As Long, isEnabled As Boolean) As Task(Of Boolean) Implements IBackupJobRepository.SetEnabledAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "UPDATE BackupJobs SET IsEnabled = @IsEnabled, UpdatedAtUtc = @UpdatedAtUtc WHERE Id = @Id;"
                    cmd.Parameters.AddWithValue("@Id", id)
                    cmd.Parameters.AddWithValue("@IsEnabled", If(isEnabled, 1, 0))
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("o"))
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Private Sub BindParameters(cmd As SqliteCommand, job As BackupJob)
            cmd.Parameters.AddWithValue("@Name", job.Name)
            cmd.Parameters.AddWithValue("@SourcePath", job.SourcePath)
            cmd.Parameters.AddWithValue("@DestinationPath", job.DestinationPath)
            cmd.Parameters.AddWithValue("@BackupType", CInt(job.BackupType))
            cmd.Parameters.AddWithValue("@ScheduleType", CInt(job.ScheduleType))
            cmd.Parameters.AddWithValue("@ScheduleTime", job.ScheduleTime)
            cmd.Parameters.AddWithValue("@ScheduleDaysOfWeek", If(job.ScheduleDaysOfWeek, String.Empty))
            cmd.Parameters.AddWithValue("@ScheduleIntervalHours", job.ScheduleIntervalHours)
            cmd.Parameters.AddWithValue("@IsEnabled", If(job.IsEnabled, 1, 0))
            cmd.Parameters.AddWithValue("@CompressionLevel", CInt(job.CompressionLevel))
            cmd.Parameters.AddWithValue("@EnableEncryption", If(job.EnableEncryption, 1, 0))
            cmd.Parameters.AddWithValue("@EncryptionPasswordHash", If(job.EncryptionPasswordHash, String.Empty))
            cmd.Parameters.AddWithValue("@EncryptionSalt", If(job.EncryptionSalt, String.Empty))
            cmd.Parameters.AddWithValue("@RetentionCount", job.RetentionCount)
            cmd.Parameters.AddWithValue("@ExcludedDirectories", If(job.ExcludedDirectories, String.Empty))
            cmd.Parameters.AddWithValue("@ExcludedExtensions", If(job.ExcludedExtensions, String.Empty))
            cmd.Parameters.AddWithValue("@FileFilterPatterns", If(job.FileFilterPatterns, "*.*"))
            cmd.Parameters.AddWithValue("@LastRunUtc", If(job.LastRunUtc.HasValue, job.LastRunUtc.Value.ToString("o"), DBNull.Value))
            cmd.Parameters.AddWithValue("@LastStatus", If(job.LastStatus.HasValue, CInt(job.LastStatus.Value), DBNull.Value))
            cmd.Parameters.AddWithValue("@CreatedAtUtc", job.CreatedAtUtc.ToString("o"))
            cmd.Parameters.AddWithValue("@UpdatedAtUtc", job.UpdatedAtUtc.ToString("o"))
        End Sub

        Private Function ReadJob(reader As SqliteDataReader) As BackupJob
            Dim job As New BackupJob()
            job.Id = reader.GetInt64(0)
            job.Name = reader.GetString(1)
            job.SourcePath = reader.GetString(2)
            job.DestinationPath = reader.GetString(3)
            job.BackupType = CType(reader.GetInt32(4), BackupType)
            job.ScheduleType = CType(reader.GetInt32(5), ScheduleType)
            job.ScheduleTime = reader.GetString(6)
            job.ScheduleDaysOfWeek = If(reader.IsDBNull(7), String.Empty, reader.GetString(7))
            job.ScheduleIntervalHours = reader.GetInt32(8)
            job.IsEnabled = (reader.GetInt32(9) = 1)
            job.CompressionLevel = CType(reader.GetInt32(10), CompressionLevelOption)
            job.EnableEncryption = (reader.GetInt32(11) = 1)
            job.EncryptionPasswordHash = If(reader.IsDBNull(12), String.Empty, reader.GetString(12))
            job.EncryptionSalt = If(reader.IsDBNull(13), String.Empty, reader.GetString(13))
            job.RetentionCount = reader.GetInt32(14)
            job.ExcludedDirectories = If(reader.IsDBNull(15), String.Empty, reader.GetString(15))
            job.ExcludedExtensions = If(reader.IsDBNull(16), String.Empty, reader.GetString(16))
            job.FileFilterPatterns = If(reader.IsDBNull(17), "*.*", reader.GetString(17))

            If Not reader.IsDBNull(18) Then
                Dim strVal = reader.GetString(18)
                Dim dtVal As DateTime
                If DateTime.TryParse(strVal, Nothing, Globalization.DateTimeStyles.RoundtripKind, dtVal) Then
                    job.LastRunUtc = dtVal
                End If
            End If

            If Not reader.IsDBNull(19) Then
                job.LastStatus = CType(reader.GetInt32(19), BackupStatus)
            End If

            job.CreatedAtUtc = DateTime.Parse(reader.GetString(20), Nothing, Globalization.DateTimeStyles.RoundtripKind)
            job.UpdatedAtUtc = DateTime.Parse(reader.GetString(21), Nothing, Globalization.DateTimeStyles.RoundtripKind)

            Return job
        End Function
    End Class
End Namespace
