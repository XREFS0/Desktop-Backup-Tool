Imports System.IO
Imports System.IO.Compression
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Application.Interfaces
Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Infrastructure.Compression
    Public Class ZipCompressionService
        Implements ICompressionService

        Public Async Function CompressDirectoryAsync(sourceDirectory As String, destinationZipPath As String, level As CompressionLevelOption, progress As IProgress(Of BackupProgressReport), cancellationToken As Threading.CancellationToken) As Task Implements ICompressionService.CompressDirectoryAsync
            Dim destDir = Path.GetDirectoryName(destinationZipPath)
            If Not String.IsNullOrWhiteSpace(destDir) AndAlso Not Directory.Exists(destDir) Then
                Directory.CreateDirectory(destDir)
            End If

            Dim zipLevel As CompressionLevel
            Select Case level
                Case CompressionLevelOption.Fast
                    zipLevel = CompressionLevel.Fastest
                Case CompressionLevelOption.Optimal
                    zipLevel = CompressionLevel.Optimal
                Case Else
                    zipLevel = CompressionLevel.NoCompression
            End Select

            Dim allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
            Dim totalBytes As Long = 0
            For Each f In allFiles
                Try
                    Dim fi As New FileInfo(f)
                    totalBytes += fi.Length
                Catch
                End Try
            Next

            Dim processedBytes As Long = 0
            Dim processedFiles As Integer = 0

            Using zipStream = New FileStream(destinationZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync:=True)
                Using archive = New ZipArchive(zipStream, ZipArchiveMode.Create, False)
                    For Each filePath In allFiles
                        cancellationToken.ThrowIfCancellationRequested()

                        Dim relPath = Path.GetRelativePath(sourceDirectory, filePath).Replace("\"c, "/"c)
                        Dim entry = archive.CreateEntry(relPath, zipLevel)

                        Using entryStream = entry.Open()
                            Using fileStream = New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync:=True)
                                Dim buffer(65535) As Byte
                                Dim bytesRead As Integer

                                Do
                                    cancellationToken.ThrowIfCancellationRequested()
                                    bytesRead = Await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(False)
                                    If bytesRead > 0 Then
                                        Await entryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(False)
                                        processedBytes += bytesRead

                                        If progress IsNot Nothing Then
                                            Dim rep As New BackupProgressReport()
                                            rep.CurrentFile = relPath
                                            rep.ProcessedFiles = processedFiles
                                            rep.TotalFiles = allFiles.Length
                                            rep.ProcessedBytes = processedBytes
                                            rep.TotalBytes = totalBytes
                                            rep.Percentage = If(totalBytes > 0, CInt((processedBytes * 100) \ totalBytes), 0)
                                            progress.Report(rep)
                                        End If
                                    End If
                                Loop While bytesRead > 0
                            End Using
                        End Using

                        processedFiles += 1
                    Next
                End Using
            End Using
        End Function

        Public Async Function ExtractArchiveAsync(zipPath As String, targetDirectory As String, selectedFiles As IEnumerable(Of String), overwriteMode As OverwriteMode, progress As IProgress(Of RestoreProgressReport), cancellationToken As Threading.CancellationToken) As Task Implements ICompressionService.ExtractArchiveAsync
            If Not Directory.Exists(targetDirectory) Then
                Directory.CreateDirectory(targetDirectory)
            End If

            Dim filterSet As HashSet(Of String) = Nothing
            If selectedFiles IsNot Nothing AndAlso selectedFiles.Any() Then
                filterSet = New HashSet(Of String)(selectedFiles.Select(Function(f) f.Replace("\"c, "/"c)), StringComparer.OrdinalIgnoreCase)
            End If

            Using zipStream = New FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync:=True)
                Using archive = New ZipArchive(zipStream, ZipArchiveMode.Read, False)
                    Dim entriesToExtract = archive.Entries.Where(Function(e) Not String.IsNullOrEmpty(e.Name)).ToList()

                    If filterSet IsNot Nothing Then
                        entriesToExtract = entriesToExtract.Where(Function(e) filterSet.Contains(e.FullName)).ToList()
                    End If

                    Dim totalCount = entriesToExtract.Count
                    Dim processedCount = 0

                    For Each entry In entriesToExtract
                        cancellationToken.ThrowIfCancellationRequested()

                        Dim destinationFilePath = Path.Combine(targetDirectory, entry.FullName.Replace("/"c, Path.DirectorySeparatorChar))
                        Dim parentDir = Path.GetDirectoryName(destinationFilePath)
                        If Not String.IsNullOrWhiteSpace(parentDir) AndAlso Not Directory.Exists(parentDir) Then
                            Directory.CreateDirectory(parentDir)
                        End If

                        Dim shouldExtract = True
                        If File.Exists(destinationFilePath) Then
                            Select Case overwriteMode
                                Case OverwriteMode.Skip
                                    shouldExtract = False
                                Case OverwriteMode.OverwriteIfNewer
                                    Dim currentInfo As New FileInfo(destinationFilePath)
                                    If currentInfo.LastWriteTimeUtc >= entry.LastWriteTime.UtcDateTime Then
                                        shouldExtract = False
                                    End If
                                Case OverwriteMode.Overwrite
                                    shouldExtract = True
                            End Select
                        End If

                        If shouldExtract Then
                            Using entryStream = entry.Open()
                                Using outputStream = New FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync:=True)
                                    Dim buffer(65535) As Byte
                                    Dim bytesRead As Integer

                                    Do
                                        cancellationToken.ThrowIfCancellationRequested()
                                        bytesRead = Await entryStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(False)
                                        If bytesRead > 0 Then
                                            Await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(False)
                                        End If
                                    Loop While bytesRead > 0
                                End Using
                            End Using

                            File.SetLastWriteTimeUtc(destinationFilePath, entry.LastWriteTime.UtcDateTime)
                        End If

                        processedCount += 1

                        If progress IsNot Nothing Then
                            Dim rep As New RestoreProgressReport()
                            rep.CurrentFile = entry.FullName
                            rep.ProcessedFiles = processedCount
                            rep.TotalFiles = totalCount
                            rep.Percentage = If(totalCount > 0, CInt((processedCount * 100) \ totalCount), 0)
                            progress.Report(rep)
                        End If
                    Next
                End Using
            End Using
        End Function

        Public Function ListArchiveFiles(zipPath As String) As IReadOnlyList(Of String) Implements ICompressionService.ListArchiveFiles
            Dim list As New List(Of String)()
            Using archive = ZipFile.OpenRead(zipPath)
                For Each entry In archive.Entries
                    If Not String.IsNullOrEmpty(entry.Name) Then
                        list.Add(entry.FullName.Replace("/"c, Path.DirectorySeparatorChar))
                    End If
                Next
            End Using
            Return list
        End Function
    End Class
End Namespace
