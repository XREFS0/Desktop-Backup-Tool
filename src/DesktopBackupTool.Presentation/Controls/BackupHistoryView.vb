Imports System.Drawing
Imports System.Windows.Forms
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.Dialogs
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Controls
    Public Class BackupHistoryView
        Inherits UserControl

        Private gridRuns As DataGridView
        Private btnVerify As Button
        Private btnRestore As Button
        Private btnDelete As Button
        Private btnRefresh As Button
        Private lblDetails As Label
        Private gridFiles As DataGridView
        Private lblEmpty As Label
        Private _runsList As New List(Of BackupRun)()

        Public Event RestoreFromRunRequested(sender As Object, runId As Long)

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont
            Me.Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Backup History", .Font = UITheme.HeaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Left, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlToolbar As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            btnVerify = New Button() With {.Text = "Verify Integrity", .Location = New Point(0, 5), .Size = New Size(130, 32)}
            UITheme.ApplyPrimaryButtonStyle(btnVerify)
            AddHandler btnVerify.Click, AddressOf OnVerifyClick

            btnRestore = New Button() With {.Text = "Restore From Selected", .Location = New Point(140, 5), .Size = New Size(160, 32)}
            UITheme.ApplySecondaryButtonStyle(btnRestore)
            AddHandler btnRestore.Click, AddressOf OnRestoreClick

            btnDelete = New Button() With {.Text = "Delete Record", .Location = New Point(310, 5), .Size = New Size(110, 32)}
            UITheme.ApplyDangerButtonStyle(btnDelete)
            AddHandler btnDelete.Click, AddressOf OnDeleteClick

            btnRefresh = New Button() With {.Text = "Refresh", .Location = New Point(430, 5), .Size = New Size(80, 32)}
            UITheme.ApplySecondaryButtonStyle(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub() Await LoadHistoryAsync()

            pnlToolbar.Controls.AddRange({btnVerify, btnRestore, btnDelete, btnRefresh})

            Dim splitContainer As New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Horizontal,
                .SplitterDistance = 250,
                .SplitterWidth = 6
            }

            gridRuns = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridRuns)
            gridRuns.Columns.Add("Id", "Run ID")
            gridRuns.Columns.Add("JobName", "Job Name")
            gridRuns.Columns.Add("Type", "Type")
            gridRuns.Columns.Add("Status", "Status")
            gridRuns.Columns.Add("Started", "Started At")
            gridRuns.Columns.Add("Duration", "Duration")
            gridRuns.Columns.Add("Files", "Copied / Total")
            gridRuns.Columns.Add("Size", "Copied Size")
            gridRuns.Columns.Add("Destination", "Destination Path")

            gridRuns.Columns(0).Width = 70
            gridRuns.Columns(1).Width = 150
            gridRuns.Columns(2).Width = 80
            gridRuns.Columns(3).Width = 100
            gridRuns.Columns(4).Width = 140
            gridRuns.Columns(5).Width = 80
            gridRuns.Columns(6).Width = 100
            gridRuns.Columns(7).Width = 95
            gridRuns.Columns(8).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            AddHandler gridRuns.SelectionChanged, AddressOf OnRunSelectionChanged

            lblEmpty = New Label() With {
                .Text = "No backup history recorded yet.",
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter,
                .ForeColor = UITheme.TextSecondary,
                .Visible = False
            }

            splitContainer.Panel1.Controls.Add(gridRuns)
            splitContainer.Panel1.Controls.Add(lblEmpty)

            Dim pnlDetailsContainer As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}
            lblDetails = New Label() With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Font = UITheme.BoldFont,
                .ForeColor = UITheme.TextPrimary,
                .Text = "Backup Manifest Details"
            }

            gridFiles = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridFiles)
            gridFiles.Columns.Add("Path", "Relative Path")
            gridFiles.Columns.Add("Size", "File Size")
            gridFiles.Columns.Add("Modified", "Last Modified")
            gridFiles.Columns.Add("Hash", "SHA-256 Checksum")

            gridFiles.Columns(0).Width = 260
            gridFiles.Columns(1).Width = 100
            gridFiles.Columns(2).Width = 140
            gridFiles.Columns(3).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            pnlDetailsContainer.Controls.Add(gridFiles)
            pnlDetailsContainer.Controls.Add(lblDetails)

            splitContainer.Panel2.Controls.Add(pnlDetailsContainer)

            Me.Controls.Add(splitContainer)
            Me.Controls.Add(pnlToolbar)
            Me.Controls.Add(pnlHeader)
        End Sub

        Public Async Function LoadHistoryAsync() As Task
            Dim container = AppContainer.Instance
            Dim runs = Await container.RunRepository.GetAllAsync().ConfigureAwait(True)
            _runsList = runs.ToList()

            gridRuns.Rows.Clear()
            gridFiles.Rows.Clear()

            If _runsList.Count = 0 Then
                lblEmpty.Visible = True
                gridRuns.Visible = False
            Else
                lblEmpty.Visible = False
                gridRuns.Visible = True

                For Each run In _runsList
                    Dim rowIndex = gridRuns.Rows.Add(
                        run.Id,
                        run.JobName,
                        run.BackupType.ToString(),
                        run.Status.ToString(),
                        run.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                        $"{run.Duration.TotalSeconds:F1}s",
                        $"{run.CopiedFiles} / {run.TotalFiles}",
                        UITheme.FormatBytes(run.CopiedBytes),
                        run.DestinationPath
                    )

                    Dim row = gridRuns.Rows(rowIndex)
                    If run.Status = BackupStatus.Failed Then
                        row.DefaultCellStyle.ForeColor = UITheme.DangerColor
                    ElseIf run.Status = BackupStatus.CompletedWithErrors Then
                        row.DefaultCellStyle.ForeColor = UITheme.WarningColor
                    ElseIf run.Status = BackupStatus.Completed Then
                        row.DefaultCellStyle.ForeColor = UITheme.SuccessColor
                    End If
                Next
            End If
        End Function

        Private Function GetSelectedRun() As BackupRun
            If gridRuns.SelectedRows.Count = 0 Then Return Nothing
            Dim id = Convert.ToInt64(gridRuns.SelectedRows(0).Cells("Id").Value)
            Return _runsList.FirstOrDefault(Function(r) r.Id = id)
        End Function

        Private Async Sub OnRunSelectionChanged(sender As Object, e As EventArgs)
            Dim selected = GetSelectedRun()
            gridFiles.Rows.Clear()
            If selected Is Nothing Then
                lblDetails.Text = "Backup Manifest Details"
                Return
            End If

            lblDetails.Text = $"Backup Manifest: Run #{selected.Id} ({selected.JobName}) - {selected.DestinationPath}"
            Dim files = Await AppContainer.Instance.FileRepository.GetFilesByRunIdAsync(selected.Id)
            For Each f In files
                gridFiles.Rows.Add(
                    f.RelativePath,
                    UITheme.FormatBytes(f.FileSizeBytes),
                    f.LastModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    f.Sha256Hash
                )
            Next
        End Sub

        Private Async Sub OnVerifyClick(sender As Object, e As EventArgs)
            Dim selected = GetSelectedRun()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup run to verify.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            btnVerify.Enabled = False
            Try
                Dim result = Await AppContainer.Instance.VerificationService.VerifyRunAsync(selected.Id)
                Using dlg As New VerificationReportDialog(result)
                    dlg.ShowDialog(Me.FindForm())
                End Using
            Finally
                btnVerify.Enabled = True
            End Try
        End Sub

        Private Sub OnRestoreClick(sender As Object, e As EventArgs)
            Dim selected = GetSelectedRun()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup run to restore.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            RaiseEvent RestoreFromRunRequested(Me, selected.Id)
        End Sub

        Private Async Sub OnDeleteClick(sender As Object, e As EventArgs)
            Dim selected = GetSelectedRun()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup run record to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim result = MessageBox.Show(Me, $"Are you sure you want to delete the record for backup run #{selected.Id}? Note: This only deletes database metadata, not physical files.", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Await AppContainer.Instance.RunRepository.DeleteAsync(selected.Id)
                Await LoadHistoryAsync()
            End If
        End Sub
    End Class
End Namespace
