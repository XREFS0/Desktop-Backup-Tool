Imports System.IO
Imports DesktopBackupTool.Application.Validators
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace DesktopBackupTool.Tests
    <TestClass>
    Public Class ValidationTests
        <TestMethod>
        Public Sub Validate_EmptyName_ReturnsInvalid()
            Dim validator As New JobValidator()
            Dim job As New BackupJob With {
                .Name = "",
                .SourcePath = "C:\Source",
                .DestinationPath = "C:\Dest"
            }

            Dim result = validator.Validate(job)
            Assert.IsFalse(result.IsValid)
            Assert.IsTrue(result.Errors.Any(Function(e) e.Contains("Job name is required")))
        End Sub

        <TestMethod>
        Public Sub Validate_SameSourceAndDest_ReturnsInvalid()
            Dim validator As New JobValidator()
            Dim tempDir = Path.GetTempPath()
            Dim job As New BackupJob With {
                .Name = "Test Job",
                .SourcePath = tempDir,
                .DestinationPath = tempDir
            }

            Dim result = validator.Validate(job)
            Assert.IsFalse(result.IsValid)
            Assert.IsTrue(result.Errors.Any(Function(e) e.Contains("same as the source")))
        End Sub

        <TestMethod>
        Public Sub Validate_ZeroRetention_ReturnsInvalid()
            Dim validator As New JobValidator()
            Dim job As New BackupJob With {
                .Name = "Test Job",
                .SourcePath = "C:\Source",
                .DestinationPath = "C:\Dest",
                .RetentionCount = 0
            }

            Dim result = validator.Validate(job)
            Assert.IsFalse(result.IsValid)
            Assert.IsTrue(result.Errors.Any(Function(e) e.Contains("Retention count")))
        End Sub

        <TestMethod>
        Public Sub Validate_ValidJob_ReturnsValid()
            Dim validator As New JobValidator()
            Dim job As New BackupJob With {
                .Name = "Valid Job",
                .SourcePath = "C:\SourceFolder",
                .DestinationPath = "D:\BackupFolder",
                .RetentionCount = 5,
                .ScheduleType = ScheduleType.Daily,
                .ScheduleTime = "14:30"
            }

            Dim result = validator.Validate(job)
            Assert.IsTrue(result.IsValid)
            Assert.AreEqual(0, result.Errors.Count)
        End Sub
    End Class
End Namespace
