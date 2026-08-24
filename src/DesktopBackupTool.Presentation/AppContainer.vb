Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Application.Services
Imports DesktopBackupTool.Application.Validators
Imports DesktopBackupTool.Domain.Interfaces
Imports DesktopBackupTool.Infrastructure.Backup
Imports DesktopBackupTool.Infrastructure.Compression
Imports DesktopBackupTool.Infrastructure.Database
Imports DesktopBackupTool.Infrastructure.Encryption
Imports DesktopBackupTool.Infrastructure.FileSystem
Imports DesktopBackupTool.Infrastructure.Logging
Imports DesktopBackupTool.Infrastructure.Repositories
Imports DesktopBackupTool.Infrastructure.Scheduling

Namespace DesktopBackupTool.Presentation
    Public Class AppContainer
        Private Shared _instance As AppContainer
        Public Shared ReadOnly Property Instance As AppContainer
            Get
                If _instance Is Nothing Then
                    _instance = New AppContainer()
                End If
                Return _instance
            End Get
        End Property

        Public Property DbContext As DatabaseContext
        Public Property JobRepository As IBackupJobRepository
        Public Property RunRepository As IBackupRunRepository
        Public Property FileRepository As IBackupFileRepository
        Public Property LogRepository As ILogRepository
        Public Property SettingsRepository As ISettingsRepository

        Public Property ChecksumService As IChecksumService
        Public Property CompressionService As ICompressionService
        Public Property EncryptionService As IEncryptionService
        Public Property LogService As ILogService
        Public Property RetentionService As IRetentionService
        Public Property JobValidator As IJobValidator
        Public Property JobService As IBackupJobService
        Public Property BackupEngine As IBackupEngine
        Public Property RestoreEngine As IRestoreEngine
        Public Property VerificationService As IVerificationService
        Public Property SchedulerService As ISchedulerService

        Public Sub New()
            DbContext = New DatabaseContext()
            JobRepository = New BackupJobRepository(DbContext)
            RunRepository = New BackupRunRepository(DbContext)
            FileRepository = New BackupFileRepository(DbContext)
            LogRepository = New LogRepository(DbContext)
            SettingsRepository = New SettingsRepository(DbContext)

            ChecksumService = New Sha256ChecksumService()
            CompressionService = New ZipCompressionService()
            EncryptionService = New AesEncryptionService()
            LogService = New LogService(LogRepository)
            RetentionService = New RetentionService(RunRepository, LogService)
            JobValidator = New JobValidator()

            JobService = New BackupJobService(JobRepository, RunRepository, JobValidator, LogService)
            BackupEngine = New BackupEngine(JobRepository, RunRepository, FileRepository, ChecksumService, CompressionService, EncryptionService, RetentionService, LogService)
            RestoreEngine = New RestoreEngine(JobRepository, RunRepository, FileRepository, CompressionService, EncryptionService, LogService)
            VerificationService = New VerificationService(RunRepository, FileRepository, ChecksumService, LogService)
            SchedulerService = New WindowsSchedulerService(JobRepository, LogService)
        End Sub

        Public Async Function InitializeAsync() As Task
            Await DbContext.InitializeDatabaseAsync().ConfigureAwait(False)
            SchedulerService.Start()
        End Function
    End Class
End Namespace
