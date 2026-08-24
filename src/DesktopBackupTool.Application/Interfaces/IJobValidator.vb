Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IJobValidator
        Function Validate(job As BackupJob) As JobValidationResult
    End Interface
End Namespace
