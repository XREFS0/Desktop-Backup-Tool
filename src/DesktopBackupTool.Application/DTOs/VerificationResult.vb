Namespace DesktopBackupTool.Application.DTOs
    Public Class VerificationItem
        Public Property RelativePath As String = String.Empty
        Public Property ExpectedSize As Long
        Public Property ActualSize As Long
        Public Property ExpectedHash As String = String.Empty
        Public Property ActualHash As String = String.Empty
        Public Property IsValid As Boolean
        Public Property ErrorMessage As String = String.Empty
    End Class

    Public Class VerificationResult
        Public Property IsSuccess As Boolean = True
        Public Property TotalFiles As Integer = 0
        Public Property VerifiedFiles As Integer = 0
        Public Property FailedFiles As Integer = 0
        Public Property MissingFiles As Integer = 0
        Public Property CorruptedFiles As Integer = 0
        Public Property Items As New List(Of VerificationItem)
        Public Property Message As String = String.Empty
        Public Property Duration As TimeSpan = TimeSpan.Zero
    End Class
End Namespace
