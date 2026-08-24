Imports Microsoft.Data.Sqlite

Namespace DesktopBackupTool.Infrastructure.Database
    Public Class DatabaseContext
        Private ReadOnly _connectionString As String
        Private ReadOnly _databasePath As String

        Public Sub New(Optional customDbPath As String = Nothing)
            If String.IsNullOrWhiteSpace(customDbPath) Then
                Dim appDataDir = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopBackupTool")
                If Not IO.Directory.Exists(appDataDir) Then
                    IO.Directory.CreateDirectory(appDataDir)
                End If
                _databasePath = IO.Path.Combine(appDataDir, "backup_tool.db")
            Else
                _databasePath = customDbPath
                Dim dir = IO.Path.GetDirectoryName(_databasePath)
                If Not String.IsNullOrWhiteSpace(dir) AndAlso Not IO.Directory.Exists(dir) Then
                    IO.Directory.CreateDirectory(dir)
                End If
            End If

            Dim builder As New SqliteConnectionStringBuilder()
            builder.DataSource = _databasePath
            builder.Mode = SqliteOpenMode.ReadWriteCreate
            builder.ForeignKeys = True
            builder.Pooling = True
            _connectionString = builder.ToString()
        End Sub

        Public ReadOnly Property DatabasePath As String
            Get
                Return _databasePath
            End Get
        End Property

        Public Function CreateConnection() As SqliteConnection
            Dim conn As New SqliteConnection(_connectionString)
            Return conn
        End Function

        Public Async Function InitializeDatabaseAsync() As Task
            Using conn = CreateConnection()
                Await conn.OpenAsync().ConfigureAwait(False)

                Using walCmd = conn.CreateCommand()
                    walCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;"
                    Await walCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                End Using

                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
CREATE TABLE IF NOT EXISTS BackupJobs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    SourcePath TEXT NOT NULL,
    DestinationPath TEXT NOT NULL,
    BackupType INTEGER NOT NULL,
    ScheduleType INTEGER NOT NULL,
    ScheduleTime TEXT NOT NULL,
    ScheduleDaysOfWeek TEXT,
    ScheduleIntervalHours INTEGER NOT NULL,
    IsEnabled INTEGER NOT NULL,
    CompressionLevel INTEGER NOT NULL,
    EnableEncryption INTEGER NOT NULL,
    EncryptionPasswordHash TEXT,
    EncryptionSalt TEXT,
    RetentionCount INTEGER NOT NULL,
    ExcludedDirectories TEXT,
    ExcludedExtensions TEXT,
    FileFilterPatterns TEXT,
    LastRunUtc TEXT,
    LastStatus INTEGER,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS BackupRuns (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    JobId INTEGER NOT NULL,
    JobName TEXT NOT NULL,
    BackupType INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    StartedAtUtc TEXT NOT NULL,
    CompletedAtUtc TEXT,
    TotalFiles INTEGER NOT NULL,
    CopiedFiles INTEGER NOT NULL,
    SkippedFiles INTEGER NOT NULL,
    FailedFiles INTEGER NOT NULL,
    TotalBytes INTEGER NOT NULL,
    CopiedBytes INTEGER NOT NULL,
    DestinationPath TEXT NOT NULL,
    IsCompressed INTEGER NOT NULL,
    IsEncrypted INTEGER NOT NULL,
    ErrorMessage TEXT,
    FOREIGN KEY (JobId) REFERENCES BackupJobs(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS BackupFiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RunId INTEGER NOT NULL,
    JobId INTEGER NOT NULL,
    RelativePath TEXT NOT NULL,
    FileSizeBytes INTEGER NOT NULL,
    LastModifiedUtc TEXT NOT NULL,
    Sha256Hash TEXT NOT NULL,
    IsDirectory INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES BackupRuns(Id) ON DELETE CASCADE,
    FOREIGN KEY (JobId) REFERENCES BackupJobs(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AppLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampUtc TEXT NOT NULL,
    Level INTEGER NOT NULL,
    Message TEXT NOT NULL,
    Context TEXT,
    ExceptionDetails TEXT
);

CREATE TABLE IF NOT EXISTS AppSettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SettingKey TEXT UNIQUE NOT NULL,
    SettingValue TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_backup_runs_jobid ON BackupRuns(JobId, StartedAtUtc DESC);
CREATE INDEX IF NOT EXISTS idx_backup_files_runid ON BackupFiles(RunId);
CREATE INDEX IF NOT EXISTS idx_backup_files_job_path ON BackupFiles(JobId, RelativePath);
CREATE INDEX IF NOT EXISTS idx_app_logs_level_timestamp ON AppLogs(Level, TimestampUtc DESC);
CREATE INDEX IF NOT EXISTS idx_settings_key ON AppSettings(SettingKey);
"
                    Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                End Using
            End Using
        End Function
    End Class
End Namespace
