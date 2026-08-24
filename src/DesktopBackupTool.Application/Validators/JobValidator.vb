Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities

Namespace DesktopBackupTool.Application.Validators
    Public Class JobValidator
        Implements IJobValidator

        Public Function Validate(job As BackupJob) As JobValidationResult Implements IJobValidator.Validate
            Dim result As New JobValidationResult()

            If job Is Nothing Then
                result.AddError("Job cannot be null.")
                Return result
            End If

            If String.IsNullOrWhiteSpace(job.Name) Then
                result.AddError("Job name is required.")
            ElseIf job.Name.Length > 100 Then
                result.AddError("Job name cannot exceed 100 characters.")
            End If

            If String.IsNullOrWhiteSpace(job.SourcePath) Then
                result.AddError("Source path is required.")
            Else
                Try
                    Dim fullSource As String = IO.Path.GetFullPath(job.SourcePath)
                    If Not IO.Directory.Exists(fullSource) Then
                        result.AddWarning("Source directory does not currently exist.")
                    End If
                Catch ex As Exception
                    result.AddError("Source path is invalid: " & ex.Message)
                End Try
            End If

            If String.IsNullOrWhiteSpace(job.DestinationPath) Then
                result.AddError("Destination path is required.")
            Else
                Try
                    Dim fullDest As String = IO.Path.GetFullPath(job.DestinationPath)
                    If Not String.IsNullOrWhiteSpace(job.SourcePath) Then
                        Dim fullSource As String = IO.Path.GetFullPath(job.SourcePath)
                        If fullDest.Equals(fullSource, StringComparison.OrdinalIgnoreCase) Then
                            result.AddError("Destination cannot be the same as the source directory.")
                        ElseIf fullDest.StartsWith(fullSource.TrimEnd(IO.Path.DirectorySeparatorChar) & IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) Then
                            result.AddError("Destination path cannot be a subdirectory of the source directory.")
                        End If
                    End If
                Catch ex As Exception
                    result.AddError("Destination path is invalid: " & ex.Message)
                End Try
            End If

            If job.RetentionCount < 1 Then
                result.AddError("Retention count must be at least 1.")
            ElseIf job.RetentionCount > 1000 Then
                result.AddError("Retention count cannot exceed 1000.")
            End If

            If job.ScheduleType = Domain.Enums.ScheduleType.Daily OrElse job.ScheduleType = Domain.Enums.ScheduleType.Weekly Then
                If String.IsNullOrWhiteSpace(job.ScheduleTime) OrElse Not System.Text.RegularExpressions.Regex.IsMatch(job.ScheduleTime, "^([01]?[0-9]|2[0-3]):[0-5][0-9]$") Then
                    result.AddError("Schedule time must be in HH:mm 24-hour format.")
                End If
            End If

            If job.ScheduleType = Domain.Enums.ScheduleType.IntervalHours Then
                If job.ScheduleIntervalHours < 1 OrElse job.ScheduleIntervalHours > 720 Then
                    result.AddError("Schedule interval must be between 1 and 720 hours.")
                End If
            End If

            If job.EnableEncryption AndAlso String.IsNullOrWhiteSpace(job.EncryptionPasswordHash) Then
                result.AddError("Encryption is enabled but no password has been configured.")
            End If

            Return result
        End Function
    End Class
End Namespace
