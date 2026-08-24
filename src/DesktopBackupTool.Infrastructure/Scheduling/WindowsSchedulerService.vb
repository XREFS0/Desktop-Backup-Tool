Imports System.Threading
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Infrastructure.Scheduling
    Public Class WindowsSchedulerService
        Implements ISchedulerService, IDisposable

        Private ReadOnly _jobRepository As IBackupJobRepository
        Private ReadOnly _logService As ILogService
        Private _timer As Timer
        Private _isProcessing As Boolean = False
        Private _disposed As Boolean = False

        Public Event JobTriggered(sender As Object, job As BackupJob) Implements ISchedulerService.JobTriggered

        Public Sub New(jobRepository As IBackupJobRepository, logService As ILogService)
            _jobRepository = jobRepository
            _logService = logService
        End Sub

        Public Sub Start() Implements ISchedulerService.Start
            If _timer Is Nothing Then
                _timer = New Timer(AddressOf OnTimerTick, Nothing, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30))
            End If
        End Sub

        Public Sub [Stop]() Implements ISchedulerService.Stop
            _timer?.Change(Timeout.Infinite, Timeout.Infinite)
        End Sub

        Private Async Sub OnTimerTick(state As Object)
            If _isProcessing Then
                Return
            End If
            _isProcessing = True

            Dim tickError As Exception = Nothing
            Dim triggeredJobs As New List(Of BackupJob)()

            Try
                Dim enabledJobs = Await _jobRepository.GetEnabledJobsAsync().ConfigureAwait(False)
                Dim nowUtc = DateTime.UtcNow

                For Each job In enabledJobs
                    If IsDue(job, nowUtc) Then
                        triggeredJobs.Add(job)
                    End If
                Next
            Catch ex As Exception
                tickError = ex
            Finally
                _isProcessing = False
            End Try

            If tickError IsNot Nothing Then
                Await _logService.LogErrorAsync($"Scheduler tick error: {tickError.Message}", tickError, "SchedulerService").ConfigureAwait(False)
            End If

            For Each job In triggeredJobs
                Await _logService.LogInfoAsync($"Scheduler triggered job '{job.Name}' (ID: {job.Id})", "SchedulerService").ConfigureAwait(False)
                RaiseEvent JobTriggered(Me, job)
            Next
        End Sub

        Public Function IsDue(job As BackupJob, referenceTimeUtc As DateTime) As Boolean Implements ISchedulerService.IsDue
            If job Is Nothing OrElse Not job.IsEnabled Then
                Return False
            End If

            If job.ScheduleType = ScheduleType.Manual Then
                Return False
            End If

            Dim localRefTime = referenceTimeUtc.ToLocalTime()

            Select Case job.ScheduleType
                Case ScheduleType.Daily
                    Dim targetTime As TimeSpan
                    If TimeSpan.TryParse(job.ScheduleTime, targetTime) Then
                        If localRefTime.TimeOfDay >= targetTime Then
                            If Not job.LastRunUtc.HasValue Then
                                Return True
                            End If
                            Dim lastLocal = job.LastRunUtc.Value.ToLocalTime()
                            If lastLocal.Date < localRefTime.Date Then
                                Return True
                            End If
                        End If
                    End If

                Case ScheduleType.Weekly
                    Dim targetTime As TimeSpan
                    If TimeSpan.TryParse(job.ScheduleTime, targetTime) Then
                        Dim currentDay = localRefTime.DayOfWeek.ToString()
                        Dim days = If(job.ScheduleDaysOfWeek, String.Empty).Split(","c, StringSplitOptions.RemoveEmptyEntries).Select(Function(d) d.Trim())

                        If days.Contains(currentDay, StringComparer.OrdinalIgnoreCase) Then
                            If localRefTime.TimeOfDay >= targetTime Then
                                If Not job.LastRunUtc.HasValue Then
                                    Return True
                                End If
                                Dim lastLocal = job.LastRunUtc.Value.ToLocalTime()
                                If lastLocal.Date < localRefTime.Date Then
                                    Return True
                                End If
                            End If
                        End If
                    End If

                Case ScheduleType.IntervalHours
                    If Not job.LastRunUtc.HasValue Then
                        Return True
                    End If
                    Dim elapsedHours = (referenceTimeUtc - job.LastRunUtc.Value).TotalHours
                    If elapsedHours >= job.ScheduleIntervalHours Then
                        Return True
                    End If
            End Select

            Return False
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                _timer?.Dispose()
                _disposed = True
            End If
        End Sub
    End Class
End Namespace
