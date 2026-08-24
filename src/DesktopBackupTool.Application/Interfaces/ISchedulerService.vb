Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface ISchedulerService
        Sub Start()
        Sub [Stop]()
        Function IsDue(job As BackupJob, referenceTimeUtc As DateTime) As Boolean
        Event JobTriggered(sender As Object, job As BackupJob)
    End Interface
End Namespace
