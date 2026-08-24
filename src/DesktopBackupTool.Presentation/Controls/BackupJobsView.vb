Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.Dialogs
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Controls
    Public Class BackupJobsView
        Inherits UserControl

        Private gridJobs As DataGridView
        Private btnNew As Button
        Private btnRun As Button
        Private btnEdit As Button
        Private btnDuplicate As Button
        Private btnToggle As Button
        Private btnDelete As Button
        Private btnRefresh As Button
        Private lblEmpty As Label
        Private _jobsList As New List(Of BackupJob)()

        Public Event RunJobRequested(sender As Object, job As BackupJob)

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont
            Me.Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Backup Jobs", .Font = UITheme.HeaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Left, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlToolbar As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            btnNew = New Button() With {.Text = "+ New Job", .Location = New Point(0, 5), .Size = New Size(95, 32)}
            UITheme.ApplyPrimaryButtonStyle(btnNew)
            AddHandler btnNew.Click, AddressOf OnNewJob

            btnRun = New Button() With {.Text = "Run Now", .Location = New Point(105, 5), .Size = New Size(90, 32)}
            UITheme.ApplySecondaryButtonStyle(btnRun)
            AddHandler btnRun.Click, AddressOf OnRunJob

            btnEdit = New Button() With {.Text = "Edit", .Location = New Point(205, 5), .Size = New Size(75, 32)}
            UITheme.ApplySecondaryButtonStyle(btnEdit)
            AddHandler btnEdit.Click, AddressOf OnEditJob

            btnDuplicate = New Button() With {.Text = "Duplicate", .Location = New Point(290, 5), .Size = New Size(85, 32)}
            UITheme.ApplySecondaryButtonStyle(btnDuplicate)
            AddHandler btnDuplicate.Click, AddressOf OnDuplicateJob

            btnToggle = New Button() With {.Text = "Enable/Disable", .Location = New Point(385, 5), .Size = New Size(115, 32)}
            UITheme.ApplySecondaryButtonStyle(btnToggle)
            AddHandler btnToggle.Click, AddressOf OnToggleJob

            btnDelete = New Button() With {.Text = "Delete", .Location = New Point(510, 5), .Size = New Size(80, 32)}
            UITheme.ApplyDangerButtonStyle(btnDelete)
            AddHandler btnDelete.Click, AddressOf OnDeleteJob

            btnRefresh = New Button() With {.Text = "Refresh", .Location = New Point(600, 5), .Size = New Size(80, 32)}
            UITheme.ApplySecondaryButtonStyle(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub() Await LoadJobsAsync()

            pnlToolbar.Controls.AddRange({btnNew, btnRun, btnEdit, btnDuplicate, btnToggle, btnDelete, btnRefresh})

            Dim pnlGridContainer As New Panel() With {.Dock = DockStyle.Fill}
            gridJobs = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridJobs)

            gridJobs.Columns.Add("Id", "ID")
            gridJobs.Columns.Add("Name", "Job Name")
            gridJobs.Columns.Add("Source", "Source Directory")
            gridJobs.Columns.Add("Destination", "Destination")
            gridJobs.Columns.Add("Type", "Type")
            gridJobs.Columns.Add("Schedule", "Schedule")
            gridJobs.Columns.Add("LastRun", "Last Run")
            gridJobs.Columns.Add("Status", "Last Status")
            gridJobs.Columns.Add("Enabled", "Enabled")

            gridJobs.Columns(0).Visible = False
            gridJobs.Columns(1).Width = 160
            gridJobs.Columns(2).Width = 180
            gridJobs.Columns(3).Width = 180
            gridJobs.Columns(4).Width = 90
            gridJobs.Columns(5).Width = 110
            gridJobs.Columns(6).Width = 140
            gridJobs.Columns(7).Width = 110
            gridJobs.Columns(8).Width = 70

            lblEmpty = New Label() With {
                .Text = "No backup jobs configured yet. Click '+ New Job' to create one.",
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter,
                .ForeColor = UITheme.TextSecondary,
                .Visible = False
            }

            pnlGridContainer.Controls.Add(gridJobs)
            pnlGridContainer.Controls.Add(lblEmpty)

            Me.Controls.Add(pnlGridContainer)
            Me.Controls.Add(pnlToolbar)
            Me.Controls.Add(pnlHeader)
        End Sub

        Public Async Function LoadJobsAsync() As Task
            Dim container = AppContainer.Instance
            Dim jobs = Await container.JobService.GetAllJobsAsync().ConfigureAwait(True)
            _jobsList = jobs.ToList()

            gridJobs.Rows.Clear()
            If _jobsList.Count = 0 Then
                lblEmpty.Visible = True
                gridJobs.Visible = False
            Else
                lblEmpty.Visible = False
                gridJobs.Visible = True

                For Each job In _jobsList
                    Dim scheduleDisplay = job.ScheduleType.ToString()
                    If job.ScheduleType = ScheduleType.Daily Then
                        scheduleDisplay = $"Daily @ {job.ScheduleTime}"
                    ElseIf job.ScheduleType = ScheduleType.Weekly Then
                        scheduleDisplay = $"Weekly @ {job.ScheduleTime}"
                    ElseIf job.ScheduleType = ScheduleType.IntervalHours Then
                        scheduleDisplay = $"Every {job.ScheduleIntervalHours}h"
                    End If

                    Dim lastRunStr = If(job.LastRunUtc.HasValue, job.LastRunUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), "Never")
                    Dim statusStr = If(job.LastStatus.HasValue, job.LastStatus.Value.ToString(), "None")

                    Dim rowIndex = gridJobs.Rows.Add(
                        job.Id,
                        job.Name,
                        job.SourcePath,
                        job.DestinationPath,
                        job.BackupType.ToString(),
                        scheduleDisplay,
                        lastRunStr,
                        statusStr,
                        If(job.IsEnabled, "Yes", "No")
                    )

                    Dim row = gridJobs.Rows(rowIndex)
                    If Not job.IsEnabled Then
                        row.DefaultCellStyle.ForeColor = Color.Gray
                    ElseIf job.LastStatus.HasValue Then
                        If job.LastStatus.Value = BackupStatus.Failed Then
                            row.DefaultCellStyle.ForeColor = UITheme.DangerColor
                        ElseIf job.LastStatus.Value = BackupStatus.Completed Then
                            row.DefaultCellStyle.ForeColor = UITheme.SuccessColor
                        End If
                    End If
                Next
            End If
        End Function

        Private Function GetSelectedJob() As BackupJob
            If gridJobs.SelectedRows.Count = 0 Then Return Nothing
            Dim id = Convert.ToInt64(gridJobs.SelectedRows(0).Cells("Id").Value)
            Return _jobsList.FirstOrDefault(Function(j) j.Id = id)
        End Function

        Private Async Sub OnNewJob(sender As Object, e As EventArgs)
            Dim newJob As New BackupJob()
            Using dlg As New JobEditorDialog(newJob, AppContainer.Instance.EncryptionService)
                If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                    Dim validation = Await AppContainer.Instance.JobService.SaveJobAsync(dlg.Job)
                    If Not validation.IsValid Then
                        MessageBox.Show(Me, String.Join(Environment.NewLine, validation.Errors), "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Else
                        Await LoadJobsAsync()
                    End If
                End If
            End Using
        End Sub

        Private Async Sub OnEditJob(sender As Object, e As EventArgs)
            Dim selected = GetSelectedJob()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup job to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dlg As New JobEditorDialog(selected, AppContainer.Instance.EncryptionService)
                If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                    Dim validation = Await AppContainer.Instance.JobService.SaveJobAsync(dlg.Job)
                    If Not validation.IsValid Then
                        MessageBox.Show(Me, String.Join(Environment.NewLine, validation.Errors), "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Else
                        Await LoadJobsAsync()
                    End If
                End If
            End Using
        End Sub

        Private Sub OnRunJob(sender As Object, e As EventArgs)
            Dim selected = GetSelectedJob()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup job to run.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            RaiseEvent RunJobRequested(Me, selected)
        End Sub

        Private Async Sub OnDuplicateJob(sender As Object, e As EventArgs)
            Dim selected = GetSelectedJob()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup job to duplicate.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Await AppContainer.Instance.JobService.DuplicateJobAsync(selected.Id)
            Await LoadJobsAsync()
        End Sub

        Private Async Sub OnToggleJob(sender As Object, e As EventArgs)
            Dim selected = GetSelectedJob()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup job to toggle.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Await AppContainer.Instance.JobService.ToggleEnabledAsync(selected.Id)
            Await LoadJobsAsync()
        End Sub

        Private Async Sub OnDeleteJob(sender As Object, e As EventArgs)
            Dim selected = GetSelectedJob()
            If selected Is Nothing Then
                MessageBox.Show(Me, "Please select a backup job to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim result = MessageBox.Show(Me, $"Are you sure you want to permanently delete the backup job '{selected.Name}'?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If result = DialogResult.Yes Then
                Await AppContainer.Instance.JobService.DeleteJobAsync(selected.Id)
                Await LoadJobsAsync()
            End If
        End Sub
    End Class
End Namespace
