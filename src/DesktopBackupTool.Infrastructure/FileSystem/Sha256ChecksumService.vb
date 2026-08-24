Imports System.IO
Imports System.Security.Cryptography
Imports DesktopBackupTool.Application.Interfaces

Namespace DesktopBackupTool.Infrastructure.FileSystem
    Public Class Sha256ChecksumService
        Implements IChecksumService

        Public Async Function ComputeSha256Async(filePath As String, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of String) Implements IChecksumService.ComputeSha256Async
            Using hasher As SHA256 = SHA256.Create()
                Using stream = New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync:=True)
                    Dim hashBytes = Await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(False)
                    Return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
                End Using
            End Using
        End Function

        Public Function ComputeSha256(stream As Stream) As String Implements IChecksumService.ComputeSha256
            Using hasher As SHA256 = SHA256.Create()
                Dim hashBytes = hasher.ComputeHash(stream)
                Return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
            End Using
        End Function
    End Class
End Namespace
