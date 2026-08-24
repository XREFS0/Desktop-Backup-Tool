Imports System.Drawing
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.Dialogs
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Controls
    Public Class RestoreView
        Inherits UserControl

        Private cboRuns As ComboBox
        Private btnRefreshRuns As Button
        Private chkSelectAll As CheckBox
        Private clbFiles As CheckedListBox
        Private txtDestination As TextBox
        Private btnBrowseDest As Button
        Private cboOverwrite As ComboBox
        Private pnlPassword As Panel
        Private txtPassword As TextBox
        Private btnStartRestore As Button
        Private lblRunInfo As Label
        Private _runsList As New List(Of BackupRun)()
        Private _filesList As New List(Of BackupFileRecord)()

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont
            Me.Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Restore Files", .Font = UITheme.HeaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Left, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlTopStep1 As New Panel() With {.Dock = DockStyle.Top, .Height = 70}
            Dim lblStep1 As New Label() With {.Text = "Step 1: Select Backup Source", .Font = UITheme.SubheaderFont, .ForeColor = UITheme.TextPrimary, .Location = New Point(0, 0), .AutoSize = True}
            cboRuns = New ComboBox() With {.Location = New Point(0, 26), .Width = 520, .DropDownStyle = ComboBoxStyle.DropDownList}
            AddHandler cboRuns.SelectedIndexChanged, AddressOf OnRunSelected

            btnRefreshRuns = New Button() With {.Text = "Refresh", .Location = New Point(530, 24), .Size = New Size(80, 28)}
            UITheme.ApplySecondaryButtonStyle(btnRefreshRuns)
            AddHandler btnRefreshRuns.Click, Async Sub() Await LoadRunsAsync()

            lblRunInfo = New Label() With {.Location = New Point(0, 52), .Size = New Size(700, 18), .ForeColor = UITheme.TextSecondary, .Font = UITheme.SmallFont}

            pnlTopStep1.Controls.AddRange({lblStep1, cboRuns, btnRefreshRuns, lblRunInfo})

            Dim pnlStep3 As New Panel() With {.Dock = DockStyle.Bottom, .Height = 180, .Padding = New Padding(0, 10, 0, 0)}
            Dim lblStep3 As New Label() With {.Text = "Step 3: Restore Destination & Options", .Font = UITheme.SubheaderFont, .ForeColor = UITheme.TextPrimary, .Location = New Point(0, 5), .AutoSize = True}

            Dim lblDest As New Label() With {.Text = "Restore to folder:", .Location = New Point(0, 32), .AutoSize = True}
            txtDestination = New TextBox() With {.Location = New Point(0, 54), .Width = 440}
            btnBrowseDest = New Button() With {.Text = "Browse...", .Location = New Point(450, 52), .Size = New Size(90, 27)}
            UITheme.ApplySecondaryButtonStyle(btnBrowseDest)
            AddHandler btnBrowseDest.Click, AddressOf OnBrowseDest

            Dim lblOverwrite As New Label() With {.Text = "Overwrite rule:", .Location = New Point(0, 90), .AutoSize = True}
            cboOverwrite = New ComboBox() With {.Location = New Point(0, 112), .Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboOverwrite.Items.AddRange({"Always Overwrite", "Skip Existing Files", "Overwrite If Newer"})
            cboOverwrite.SelectedIndex = 0

            pnlPassword = New Panel() With {.Location = New Point(240, 90), .Size = New Size(300, 50), .Visible = False}
            Dim lblPwd As New Label() With {.Text = "Decryption Password:", .Location = New Point(0, 0), .AutoSize = True}
            txtPassword = New TextBox() With {.Location = New Point(0, 22), .Width = 220, .UseSystemPasswordChar = True}
            pnlPassword.Controls.AddRange({lblPwd, txtPassword})

            btnStartRestore = New Button() With {.Text = "Start Restore", .Location = New Point(0, 145), .Size = New Size(140, 32)}
            UITheme.ApplyPrimaryButtonStyle(btnStartRestore)
            AddHandler btnStartRestore.Click, AddressOf OnStartRestoreClick

            pnlStep3.Controls.AddRange({lblStep3, lblDest, txtDestination, btnBrowseDest, lblOverwrite, cboOverwrite, pnlPassword, btnStartRestore})

            Dim pnlStep2 As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 8)}
            Dim lblStep2 As New Label() With {.Text = "Step 2: Select Files and Folders to Restore", .Font = UITheme.SubheaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Top, .Height = 24}
            chkSelectAll = New CheckBox() With {.Text = "Select All Files", .Dock = DockStyle.Top, .Height = 24, .Checked = True}
            AddHandler chkSelectAll.CheckedChanged, AddressOf OnSelectAllChanged

            clbFiles = New CheckedListBox() With {.Dock = DockStyle.Fill, .CheckOnClick = True}

            pnlStep2.Controls.Add(clbFiles)
            pnlStep2.Controls.Add(chkSelectAll)
            pnlStep2.Controls.Add(lblStep2)

            Me.Controls.Add(pnlStep2)
            Me.Controls.Add(pnlStep3)
            Me.Controls.Add(pnlTopStep1)
            Me.Controls.Add(pnlHeader)
        End Sub

        Public Async Function LoadRunsAsync(Optional preselectedRunId As Nullable(Of Long) = Nothing) As Task
            Dim container = AppContainer.Instance
            Dim runs = Await container.RunRepository.GetAllAsync().ConfigureAwait(True)
            _runsList = runs.Where(Function(r) r.Status = BackupStatus.Completed OrElse r.Status = BackupStatus.CompletedWithErrors).ToList()

            cboRuns.Items.Clear()
            Dim selectedIdx = -1

            For i As Integer = 0 To _runsList.Count - 1
                Dim run = _runsList(i)
                Dim itemText = $"Run #{run.Id} - {run.JobName} ({run.StartedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}) - {UITheme.FormatBytes(run.CopiedBytes)}"
                cboRuns.Items.Add(itemText)

                If preselectedRunId.HasValue AndAlso run.Id = preselectedRunId.Value Then
                    selectedIdx = i
                End If
            Next

            If cboRuns.Items.Count > 0 Then
                cboRuns.SelectedIndex = If(selectedIdx >= 0, selectedIdx, 0)
            Else
                lblRunInfo.Text = "No completed backup runs found."
                clbFiles.Items.Clear()
            End If
        End Function

        Private Async Sub OnRunSelected(sender As Object, e As EventArgs)
            If cboRuns.SelectedIndex < 0 OrElse cboRuns.SelectedIndex >= _runsList.Count Then
                Return
            End If

            Dim run = _runsList(cboRuns.SelectedIndex)
            lblRunInfo.Text = $"Source Path: {run.DestinationPath} | Compressed: {If(run.IsCompressed, "Yes", "No")} | Encrypted: {If(run.IsEncrypted, "Yes", "No")}"
            pnlPassword.Visible = run.IsEncrypted

            Dim files = Await AppContainer.Instance.FileRepository.GetFilesByRunIdAsync(run.Id)
            _filesList = files.ToList()

            clbFiles.Items.Clear()
            For Each f In _filesList
                clbFiles.Items.Add(f.RelativePath, True)
            Next
            chkSelectAll.Checked = True
        End Sub

        Private Sub OnSelectAllChanged(sender As Object, e As EventArgs)
            Dim isChecked = chkSelectAll.Checked
            For i As Integer = 0 To clbFiles.Items.Count - 1
                clbFiles.SetItemChecked(i, isChecked)
            Next
        End Sub

        Private Sub OnBrowseDest(sender As Object, e As EventArgs)
            Using fbd As New FolderBrowserDialog()
                fbd.Description = "Select Restore Destination Directory"
                fbd.UseDescriptionForTitle = True
                If Directory.Exists(txtDestination.Text) Then
                    fbd.InitialDirectory = txtDestination.Text
                End If
                If fbd.ShowDialog(Me) = DialogResult.OK Then
                    txtDestination.Text = fbd.SelectedPath
                End If
            End Using
        End Sub

        Private Async Sub OnStartRestoreClick(sender As Object, e As EventArgs)
            If cboRuns.SelectedIndex < 0 OrElse cboRuns.SelectedIndex >= _runsList.Count Then
                MessageBox.Show(Me, "Please select a backup run to restore.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If String.IsNullOrWhiteSpace(txtDestination.Text) Then
                MessageBox.Show(Me, "Please select a destination folder for restored files.", "Destination Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim run = _runsList(cboRuns.SelectedIndex)
            Dim selectedPaths As New List(Of String)()
            For Each item In clbFiles.CheckedItems
                selectedPaths.Add(item.ToString())
            Next

            If selectedPaths.Count = 0 Then
                MessageBox.Show(Me, "Please select at least one file to restore.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim request As New RestoreRequest()
            request.RunId = run.Id
            request.DestinationDirectory = txtDestination.Text.Trim()
            request.SelectedRelativePaths = selectedPaths
            request.OverwriteMode = CType(cboOverwrite.SelectedIndex, OverwriteMode)
            request.DecryptionPassword = txtPassword.Text

            Dim cts As New CancellationTokenSource()
            Using progDlg As New ProgressDialog("Restoring Backup Files", cts)
                Dim progress = New Progress(Of RestoreProgressReport)(Sub(rep) progDlg.UpdateRestoreProgress(rep))
                progDlg.Show(Me.FindForm())

                Try
                    Dim success = Await AppContainer.Instance.RestoreEngine.RestoreAsync(request, progress, cts.Token)
                    If success Then
                        progDlg.MarkCompleted(True, $"Restore completed successfully ({selectedPaths.Count} files restored).")
                    Else
                        progDlg.MarkCompleted(False, "Restore was cancelled.")
                    End If
                Catch ex As Exception
                    progDlg.MarkCompleted(False, $"Restore failed: {ex.Message}")
                End Try
            End Using
        End Sub
    End Class
End Namespace
