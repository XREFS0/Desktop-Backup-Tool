Namespace DesktopBackupTool.Application.Interfaces
    Public Interface IEncryptionService
        Function EncryptFileAsync(sourceFilePath As String, destinationEncryptedPath As String, password As String, cancellationToken As Threading.CancellationToken) As Task
        Function DecryptFileAsync(sourceEncryptedPath As String, destinationDecryptedPath As String, password As String, cancellationToken As Threading.CancellationToken) As Task
        Function GenerateSalt() As String
        Function HashPassword(password As String, saltBase64 As String) As String
        Function VerifyPassword(password As String, saltBase64 As String, expectedHashBase64 As String) As Boolean
    End Interface
End Namespace
