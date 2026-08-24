Imports System.IO
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Infrastructure.Backup
    Public Class RetentionService
        Implements IRetentionService

        Private ReadOnly _runRepository As IBackupRunRepository
        Private ReadOnly _logService As ILogService

        Public Sub New(runRepository As IBackupRunRepository, logService As ILogService)
            _runRepository = runRepository
            _logService = logService
        End Sub

        Public Async Function ApplyRetentionPolicyAsync(job As BackupJob) As Task(Of Integer) Implements IRetentionService.ApplyRetentionPolicyAsync
            If job Is Nothing OrElse job.RetentionCount <= 0 Then
                Return 0
            End If

            Dim oldRuns = Await _runRepository.GetOldRunsForRetentionAsync(job.Id, job.RetentionCount).ConfigureAwait(False)
            Dim deletedCount = 0

            For Each run In oldRuns
                Dim errorEx As Exception = Nothing
                Try
                    If Not String.IsNullOrWhiteSpace(run.DestinationPath) Then
                        If File.Exists(run.DestinationPath) Then
                            File.Delete(run.DestinationPath)
                        ElseIf Directory.Exists(run.DestinationPath) Then
                            Directory.Delete(run.DestinationPath, True)
                        End If
                    End If
                Catch ex As Exception
                    errorEx = ex
                End Try

                If errorEx IsNot Nothing Then
                    Await _logService.LogErrorAsync($"Failed to clean up old backup run #{run.Id}: {errorEx.Message}", errorEx, "RetentionService").ConfigureAwait(False)
                Else
                    Await _runRepository.DeleteAsync(run.Id).ConfigureAwait(False)
                    deletedCount += 1
                    Await _logService.LogInfoAsync($"Pruned old backup run #{run.Id} for job '{job.Name}' (Retention limit: {job.RetentionCount})", "RetentionService").ConfigureAwait(False)
                End If
            Next

            Return deletedCount
        End Function
    End Class
End Namespace
