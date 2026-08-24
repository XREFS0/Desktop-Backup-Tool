Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Infrastructure.Scheduling
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class SchedulerTests
        <TestMethod>
        Public Sub IsDue_ManualJob_ReturnsFalse()
            Dim service As New WindowsSchedulerService(Nothing, Nothing)
            Dim job As New BackupJob With {.IsEnabled = True, .ScheduleType = ScheduleType.Manual}

            Assert.IsFalse(service.IsDue(job, DateTime.UtcNow))
        End Sub

        <TestMethod>
        Public Sub IsDue_DisabledJob_ReturnsFalse()
            Dim service As New WindowsSchedulerService(Nothing, Nothing)
            Dim job As New BackupJob With {.IsEnabled = False, .ScheduleType = ScheduleType.Daily, .ScheduleTime = "00:00"}

            Assert.IsFalse(service.IsDue(job, DateTime.UtcNow))
        End Sub

        <TestMethod>
        Public Sub IsDue_DailyJob_DueWhenTimePassedAndNotRunToday()
            Dim service As New WindowsSchedulerService(Nothing, Nothing)
            Dim nowUtc = DateTime.UtcNow
            Dim localTime = nowUtc.ToLocalTime()

            Dim pastTimeStr = localTime.AddMinutes(-5).ToString("HH:mm")
            Dim futureTimeStr = localTime.AddMinutes(5).ToString("HH:mm")

            Dim dueJob As New BackupJob With {
                .IsEnabled = True,
                .ScheduleType = ScheduleType.Daily,
                .ScheduleTime = pastTimeStr,
                .LastRunUtc = Nothing
            }
            Assert.IsTrue(service.IsDue(dueJob, nowUtc))

            Dim notDueJob As New BackupJob With {
                .IsEnabled = True,
                .ScheduleType = ScheduleType.Daily,
                .ScheduleTime = futureTimeStr,
                .LastRunUtc = Nothing
            }
            Assert.IsFalse(service.IsDue(notDueJob, nowUtc))

            Dim alreadyRunTodayJob As New BackupJob With {
                .IsEnabled = True,
                .ScheduleType = ScheduleType.Daily,
                .ScheduleTime = pastTimeStr,
                .LastRunUtc = nowUtc.AddMinutes(-10)
            }
            Assert.IsFalse(service.IsDue(alreadyRunTodayJob, nowUtc))
        End Sub

        <TestMethod>
        Public Sub IsDue_IntervalJob_DueWhenHoursElapsed()
            Dim service As New WindowsSchedulerService(Nothing, Nothing)
            Dim nowUtc = DateTime.UtcNow

            Dim job As New BackupJob With {
                .IsEnabled = True,
                .ScheduleType = ScheduleType.IntervalHours,
                .ScheduleIntervalHours = 4,
                .LastRunUtc = nowUtc.AddHours(-5)
            }
            Assert.IsTrue(service.IsDue(job, nowUtc))

            job.LastRunUtc = nowUtc.AddHours(-2)
            Assert.IsFalse(service.IsDue(job, nowUtc))
        End Sub
    End Class
End Namespace
