Imports System.IO
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Infrastructure.Backup
    Public Class RestoreEngine
        Implements IRestoreEngine

        Private ReadOnly _jobRepository As IBackupJobRepository
        Private ReadOnly _runRepository As IBackupRunRepository
        Private ReadOnly _fileRepository As IBackupFileRepository
        Private ReadOnly _compressionService As ICompressionService
        Private ReadOnly _encryptionService As IEncryptionService
        Private ReadOnly _logService As ILogService

        Public Sub New(
            jobRepository As IBackupJobRepository,
            runRepository As IBackupRunRepository,
            fileRepository As IBackupFileRepository,
            compressionService As ICompressionService,
            encryptionService As IEncryptionService,
            logService As ILogService)

            _jobRepository = jobRepository
            _runRepository = runRepository
            _fileRepository = fileRepository
            _compressionService = compressionService
            _encryptionService = encryptionService
            _logService = logService
        End Sub

        Public Async Function GetPreviewFilesAsync(runId As Long) As Task(Of IReadOnlyList(Of BackupFileRecord)) Implements IRestoreEngine.GetPreviewFilesAsync
            Return Await _fileRepository.GetFilesByRunIdAsync(runId).ConfigureAwait(False)
        End Function

        Public Async Function RestoreAsync(request As RestoreRequest, Optional progress As IProgress(Of RestoreProgressReport) = Nothing, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of Boolean) Implements IRestoreEngine.RestoreAsync
            If request Is Nothing Then
                Throw New ArgumentNullException(NameOf(request))
            End If

            If String.IsNullOrWhiteSpace(request.DestinationDirectory) Then
                Throw New ArgumentException("Restore destination directory is required.", NameOf(request.DestinationDirectory))
            End If

            Dim run = Await _runRepository.GetByIdAsync(request.RunId).ConfigureAwait(False)
            If run Is Nothing Then
                Throw New InvalidOperationException($"Backup run #{request.RunId} was not found.")
            End If

            If Not Directory.Exists(request.DestinationDirectory) Then
                Directory.CreateDirectory(request.DestinationDirectory)
            End If

            Await _logService.LogInfoAsync($"Starting restore operation for run #{run.Id} (Job: '{run.JobName}') to '{request.DestinationDirectory}'", "RestoreEngine").ConfigureAwait(False)

            Dim workingPayload = run.DestinationPath
            Dim tempFilesToClean As New List(Of String)()
            Dim tempDirsToClean As New List(Of String)()
            Dim isCancelled As Boolean = False
            Dim errorEx As Exception = Nothing

            Try
                If run.IsEncrypted Then
                    If String.IsNullOrEmpty(request.DecryptionPassword) Then
                        Throw New UnauthorizedAccessException("Password is required to decrypt this backup.")
                    End If

                    Dim job = Await _jobRepository.GetByIdAsync(run.JobId).ConfigureAwait(False)
                    Dim passToUse = request.DecryptionPassword
                    If job IsNot Nothing AndAlso Not String.IsNullOrEmpty(job.EncryptionSalt) AndAlso Not String.IsNullOrEmpty(job.EncryptionPasswordHash) Then
                        If Not _encryptionService.VerifyPassword(request.DecryptionPassword, job.EncryptionSalt, job.EncryptionPasswordHash) Then
                            Throw New UnauthorizedAccessException("Incorrect password provided for decryption.")
                        End If
                        passToUse = job.EncryptionPasswordHash
                    End If

                    Dim decryptedTemp = Path.Combine(Path.GetTempPath(), $"Restore_Decrypted_{Guid.NewGuid().ToString("N")}" & If(run.IsCompressed, ".zip", ".dbk"))
                    tempFilesToClean.Add(decryptedTemp)

                    If progress IsNot Nothing Then
                        progress.Report(New RestoreProgressReport With {.StatusMessage = "Decrypting backup archive..."})
                    End If

                    Await _encryptionService.DecryptFileAsync(workingPayload, decryptedTemp, passToUse, cancellationToken).ConfigureAwait(False)
                    workingPayload = decryptedTemp
                End If

                If run.IsCompressed OrElse workingPayload.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) Then
                    If progress IsNot Nothing Then
                        progress.Report(New RestoreProgressReport With {.StatusMessage = "Extracting files from archive..."})
                    End If

                    Await _compressionService.ExtractArchiveAsync(workingPayload, request.DestinationDirectory, request.SelectedRelativePaths, request.OverwriteMode, progress, cancellationToken).ConfigureAwait(False)
                Else
                    Dim filesToRestore = Await _fileRepository.GetFilesByRunIdAsync(run.Id).ConfigureAwait(False)
                    If request.SelectedRelativePaths IsNot Nothing AndAlso request.SelectedRelativePaths.Count > 0 Then
                        Dim selectedSet As New HashSet(Of String)(request.SelectedRelativePaths, StringComparer.OrdinalIgnoreCase)
                        filesToRestore = filesToRestore.Where(Function(f) selectedSet.Contains(f.RelativePath)).ToList()
                    End If

                    Dim totalFiles = filesToRestore.Count
                    Dim processedFiles = 0

                    For Each fileRec In filesToRestore
                        cancellationToken.ThrowIfCancellationRequested()

                        Dim sourceFilePath = Path.Combine(workingPayload, fileRec.RelativePath)
                        Dim destFilePath = Path.Combine(request.DestinationDirectory, fileRec.RelativePath)

                        Dim shouldCopy = True
                        If File.Exists(destFilePath) Then
                            Select Case request.OverwriteMode
                                Case OverwriteMode.Skip
                                    shouldCopy = False
                                Case OverwriteMode.OverwriteIfNewer
                                    Dim curInfo As New FileInfo(destFilePath)
                                    If curInfo.LastWriteTimeUtc >= fileRec.LastModifiedUtc Then
                                        shouldCopy = False
                                    End If
                                Case OverwriteMode.Overwrite
                                    shouldCopy = True
                            End Select
                        End If

                        If shouldCopy AndAlso File.Exists(sourceFilePath) Then
                            Dim parentDir = Path.GetDirectoryName(destFilePath)
                            If Not String.IsNullOrWhiteSpace(parentDir) AndAlso Not Directory.Exists(parentDir) Then
                                Directory.CreateDirectory(parentDir)
                            End If

                            File.Copy(sourceFilePath, destFilePath, True)
                            File.SetLastWriteTimeUtc(destFilePath, fileRec.LastModifiedUtc)
                        End If

                        processedFiles += 1

                        If progress IsNot Nothing Then
                            Dim report As New RestoreProgressReport()
                            report.CurrentFile = fileRec.RelativePath
                            report.ProcessedFiles = processedFiles
                            report.TotalFiles = totalFiles
                            report.Percentage = If(totalFiles > 0, (processedFiles * 100) \ totalFiles, 0)
                            report.StatusMessage = $"Restoring file {processedFiles} of {totalFiles}"
                            progress.Report(report)
                        End If
                    Next
                End If

            Catch ex As OperationCanceledException
                isCancelled = True
            Catch ex As Exception
                errorEx = ex
            Finally
                For Each tf In tempFilesToClean
                    Try
                        If File.Exists(tf) Then
                            File.Delete(tf)
                        End If
                    Catch
                    End Try
                Next
                For Each td In tempDirsToClean
                    Try
                        If Directory.Exists(td) Then
                            Directory.Delete(td, True)
                        End If
                    Catch
                    End Try
                Next
            End Try

            If isCancelled Then
                Await _logService.LogWarningAsync($"Restore operation cancelled for run #{run.Id}", "RestoreEngine").ConfigureAwait(False)
                Return False
            ElseIf errorEx IsNot Nothing Then
                Await _logService.LogErrorAsync($"Restore operation failed for run #{run.Id}: {errorEx.Message}", errorEx, "RestoreEngine").ConfigureAwait(False)
                Throw errorEx
            Else
                Await _logService.LogInfoAsync($"Restore operation completed successfully for run #{run.Id} to '{request.DestinationDirectory}'", "RestoreEngine").ConfigureAwait(False)
                Return True
            End If
        End Function
    End Class
End Namespace
