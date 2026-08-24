Imports DesktopBackupTool.Domain.Interfaces
Imports DesktopBackupTool.Infrastructure.Database
Imports Microsoft.Data.Sqlite

Namespace DesktopBackupTool.Infrastructure.Repositories
    Public Class SettingsRepository
        Implements ISettingsRepository

        Private ReadOnly _dbContext As DatabaseContext

        Public Sub New(dbContext As DatabaseContext)
            _dbContext = dbContext
        End Sub

        Public Async Function GetValueAsync(key As String, Optional defaultValue As String = "") As Task(Of String) Implements ISettingsRepository.GetValueAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT SettingValue FROM AppSettings WHERE SettingKey = @SettingKey LIMIT 1;"
                    cmd.Parameters.AddWithValue("@SettingKey", key)
                    Dim result = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    If result IsNot Nothing AndAlso Not Convert.IsDBNull(result) Then
                        Return Convert.ToString(result)
                    End If
                End Using
            End Using
            Return defaultValue
        End Function

        Public Async Function SetValueAsync(key As String, value As String) As Task(Of Boolean) Implements ISettingsRepository.SetValueAsync
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
INSERT INTO AppSettings (SettingKey, SettingValue, UpdatedAtUtc)
VALUES (@SettingKey, @SettingValue, @UpdatedAtUtc)
ON CONFLICT(SettingKey) DO UPDATE SET
    SettingValue = excluded.SettingValue,
    UpdatedAtUtc = excluded.UpdatedAtUtc;"
                    cmd.Parameters.AddWithValue("@SettingKey", key)
                    cmd.Parameters.AddWithValue("@SettingValue", value)
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("o"))
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function GetAllSettingsAsync() As Task(Of IReadOnlyDictionary(Of String, String)) Implements ISettingsRepository.GetAllSettingsAsync
            Dim dict As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Using conn = _dbContext.CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT SettingKey, SettingValue FROM AppSettings;"
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            dict(reader.GetString(0)) = reader.GetString(1)
                        End While
                    End Using
                End Using
            End Using
            Return dict
        End Function
    End Class
End Namespace
