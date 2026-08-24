Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces
Imports DesktopBackupTool.Infrastructure.Database
Imports Microsoft.Data.Sqlite

Namespace DesktopBackupTool.Infrastructure.Repositories
    Public Class LogRepository
        Implements ILogRepository

        Private ReadOnly _dbContext As DatabaseContext

        Public Sub New(dbContext As DatabaseContext)
            _dbContext = dbContext
        End Sub

        Public Async Function InsertAsync(log As AppLog) As Task(Of Long) Implements ILogRepository.InsertAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
INSERT INTO AppLogs (TimestampUtc, Level, Message, Context, ExceptionDetails)
VALUES (@TimestampUtc, @Level, @Message, @Context, @ExceptionDetails);
SELECT last_insert_rowid();"
                    cmd.Parameters.AddWithValue("@TimestampUtc", log.TimestampUtc.ToString("o"))
                    cmd.Parameters.AddWithValue("@Level", CInt(log.Level))
                    cmd.Parameters.AddWithValue("@Message", log.Message)
                    cmd.Parameters.AddWithValue("@Context", If(log.Context, String.Empty))
                    cmd.Parameters.AddWithValue("@ExceptionDetails", If(log.ExceptionDetails, String.Empty))
                    Dim result = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt64(result)
                End Using
            End Using
        End Function

        Public Async Function GetLogsAsync(limit As Integer, Optional minLevel As Nullable(Of LogLevel) = Nothing, Optional searchTerm As String = Nothing) As Task(Of IReadOnlyList(Of AppLog)) Implements ILogRepository.GetLogsAsync
            Dim list As New List(Of AppLog)()
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    Dim sql = "SELECT Id, TimestampUtc, Level, Message, Context, ExceptionDetails FROM AppLogs WHERE 1=1"

                    If minLevel.HasValue Then
                        sql &= " AND Level >= @MinLevel"
                        cmd.Parameters.AddWithValue("@MinLevel", CInt(minLevel.Value))
                    End If

                    If Not String.IsNullOrWhiteSpace(searchTerm) Then
                        sql &= " AND (Message LIKE @Search OR Context LIKE @Search OR ExceptionDetails LIKE @Search)"
                        cmd.Parameters.AddWithValue("@Search", "%" & searchTerm.Trim() & "%")
                    End If

                    sql &= " ORDER BY TimestampUtc DESC LIMIT @Limit;"
                    cmd.Parameters.AddWithValue("@Limit", limit)
                    cmd.CommandText = sql

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            Dim log As New AppLog()
                            log.Id = reader.GetInt64(0)
                            log.TimestampUtc = DateTime.Parse(reader.GetString(1), Nothing, Globalization.DateTimeStyles.RoundtripKind)
                            log.Level = CType(reader.GetInt32(2), LogLevel)
                            log.Message = reader.GetString(3)
                            log.Context = If(reader.IsDBNull(4), String.Empty, reader.GetString(4))
                            log.ExceptionDetails = If(reader.IsDBNull(5), String.Empty, reader.GetString(5))
                            list.Add(log)
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function ClearLogsAsync() As Task(Of Integer) Implements ILogRepository.ClearLogsAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM AppLogs;"
                    Return Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                End Using
            End Using
        End Function
    End Class
End Namespace
