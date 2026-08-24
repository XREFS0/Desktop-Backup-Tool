Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Interfaces
Imports DesktopBackupTool.Infrastructure.Database
Imports Microsoft.Data.Sqlite

Namespace DesktopBackupTool.Infrastructure.Repositories
    Public Class BackupFileRepository
        Implements IBackupFileRepository

        Private ReadOnly _dbContext As DatabaseContext

        Public Sub New(dbContext As DatabaseContext)
            _dbContext = dbContext
        End Sub

        Public Async Function InsertBatchAsync(files As IEnumerable(Of BackupFileRecord)) As Task(Of Integer) Implements IBackupFileRepository.InsertBatchAsync
            Dim list = files.ToList()
            If list.Count = 0 Then
                Return 0
            End If

            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using tx = conn.BeginTransaction()
                    Using cmd = conn.CreateCommand()
                        cmd.Transaction = tx
                        cmd.CommandText = "
INSERT INTO BackupFiles (RunId, JobId, RelativePath, FileSizeBytes, LastModifiedUtc, Sha256Hash, IsDirectory, CreatedAtUtc)
VALUES (@RunId, @JobId, @RelativePath, @FileSizeBytes, @LastModifiedUtc, @Sha256Hash, @IsDirectory, @CreatedAtUtc);"

                        Dim pRunId = cmd.Parameters.Add("@RunId", SqliteType.Integer)
                        Dim pJobId = cmd.Parameters.Add("@JobId", SqliteType.Integer)
                        Dim pRelPath = cmd.Parameters.Add("@RelativePath", SqliteType.Text)
                        Dim pSize = cmd.Parameters.Add("@FileSizeBytes", SqliteType.Integer)
                        Dim pLastMod = cmd.Parameters.Add("@LastModifiedUtc", SqliteType.Text)
                        Dim pHash = cmd.Parameters.Add("@Sha256Hash", SqliteType.Text)
                        Dim pIsDir = cmd.Parameters.Add("@IsDirectory", SqliteType.Integer)
                        Dim pCreatedAt = cmd.Parameters.Add("@CreatedAtUtc", SqliteType.Text)

                        For Each file In list
                            pRunId.Value = file.RunId
                            pJobId.Value = file.JobId
                            pRelPath.Value = file.RelativePath
                            pSize.Value = file.FileSizeBytes
                            pLastMod.Value = file.LastModifiedUtc.ToString("o")
                            pHash.Value = file.Sha256Hash
                            pIsDir.Value = If(file.IsDirectory, 1, 0)
                            pCreatedAt.Value = file.CreatedAtUtc.ToString("o")
                            Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                        Next
                    End Using
                    Await tx.CommitAsync().ConfigureAwait(False)
                End Using
            End Using
            Return list.Count
        End Function

        Public Async Function GetFilesByRunIdAsync(runId As Long) As Task(Of IReadOnlyList(Of BackupFileRecord)) Implements IBackupFileRepository.GetFilesByRunIdAsync
            Dim list As New List(Of BackupFileRecord)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, RunId, JobId, RelativePath, FileSizeBytes, LastModifiedUtc, Sha256Hash, IsDirectory, CreatedAtUtc FROM BackupFiles WHERE RunId = @RunId ORDER BY RelativePath ASC;"
                    cmd.Parameters.AddWithValue("@RunId", runId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(ReadFileRecord(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetLatestFileRecordsForJobAsync(jobId As Long) As Task(Of IReadOnlyDictionary(Of String, BackupFileRecord)) Implements IBackupFileRepository.GetLatestFileRecordsForJobAsync
            Dim dict As New Dictionary(Of String, BackupFileRecord)(StringComparer.OrdinalIgnoreCase)
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
SELECT bf.Id, bf.RunId, bf.JobId, bf.RelativePath, bf.FileSizeBytes, bf.LastModifiedUtc, bf.Sha256Hash, bf.IsDirectory, bf.CreatedAtUtc
FROM BackupFiles bf
WHERE bf.RunId = (
    SELECT br.Id FROM BackupRuns br
    WHERE br.JobId = @JobId AND (br.Status = 2 OR br.Status = 3)
    ORDER BY br.StartedAtUtc DESC LIMIT 1
);"
                    cmd.Parameters.AddWithValue("@JobId", jobId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            Dim record = ReadFileRecord(reader)
                            dict(record.RelativePath) = record
                        End While
                    End Using
                End Using
            End Using
            Return dict
        End Function

        Public Async Function DeleteByRunIdAsync(runId As Long) As Task(Of Integer) Implements IBackupFileRepository.DeleteByRunIdAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM BackupFiles WHERE RunId = @RunId;"
                    cmd.Parameters.AddWithValue("@RunId", runId)
                    Return Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                End Using
            End Using
        End Function

        Private Function ReadFileRecord(reader As SqliteDataReader) As BackupFileRecord
            Dim file As New BackupFileRecord()
            file.Id = reader.GetInt64(0)
            file.RunId = reader.GetInt64(1)
            file.JobId = reader.GetInt64(2)
            file.RelativePath = reader.GetString(3)
            file.FileSizeBytes = reader.GetInt64(4)
            file.LastModifiedUtc = DateTime.Parse(reader.GetString(5), Nothing, Globalization.DateTimeStyles.RoundtripKind)
            file.Sha256Hash = reader.GetString(6)
            file.IsDirectory = (reader.GetInt32(7) = 1)
            file.CreatedAtUtc = DateTime.Parse(reader.GetString(8), Nothing, Globalization.DateTimeStyles.RoundtripKind)
            Return file
        End Function
    End Class
End Namespace
