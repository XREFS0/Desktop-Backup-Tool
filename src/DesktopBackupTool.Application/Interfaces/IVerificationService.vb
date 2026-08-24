Imports DesktopBackupTool.Application.DTOs

Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IVerificationService
        Function VerifyRunAsync(runId As Long, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of VerificationResult)
    End Interface
End Namespace
