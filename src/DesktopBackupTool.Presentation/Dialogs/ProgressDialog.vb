Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Dialogs
    Public Class ProgressDialog
        Inherits Form

        Private ReadOnly _cts As CancellationTokenSource
        Private lblTitle As Label
        Private lblStatus As Label
        Private lblCurrentFile As Label
        Private lblDetails As Label
        Private progressBar As ProgressBar
        Private btnCancel As Button
        Private _isCompleted As Boolean = False

        Public ReadOnly Property CancellationTokenSource As CancellationTokenSource
            Get
                Return _cts
            End Get
        End Property

        Public Sub New(operationTitle As String, cts As CancellationTokenSource)
            _cts = cts
            InitializeComponent(operationTitle)
        End Sub

        Private Sub InitializeComponent(operationTitle As String)
            Me.Text = operationTitle
            Me.Size = New Size(540, 240)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont

            lblTitle = New Label() With {.Text = operationTitle, .Location = New Point(20, 16), .Size = New Size(480, 24), .Font = UITheme.SubheaderFont, .ForeColor = UITheme.TextPrimary}
            lblStatus = New Label() With {.Text = "Initializing operation...", .Location = New Point(20, 48), .Size = New Size(480, 20), .ForeColor = UITheme.TextSecondary}
            progressBar = New ProgressBar() With {.Location = New Point(20, 72), .Size = New Size(480, 22), .Minimum = 0, .Maximum = 100, .Value = 0}
            lblCurrentFile = New Label() With {.Text = "", .Location = New Point(20, 102), .Size = New Size(480, 20), .AutoEllipsis = True, .ForeColor = UITheme.TextSecondary, .Font = UITheme.SmallFont}
            lblDetails = New Label() With {.Text = "0 / 0 files", .Location = New Point(20, 126), .Size = New Size(480, 20), .ForeColor = UITheme.TextPrimary}

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(410, 155), .Size = New Size(90, 30)}
            UITheme.ApplySecondaryButtonStyle(btnCancel)
            AddHandler btnCancel.Click, AddressOf OnCancelClick

            Me.Controls.AddRange({lblTitle, lblStatus, progressBar, lblCurrentFile, lblDetails, btnCancel})
            Me.CancelButton = btnCancel
        End Sub

        Public Sub UpdateBackupProgress(report As BackupProgressReport)
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() UpdateBackupProgress(report))
                Return
            End If

            If report Is Nothing Then Return

            progressBar.Value = Math.Max(0, Math.Min(100, report.Percentage))
            If Not String.IsNullOrEmpty(report.StatusMessage) Then
                lblStatus.Text = report.StatusMessage
            End If

            lblCurrentFile.Text = report.CurrentFile
            Dim processedFormatted = UITheme.FormatBytes(report.ProcessedBytes)
            Dim totalFormatted = UITheme.FormatBytes(report.TotalBytes)
            lblDetails.Text = $"{report.ProcessedFiles} of {report.TotalFiles} files ({processedFormatted} / {totalFormatted}) - {report.Percentage}%"
        End Sub

        Public Sub UpdateRestoreProgress(report As RestoreProgressReport)
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() UpdateRestoreProgress(report))
                Return
            End If

            If report Is Nothing Then Return

            progressBar.Value = Math.Max(0, Math.Min(100, report.Percentage))
            If Not String.IsNullOrEmpty(report.StatusMessage) Then
                lblStatus.Text = report.StatusMessage
            End If

            lblCurrentFile.Text = report.CurrentFile
            lblDetails.Text = $"{report.ProcessedFiles} of {report.TotalFiles} files - {report.Percentage}%"
        End Sub

        Public Sub MarkCompleted(success As Boolean, message As String)
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() MarkCompleted(success, message))
                Return
            End If

            _isCompleted = True
            progressBar.Value = 100
            lblStatus.Text = message
            lblStatus.ForeColor = If(success, UITheme.SuccessColor, UITheme.DangerColor)
            lblCurrentFile.Text = String.Empty
            btnCancel.Text = "Close"
            UITheme.ApplyPrimaryButtonStyle(btnCancel)
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            If _isCompleted Then
                Me.DialogResult = DialogResult.OK
                Me.Close()
                Return
            End If

            Dim result = MessageBox.Show(Me, "Are you sure you want to cancel this operation?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                btnCancel.Enabled = False
                lblStatus.Text = "Cancelling operation, please wait..."
                _cts.Cancel()
            End If
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            If Not _isCompleted AndAlso e.CloseReason = CloseReason.UserClosing Then
                Dim result = MessageBox.Show(Me, "An operation is currently in progress. Cancel operation?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If result = DialogResult.Yes Then
                    _cts.Cancel()
                Else
                    e.Cancel = True
                End If
            End If
            MyBase.OnFormClosing(e)
        End Sub
    End Class
End Namespace
