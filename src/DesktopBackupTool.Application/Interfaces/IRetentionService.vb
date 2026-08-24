Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IRetentionService
        Function ApplyRetentionPolicyAsync(job As BackupJob) As Task(Of Integer)
    End Interface
End Namespace
