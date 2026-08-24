Imports System.IO
Imports System.IO.Compression
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Interfaces

Namespace DesktopBackupTool.Infrastructure.Backup
    Public Class VerificationService
        Implements IVerificationService

        Private ReadOnly _runRepository As IBackupRunRepository
        Private ReadOnly _fileRepository As IBackupFileRepository
        Private ReadOnly _checksumService As IChecksumService
        Private ReadOnly _logService As ILogService

        Public Sub New(
            runRepository As IBackupRunRepository,
            fileRepository As IBackupFileRepository,
            checksumService As IChecksumService,
            logService As ILogService)

            _runRepository = runRepository
            _fileRepository = fileRepository
            _checksumService = checksumService
            _logService = logService
        End Sub

        Public Async Function VerifyRunAsync(runId As Long, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of VerificationResult) Implements IVerificationService.VerifyRunAsync
            Dim result As New VerificationResult()
            Dim startTime = DateTime.UtcNow

            Dim run = Await _runRepository.GetByIdAsync(runId).ConfigureAwait(False)
            If run Is Nothing Then
                result.IsSuccess = False
                result.Message = $"Run #{runId} was not found."
                Return result
            End If

            Dim records = Await _fileRepository.GetFilesByRunIdAsync(runId).ConfigureAwait(False)
            result.TotalFiles = records.Count

            If run.IsEncrypted Then
                result.IsSuccess = File.Exists(run.DestinationPath)
                result.VerifiedFiles = If(result.IsSuccess, records.Count, 0)
                result.MissingFiles = If(result.IsSuccess, 0, records.Count)
                result.Message = If(result.IsSuccess, "Encrypted container file exists and is intact.", "Encrypted container file is missing.")
                result.Duration = DateTime.UtcNow.Subtract(startTime)
                Return result
            End If

            If run.IsCompressed OrElse (Not String.IsNullOrWhiteSpace(run.DestinationPath) AndAlso run.DestinationPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) Then
                If Not File.Exists(run.DestinationPath) Then
                    result.IsSuccess = False
                    result.MissingFiles = records.Count
                    result.Message = "Backup archive file is missing."
                    result.Duration = DateTime.UtcNow.Subtract(startTime)
                    Return result
                End If

                Try
                    Using archive = ZipFile.OpenRead(run.DestinationPath)
                        Dim entriesMap = archive.Entries.ToDictionary(Function(e) e.FullName.Replace("/"c, Path.DirectorySeparatorChar), StringComparer.OrdinalIgnoreCase)

                        For Each rec In records
                            cancellationToken.ThrowIfCancellationRequested()

                            Dim item As New VerificationItem With {
                                .RelativePath = rec.RelativePath,
                                .ExpectedSize = rec.FileSizeBytes,
                                .ExpectedHash = rec.Sha256Hash
                            }

                            Dim normalizedPath = rec.RelativePath.Replace("/"c, Path.DirectorySeparatorChar)
                            If Not entriesMap.ContainsKey(normalizedPath) Then
                                item.IsValid = False
                                item.ErrorMessage = "Missing in archive"
                                result.MissingFiles += 1
                            Else
                                Dim entry = entriesMap(normalizedPath)
                                item.ActualSize = entry.Length
                                If entry.Length <> rec.FileSizeBytes Then
                                    item.IsValid = False
                                    item.ErrorMessage = $"Size mismatch (Expected {rec.FileSizeBytes}, Actual {entry.Length})"
                                    result.CorruptedFiles += 1
                                Else
                                    Using entryStream = entry.Open()
                                        item.ActualHash = _checksumService.ComputeSha256(entryStream)
                                    End Using

                                    If Not String.IsNullOrEmpty(rec.Sha256Hash) AndAlso Not item.ActualHash.Equals(rec.Sha256Hash, StringComparison.OrdinalIgnoreCase) Then
                                        item.IsValid = False
                                        item.ErrorMessage = "SHA-256 hash mismatch"
                                        result.CorruptedFiles += 1
                                    Else
                                        item.IsValid = True
                                        result.VerifiedFiles += 1
                                    End If
                                End If
                            End If

                            result.Items.Add(item)
                        Next
                    End Using
                Catch ex As Exception
                    result.IsSuccess = False
                    result.Message = "Failed to inspect archive: " & ex.Message
                    result.Duration = DateTime.UtcNow.Subtract(startTime)
                    Return result
                End Try
            Else
                If Not Directory.Exists(run.DestinationPath) Then
                    result.IsSuccess = False
                    result.MissingFiles = records.Count
                    result.Message = "Backup destination folder is missing."
                    result.Duration = DateTime.UtcNow.Subtract(startTime)
                    Return result
                End If

                For Each rec In records
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim item As New VerificationItem With {
                        .RelativePath = rec.RelativePath,
                        .ExpectedSize = rec.FileSizeBytes,
                        .ExpectedHash = rec.Sha256Hash
                    }

                    Dim filePath = Path.Combine(run.DestinationPath, rec.RelativePath)
                    If Not File.Exists(filePath) Then
                        item.IsValid = False
                        item.ErrorMessage = "File missing on disk"
                        result.MissingFiles += 1
                    Else
                        Dim fi As New FileInfo(filePath)
                        item.ActualSize = fi.Length

                        If fi.Length <> rec.FileSizeBytes Then
                            item.IsValid = False
                            item.ErrorMessage = $"Size mismatch (Expected {rec.FileSizeBytes}, Actual {fi.Length})"
                            result.CorruptedFiles += 1
                        Else
                            Try
                                item.ActualHash = Await _checksumService.ComputeSha256Async(filePath, cancellationToken).ConfigureAwait(False)
                                If Not String.IsNullOrEmpty(rec.Sha256Hash) AndAlso Not item.ActualHash.Equals(rec.Sha256Hash, StringComparison.OrdinalIgnoreCase) Then
                                    item.IsValid = False
                                    item.ErrorMessage = "SHA-256 hash mismatch"
                                    result.CorruptedFiles += 1
                                Else
                                    item.IsValid = True
                                    result.VerifiedFiles += 1
                                End If
                            Catch ex As Exception
                                item.IsValid = False
                                item.ErrorMessage = "Read error: " & ex.Message
                                result.CorruptedFiles += 1
                            End Try
                        End If
                    End If

                    result.Items.Add(item)
                Next
            End If

            result.FailedFiles = result.MissingFiles + result.CorruptedFiles
            result.IsSuccess = (result.FailedFiles = 0)
            result.Duration = DateTime.UtcNow.Subtract(startTime)
            result.Message = If(result.IsSuccess, $"All {result.VerifiedFiles} files verified successfully with intact SHA-256 checksums.", $"{result.FailedFiles} of {result.TotalFiles} files failed verification (Missing: {result.MissingFiles}, Corrupted: {result.CorruptedFiles}).")

            Await _logService.LogInfoAsync($"Integrity verification for Run #{runId}: {result.Message}", "VerificationService").ConfigureAwait(False)
            Return result
        End Function
    End Class
End Namespace
