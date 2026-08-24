Imports System.IO
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Infrastructure.Backup
    Public Class BackupEngine
        Implements IBackupEngine

        Private ReadOnly _jobRepository As IBackupJobRepository
        Private ReadOnly _runRepository As IBackupRunRepository
        Private ReadOnly _fileRepository As IBackupFileRepository
        Private ReadOnly _checksumService As IChecksumService
        Private ReadOnly _compressionService As ICompressionService
        Private ReadOnly _encryptionService As IEncryptionService
        Private ReadOnly _retentionService As IRetentionService
        Private ReadOnly _logService As ILogService

        Private _isRunning As Boolean = False
        Private _activeJobId As Nullable(Of Long) = Nothing
        Private ReadOnly _lockObj As New Object()

        Public Sub New(
            jobRepository As IBackupJobRepository,
            runRepository As IBackupRunRepository,
            fileRepository As IBackupFileRepository,
            checksumService As IChecksumService,
            compressionService As ICompressionService,
            encryptionService As IEncryptionService,
            retentionService As IRetentionService,
            logService As ILogService)

            _jobRepository = jobRepository
            _runRepository = runRepository
            _fileRepository = fileRepository
            _checksumService = checksumService
            _compressionService = compressionService
            _encryptionService = encryptionService
            _retentionService = retentionService
            _logService = logService
        End Sub

        Public ReadOnly Property IsRunning As Boolean Implements IBackupEngine.IsRunning
            Get
                SyncLock _lockObj
                    Return _isRunning
                End SyncLock
            End Get
        End Property

        Public ReadOnly Property ActiveJobId As Nullable(Of Long) Implements IBackupEngine.ActiveJobId
            Get
                SyncLock _lockObj
                    Return _activeJobId
                End SyncLock
            End Get
        End Property

        Public Async Function RunBackupAsync(job As BackupJob, Optional progress As IProgress(Of BackupProgressReport) = Nothing, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of BackupRun) Implements IBackupEngine.RunBackupAsync
            SyncLock _lockObj
                If _isRunning Then
                    Throw New InvalidOperationException("Another backup operation is currently running.")
                End If
                _isRunning = True
                _activeJobId = job.Id
            End SyncLock

            Dim run As New BackupRun()
            run.JobId = job.Id
            run.JobName = job.Name
            run.BackupType = job.BackupType
            run.Status = BackupStatus.Running
            run.StartedAtUtc = DateTime.UtcNow
            run.IsCompressed = (job.CompressionLevel <> CompressionLevelOption.None)
            run.IsEncrypted = job.EnableEncryption

            Dim timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
            Dim sanitizedJobName = String.Join("_", job.Name.Split(Path.GetInvalidFileNameChars()))
            Dim runFolderName = $"{sanitizedJobName}_{timeStamp}"
            Dim targetBaseDir = Path.Combine(job.DestinationPath, runFolderName)
            Dim stagingDir = targetBaseDir
            Dim finalDestination = targetBaseDir

            Dim cancellationOccurred As Boolean = False
            Dim fatalError As Exception = Nothing

            Try
                run.Id = Await _runRepository.InsertAsync(run).ConfigureAwait(False)
                Await _logService.LogInfoAsync($"Starting {job.BackupType} backup for '{job.Name}' (Run #{run.Id})", "BackupEngine").ConfigureAwait(False)

                If Not Directory.Exists(job.SourcePath) Then
                    Throw New DirectoryNotFoundException($"Source directory does not exist: {job.SourcePath}")
                End If

                If Not Directory.Exists(job.DestinationPath) Then
                    Directory.CreateDirectory(job.DestinationPath)
                End If

                If run.IsCompressed OrElse run.IsEncrypted Then
                    stagingDir = Path.Combine(Path.GetTempPath(), "DesktopBackupTool_Staging", runFolderName)
                End If

                If Not Directory.Exists(stagingDir) Then
                    Directory.CreateDirectory(stagingDir)
                End If

                Dim previousFiles As IReadOnlyDictionary(Of String, BackupFileRecord) = New Dictionary(Of String, BackupFileRecord)(StringComparer.OrdinalIgnoreCase)
                If job.BackupType = BackupType.Incremental Then
                    previousFiles = Await _fileRepository.GetLatestFileRecordsForJobAsync(job.Id).ConfigureAwait(False)
                End If

                Dim sourceFiles = EnumerateSourceFiles(job.SourcePath, job)
                run.TotalFiles = sourceFiles.Count
                run.TotalBytes = sourceFiles.Sum(Function(f) f.Length)

                Dim fileRecords As New List(Of BackupFileRecord)()
                Dim processedBytes As Long = 0
                Dim copiedBytes As Long = 0
                Dim processedFilesCount As Integer = 0
                Dim copiedFilesCount As Integer = 0
                Dim skippedFilesCount As Integer = 0
                Dim failedFilesCount As Integer = 0
                Dim errorMessages As New List(Of String)()

                For Each fileInfo In sourceFiles
                    If cancellationToken.IsCancellationRequested Then
                        run.Status = BackupStatus.Cancelled
                        Exit For
                    End If

                    Dim relativePath = Path.GetRelativePath(job.SourcePath, fileInfo.FullName)
                    Dim destFilePath = Path.Combine(stagingDir, relativePath)

                    Try
                        Dim shouldCopy = True
                        Dim sha256Hash As String = String.Empty

                        If job.BackupType = BackupType.Incremental AndAlso previousFiles.ContainsKey(relativePath) Then
                            Dim prevRecord = previousFiles(relativePath)
                            Dim fileModUtc = fileInfo.LastWriteTimeUtc
                            Dim timeDifference = Math.Abs((fileModUtc - prevRecord.LastModifiedUtc).TotalSeconds)

                            If prevRecord.FileSizeBytes = fileInfo.Length AndAlso timeDifference < 2.0 Then
                                shouldCopy = False
                                sha256Hash = prevRecord.Sha256Hash
                                skippedFilesCount += 1
                                processedBytes += fileInfo.Length
                            End If
                        End If

                        If shouldCopy Then
                            Dim destFileDir = Path.GetDirectoryName(destFilePath)
                            If Not String.IsNullOrWhiteSpace(destFileDir) AndAlso Not Directory.Exists(destFileDir) Then
                                Directory.CreateDirectory(destFileDir)
                            End If

                            Await CopyFileWithProgressAsync(fileInfo.FullName, destFilePath, Sub(bytesCopied)
                                                                                                processedBytes += bytesCopied
                                                                                                copiedBytes += bytesCopied
                                                                                            End Sub, cancellationToken).ConfigureAwait(False)

                            File.SetLastWriteTimeUtc(destFilePath, fileInfo.LastWriteTimeUtc)
                            sha256Hash = Await _checksumService.ComputeSha256Async(destFilePath, cancellationToken).ConfigureAwait(False)
                            copiedFilesCount += 1
                        End If

                        Dim record As New BackupFileRecord With {
                            .RunId = run.Id,
                            .JobId = job.Id,
                            .RelativePath = relativePath,
                            .FileSizeBytes = fileInfo.Length,
                            .LastModifiedUtc = fileInfo.LastWriteTimeUtc,
                            .Sha256Hash = sha256Hash,
                            .IsDirectory = False,
                            .CreatedAtUtc = DateTime.UtcNow
                        }
                        fileRecords.Add(record)

                    Catch ex As OperationCanceledException
                        run.Status = BackupStatus.Cancelled
                        Exit For
                    Catch ex As Exception
                        failedFilesCount += 1
                        errorMessages.Add($"{relativePath}: {ex.Message}")
                    End Try

                    processedFilesCount += 1

                    If progress IsNot Nothing Then
                        Dim report As New BackupProgressReport()
                        report.CurrentFile = relativePath
                        report.ProcessedFiles = processedFilesCount
                        report.TotalFiles = run.TotalFiles
                        report.ProcessedBytes = processedBytes
                        report.TotalBytes = run.TotalBytes
                        report.Percentage = If(run.TotalBytes > 0, CInt(Math.Min(100, (processedBytes * 100) \ run.TotalBytes)), 0)
                        report.StatusMessage = $"Processing file {processedFilesCount} of {run.TotalFiles}"
                        progress.Report(report)
                    End If
                Next

                run.CopiedFiles = copiedFilesCount
                run.SkippedFiles = skippedFilesCount
                run.FailedFiles = failedFilesCount
                run.CopiedBytes = copiedBytes

                If run.Status <> BackupStatus.Cancelled Then
                    Dim currentPayloadPath = stagingDir

                    If run.IsCompressed Then
                        Dim zipPath = Path.Combine(If(run.IsEncrypted, Path.GetTempPath(), job.DestinationPath), $"{runFolderName}.zip")
                        If progress IsNot Nothing Then
                            progress.Report(New BackupProgressReport With {.StatusMessage = "Compressing backup archive...", .Percentage = 95})
                        End If
                        Await _compressionService.CompressDirectoryAsync(stagingDir, zipPath, job.CompressionLevel, progress, cancellationToken).ConfigureAwait(False)
                        currentPayloadPath = zipPath
                    End If

                    If run.IsEncrypted Then
                        Dim encExtension = If(run.IsCompressed, ".zip.enc", ".dbk.enc")
                        Dim encPath = Path.Combine(job.DestinationPath, $"{runFolderName}{encExtension}")
                        If progress IsNot Nothing Then
                            progress.Report(New BackupProgressReport With {.StatusMessage = "Encrypting backup with AES-256...", .Percentage = 98})
                        End If

                        If Directory.Exists(currentPayloadPath) Then
                            Dim tempZipBeforeEnc = Path.Combine(Path.GetTempPath(), $"{runFolderName}_temp.zip")
                            Await _compressionService.CompressDirectoryAsync(currentPayloadPath, tempZipBeforeEnc, CompressionLevelOption.Fast, Nothing, cancellationToken).ConfigureAwait(False)
                            Await _encryptionService.EncryptFileAsync(tempZipBeforeEnc, encPath, job.EncryptionPasswordHash, cancellationToken).ConfigureAwait(False)
                            If File.Exists(tempZipBeforeEnc) Then
                                File.Delete(tempZipBeforeEnc)
                            End If
                        Else
                            Await _encryptionService.EncryptFileAsync(currentPayloadPath, encPath, job.EncryptionPasswordHash, cancellationToken).ConfigureAwait(False)
                            If File.Exists(currentPayloadPath) Then
                                File.Delete(currentPayloadPath)
                            End If
                        End If
                        currentPayloadPath = encPath
                    End If

                    finalDestination = currentPayloadPath

                    If failedFilesCount > 0 Then
                        run.Status = BackupStatus.CompletedWithErrors
                        run.ErrorMessage = String.Join(Environment.NewLine, errorMessages.Take(10))
                    Else
                        run.Status = BackupStatus.Completed
                    End If
                End If

                run.DestinationPath = finalDestination
                run.CompletedAtUtc = DateTime.UtcNow

                Await _fileRepository.InsertBatchAsync(fileRecords).ConfigureAwait(False)
                Await _runRepository.UpdateAsync(run).ConfigureAwait(False)
                Await _jobRepository.UpdateLastRunStatusAsync(job.Id, run.StartedAtUtc, run.Status).ConfigureAwait(False)

                If run.Status = BackupStatus.Completed OrElse run.Status = BackupStatus.CompletedWithErrors Then
                    Await _retentionService.ApplyRetentionPolicyAsync(job).ConfigureAwait(False)
                    Await _logService.LogInfoAsync($"Backup completed successfully for '{job.Name}' (Status: {run.Status}, Copied: {run.CopiedFiles}, Skipped: {run.SkippedFiles}, Failed: {run.FailedFiles})", "BackupEngine").ConfigureAwait(False)
                ElseIf run.Status = BackupStatus.Cancelled Then
                    Await _logService.LogWarningAsync($"Backup cancelled for '{job.Name}' (Run #{run.Id})", "BackupEngine").ConfigureAwait(False)
                End If

            Catch ex As OperationCanceledException
                cancellationOccurred = True
            Catch ex As Exception
                fatalError = ex
            Finally
                If (run.IsCompressed OrElse run.IsEncrypted) AndAlso Directory.Exists(stagingDir) Then
                    Try
                        Directory.Delete(stagingDir, True)
                    Catch
                    End Try
                End If

                SyncLock _lockObj
                    _isRunning = False
                    _activeJobId = Nothing
                End SyncLock
            End Try

            If cancellationOccurred Then
                run.Status = BackupStatus.Cancelled
                run.CompletedAtUtc = DateTime.UtcNow
                run.DestinationPath = finalDestination
                Await _runRepository.UpdateAsync(run).ConfigureAwait(False)
                Await _jobRepository.UpdateLastRunStatusAsync(job.Id, run.StartedAtUtc, BackupStatus.Cancelled).ConfigureAwait(False)
                Await _logService.LogWarningAsync($"Backup operation cancelled for '{job.Name}'", "BackupEngine").ConfigureAwait(False)
            ElseIf fatalError IsNot Nothing Then
                run.Status = BackupStatus.Failed
                run.ErrorMessage = fatalError.Message
                run.CompletedAtUtc = DateTime.UtcNow
                run.DestinationPath = finalDestination
                Await _runRepository.UpdateAsync(run).ConfigureAwait(False)
                Await _jobRepository.UpdateLastRunStatusAsync(job.Id, run.StartedAtUtc, BackupStatus.Failed).ConfigureAwait(False)
                Await _logService.LogErrorAsync($"Backup failed for '{job.Name}': {fatalError.Message}", fatalError, "BackupEngine").ConfigureAwait(False)
            End If

            Return run
        End Function

        Private Function EnumerateSourceFiles(sourceDir As String, job As BackupJob) As List(Of FileInfo)
            Dim results As New List(Of FileInfo)()
            Dim excludedDirs = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If Not String.IsNullOrWhiteSpace(job.ExcludedDirectories) Then
                For Each item In job.ExcludedDirectories.Split(";"c, StringSplitOptions.RemoveEmptyEntries)
                    excludedDirs.Add(item.Trim())
                Next
            End If

            Dim excludedExts = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If Not String.IsNullOrWhiteSpace(job.ExcludedExtensions) Then
                For Each item In job.ExcludedExtensions.Split(";"c, StringSplitOptions.RemoveEmptyEntries)
                    Dim ext = item.Trim()
                    If Not ext.StartsWith("."c) Then
                        ext = "." & ext
                    End If
                    excludedExts.Add(ext)
                Next
            End If

            Dim filterPatterns As String() = {"*.*"}
            If Not String.IsNullOrWhiteSpace(job.FileFilterPatterns) Then
                filterPatterns = job.FileFilterPatterns.Split(";"c, StringSplitOptions.RemoveEmptyEntries).Select(Function(p) p.Trim()).ToArray()
            End If

            Dim dirQueue As New Queue(Of DirectoryInfo)()
            dirQueue.Enqueue(New DirectoryInfo(sourceDir))

            While dirQueue.Count > 0
                Dim currentDir = dirQueue.Dequeue()

                Try
                    For Each subDir In currentDir.GetDirectories()
                        If Not excludedDirs.Contains(subDir.Name) AndAlso (subDir.Attributes And FileAttributes.ReparsePoint) = 0 Then
                            dirQueue.Enqueue(subDir)
                        End If
                    Next
                Catch
                End Try

                Try
                    For Each pattern In filterPatterns
                        For Each fi In currentDir.GetFiles(pattern)
                            If Not excludedExts.Contains(fi.Extension) Then
                                If Not results.Any(Function(existing) existing.FullName.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase)) Then
                                    results.Add(fi)
                                End If
                            End If
                        Next
                    Next
                Catch
                End Try
            End While

            Return results
        End Function

        Private Async Function CopyFileWithProgressAsync(sourcePath As String, destPath As String, onChunkCopied As Action(Of Integer), cancellationToken As Threading.CancellationToken) As Task
            Using sourceStream = New FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync:=True)
                Using destStream = New FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync:=True)
                    Dim buffer(65535) As Byte
                    Dim bytesRead As Integer

                    Do
                        cancellationToken.ThrowIfCancellationRequested()
                        bytesRead = Await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(False)
                        If bytesRead > 0 Then
                            Await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(False)
                            onChunkCopied(bytesRead)
                        End If
                    Loop While bytesRead > 0
                End Using
            End Using
        End Function
    End Class
End Namespace
