Imports System.Drawing
Imports System.Windows.Forms
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Dialogs
    Public Class VerificationReportDialog
        Inherits Form

        Private ReadOnly _result As VerificationResult
        Private grid As DataGridView
        Private lblSummary As Label
        Private btnClose As Button

        Public Sub New(result As VerificationResult)
            _result = result
            InitializeComponent()
            LoadReport()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Backup Integrity Verification"
            Me.Size = New Size(760, 520)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Font = UITheme.MainFont
            Me.BackColor = UITheme.ContentBackground

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 70, .Padding = New Padding(16), .BackColor = Color.FromArgb(248, 250, 252)}
            lblSummary = New Label() With {.Dock = DockStyle.Fill, .Font = UITheme.BoldFont}
            pnlHeader.Controls.Add(lblSummary)

            grid = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(grid)
            grid.Columns.Add("Path", "Relative File Path")
            grid.Columns.Add("Size", "Expected Size")
            grid.Columns.Add("Status", "Integrity Status")
            grid.Columns.Add("Error", "Details")
            grid.Columns(0).Width = 260
            grid.Columns(1).Width = 100
            grid.Columns(2).Width = 120
            grid.Columns(3).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            Dim pnlBottom As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50, .Padding = New Padding(12)}
            btnClose = New Button() With {.Text = "Close", .Location = New Point(640, 10), .Size = New Size(90, 30), .DialogResult = DialogResult.OK}
            UITheme.ApplySecondaryButtonStyle(btnClose)
            pnlBottom.Controls.Add(btnClose)

            Me.Controls.Add(grid)
            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(pnlBottom)
            Me.AcceptButton = btnClose
        End Sub

        Private Sub LoadReport()
            If _result.IsSuccess Then
                lblSummary.ForeColor = UITheme.SuccessColor
                lblSummary.Text = $"Verification Passed: {_result.Message} (Duration: {_result.Duration.TotalSeconds:F2}s)"
            Else
                lblSummary.ForeColor = UITheme.DangerColor
                lblSummary.Text = $"Verification Failed: {_result.Message} (Duration: {_result.Duration.TotalSeconds:F2}s)"
            End If

            grid.Rows.Clear()
            For Each item In _result.Items
                Dim rowIndex = grid.Rows.Add(item.RelativePath, UITheme.FormatBytes(item.ExpectedSize), If(item.IsValid, "Valid", "Corrupted / Missing"), item.ErrorMessage)
                Dim row = grid.Rows(rowIndex)
                If Not item.IsValid Then
                    row.DefaultCellStyle.ForeColor = UITheme.DangerColor
                    row.DefaultCellStyle.BackColor = UITheme.DangerBg
                Else
                    row.DefaultCellStyle.ForeColor = UITheme.SuccessColor
                End If
            Next
        End Sub
    End Class
End Namespace
