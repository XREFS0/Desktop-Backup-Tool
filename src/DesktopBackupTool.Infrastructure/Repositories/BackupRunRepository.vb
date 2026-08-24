Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces
Imports DesktopBackupTool.Infrastructure.Database
Imports Microsoft.Data.Sqlite

Namespace DesktopBackupTool.Infrastructure.Repositories
    Public Class BackupRunRepository
        Implements IBackupRunRepository

        Private ReadOnly _dbContext As DatabaseContext

        Public Sub New(dbContext As DatabaseContext)
            _dbContext = dbContext
        End Sub

        Public Async Function GetAllAsync() As Task(Of IReadOnlyList(Of BackupRun)) Implements IBackupRunRepository.GetAllAsync
            Dim list As New List(Of BackupRun)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage FROM BackupRuns ORDER BY StartedAtUtc DESC;"
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadRun(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of BackupRun) Implements IBackupRunRepository.GetByIdAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage FROM BackupRuns WHERE Id = @Id LIMIT 1;"
                    cmd.Parameters.AddWithValue("@Id", id)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return ReadRun(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function GetRunsForJobAsync(jobId As Long) As Task(Of IReadOnlyList(Of BackupRun)) Implements IBackupRunRepository.GetRunsForJobAsync
            Dim list As New List(Of BackupRun)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage FROM BackupRuns WHERE JobId = @JobId ORDER BY StartedAtUtc DESC;"
                    cmd.Parameters.AddWithValue("@JobId", jobId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadRun(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetRecentRunsAsync(limit As Integer) As Task(Of IReadOnlyList(Of BackupRun)) Implements IBackupRunRepository.GetRecentRunsAsync
            Dim list As New List(Of BackupRun)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage FROM BackupRuns ORDER BY StartedAtUtc DESC LIMIT @Limit;"
                    cmd.Parameters.AddWithValue("@Limit", limit)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadRun(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function InsertAsync(run As BackupRun) As Task(Of Long) Implements IBackupRunRepository.InsertAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
INSERT INTO BackupRuns (JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage)
VALUES (@JobId, @JobName, @BackupType, @Status, @StartedAtUtc, @CompletedAtUtc, @TotalFiles, @CopiedFiles, @SkippedFiles, @FailedFiles, @TotalBytes, @CopiedBytes, @DestinationPath, @IsCompressed, @IsEncrypted, @ErrorMessage);
SELECT last_insert_rowid();"
                    BindParameters(cmd, run)
                    Dim result = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt64(result)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(run As BackupRun) As Task(Of Boolean) Implements IBackupRunRepository.UpdateAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
UPDATE BackupRuns SET
    JobId = @JobId,
    JobName = @JobName,
    BackupType = @BackupType,
    Status = @Status,
    StartedAtUtc = @StartedAtUtc,
    CompletedAtUtc = @CompletedAtUtc,
    TotalFiles = @TotalFiles,
    CopiedFiles = @CopiedFiles,
    SkippedFiles = @SkippedFiles,
    FailedFiles = @FailedFiles,
    TotalBytes = @TotalBytes,
    CopiedBytes = @CopiedBytes,
    DestinationPath = @DestinationPath,
    IsCompressed = @IsCompressed,
    IsEncrypted = @IsEncrypted,
    ErrorMessage = @ErrorMessage
WHERE Id = @Id;"
                    cmd.Parameters.AddWithValue("@Id", run.Id)
                    BindParameters(cmd, run)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task(Of Boolean) Implements IBackupRunRepository.DeleteAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM BackupRuns WHERE Id = @Id;"
                    cmd.Parameters.AddWithValue("@Id", id)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function GetOldRunsForRetentionAsync(jobId As Long, keepCount As Integer) As Task(Of IReadOnlyList(Of BackupRun)) Implements IBackupRunRepository.GetOldRunsForRetentionAsync
            Dim list As New List(Of BackupRun)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
SELECT Id, JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage
FROM BackupRuns
WHERE JobId = @JobId AND (Status = 2 OR Status = 3)
ORDER BY StartedAtUtc DESC
LIMIT -1 OFFSET @KeepCount;"
                    cmd.Parameters.AddWithValue("@JobId", jobId)
                    cmd.Parameters.AddWithValue("@KeepCount", keepCount)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadRun(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetLatestSuccessfulRunAsync(jobId As Long) As Task(Of BackupRun) Implements IBackupRunRepository.GetLatestSuccessfulRunAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
SELECT Id, JobId, JobName, BackupType, Status, StartedAtUtc, CompletedAtUtc, TotalFiles, CopiedFiles, SkippedFiles, FailedFiles, TotalBytes, CopiedBytes, DestinationPath, IsCompressed, IsEncrypted, ErrorMessage
FROM BackupRuns
WHERE JobId = @JobId AND (Status = 2 OR Status = 3)
ORDER BY StartedAtUtc DESC
LIMIT 1;"
                    cmd.Parameters.AddWithValue("@JobId", jobId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return ReadRun(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Private Sub BindParameters(cmd As SqliteCommand, run As BackupRun)
            cmd.Parameters.AddWithValue("@JobId", run.JobId)
            cmd.Parameters.AddWithValue("@JobName", run.JobName)
            cmd.Parameters.AddWithValue("@BackupType", CInt(run.BackupType))
            cmd.Parameters.AddWithValue("@Status", CInt(run.Status))
            cmd.Parameters.AddWithValue("@StartedAtUtc", run.StartedAtUtc.ToString("o"))
            cmd.Parameters.AddWithValue("@CompletedAtUtc", If(run.CompletedAtUtc.HasValue, run.CompletedAtUtc.Value.ToString("o"), DBNull.Value))
            cmd.Parameters.AddWithValue("@TotalFiles", run.TotalFiles)
            cmd.Parameters.AddWithValue("@CopiedFiles", run.CopiedFiles)
            cmd.Parameters.AddWithValue("@SkippedFiles", run.SkippedFiles)
            cmd.Parameters.AddWithValue("@FailedFiles", run.FailedFiles)
            cmd.Parameters.AddWithValue("@TotalBytes", run.TotalBytes)
            cmd.Parameters.AddWithValue("@CopiedBytes", run.CopiedBytes)
            cmd.Parameters.AddWithValue("@DestinationPath", If(run.DestinationPath, String.Empty))
            cmd.Parameters.AddWithValue("@IsCompressed", If(run.IsCompressed, 1, 0))
            cmd.Parameters.AddWithValue("@IsEncrypted", If(run.IsEncrypted, 1, 0))
            cmd.Parameters.AddWithValue("@ErrorMessage", If(run.ErrorMessage, String.Empty))
        End Sub

        Private Function ReadRun(reader As SqliteDataReader) As BackupRun
            Dim run As New BackupRun()
            run.Id = reader.GetInt64(0)
            run.JobId = reader.GetInt64(1)
            run.JobName = reader.GetString(2)
            run.BackupType = CType(reader.GetInt32(3), BackupType)
            run.Status = CType(reader.GetInt32(4), BackupStatus)
            run.StartedAtUtc = DateTime.Parse(reader.GetString(5), Nothing, Globalization.DateTimeStyles.RoundtripKind)

            If Not reader.IsDBNull(6) Then
                Dim strVal = reader.GetString(6)
                Dim dtVal As DateTime
                If DateTime.TryParse(strVal, Nothing, Globalization.DateTimeStyles.RoundtripKind, dtVal) Then
                    run.CompletedAtUtc = dtVal
                End If
            End If

            run.TotalFiles = reader.GetInt32(7)
            run.CopiedFiles = reader.GetInt32(8)
            run.SkippedFiles = reader.GetInt32(9)
            run.FailedFiles = reader.GetInt32(10)
            run.TotalBytes = reader.GetInt64(11)
            run.CopiedBytes = reader.GetInt64(12)
            run.DestinationPath = If(reader.IsDBNull(13), String.Empty, reader.GetString(13))
            run.IsCompressed = (reader.GetInt32(14) = 1)
            run.IsEncrypted = (reader.GetInt32(15) = 1)
            run.ErrorMessage = If(reader.IsDBNull(16), String.Empty, reader.GetString(16))

            Return run
        End Function
    End Class
End Namespace
