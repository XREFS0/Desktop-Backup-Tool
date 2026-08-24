Imports DesktopBackupTool.Domain.Enums

Namespace DesktopBackupTool.Application.DTOs
    Public Class RestoreRequest
        Public Property RunId As Long
        Public Property DestinationDirectory As String = String.Empty
        Public Property SelectedRelativePaths As New List(Of String)
        Public Property OverwriteMode As OverwriteMode = OverwriteMode.Overwrite
        Public Property DecryptionPassword As String = String.Empty
    End Class
End Namespace
