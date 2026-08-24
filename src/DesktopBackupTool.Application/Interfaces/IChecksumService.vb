Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IChecksumService
        Function ComputeSha256Async(filePath As String, Optional cancellationToken As Threading.CancellationToken = Nothing) As Task(Of String)
        Function ComputeSha256(stream As IO.Stream) As String
    End Interface
End Namespace
