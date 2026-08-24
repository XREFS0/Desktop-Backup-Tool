Namespace DesktopBackupTool.Application.DTOs
    Public Class JobValidationResult
        Public Property IsValid As Boolean = True
        Public Property Errors As New List(Of String)
        Public Property Warnings As New List(Of String)

        Public Sub AddError(errorMessage As String)
            IsValid = False
            Errors.Add(errorMessage)
        End Sub

        Public Sub AddWarning(warningMessage As String)
            Warnings.Add(warningMessage)
        End Sub
    End Class
End Namespace
