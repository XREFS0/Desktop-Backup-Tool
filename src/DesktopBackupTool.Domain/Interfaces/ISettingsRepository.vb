Namespace DesktopBackupTool.Domain.Interfaces
    Public Interface ISettingsRepository
        Function GetValueAsync(key As String, Optional defaultValue As String = "") As Task(Of String)
        Function SetValueAsync(key As String, value As String) As Task(Of Boolean)
        Function GetAllSettingsAsync() As Task(Of IReadOnlyDictionary(Of String, String))
    End Interface
End Namespace
