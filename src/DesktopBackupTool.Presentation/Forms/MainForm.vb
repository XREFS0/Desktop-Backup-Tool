Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.Controls
Imports DesktopBackupTool.Presentation.Dialogs
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Forms
    Public Class MainForm
        Inherits Form

        Private pnlSidebar As Panel
        Private pnlContent As Panel
        Private statusStrip As StatusStrip
        Private statusLabel As ToolStripStatusLabel
        Private schedulerStatusLabel As ToolStripStatusLabel

        Private btnNavDashboard As Button
        Private btnNavJobs As Button
        Private btnNavHistory As Button
        Private btnNavRestore As Button
        Private btnNavLogs As Button
        Private btnNavSettings As Button

        Private viewDashboard As DashboardView
        Private viewJobs As BackupJobsView
        Private viewHistory As BackupHistoryView
        Private viewRestore As RestoreView
        Private viewLogs As LogsView
        Private viewSettings As SettingsView

        Private currentNavButton As Button

        Public Sub New()
            InitializeComponent()
            WireEvents()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Desktop Backup Tool"
            Me.Size = New Size(1100, 720)
            Me.MinimumSize = New Size(950, 600)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Font = UITheme.MainFont
            Me.BackColor = UITheme.ContentBackground

            pnlSidebar = New Panel() With {
                .Dock = DockStyle.Left,
                .Width = 220,
                .BackColor = UITheme.SidebarBackground,
                .Padding = New Padding(0)
            }

            Dim pnlBrand As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 65,
                .BackColor = Color.FromArgb(235, 238, 242),
                .Padding = New Padding(18, 14, 18, 14)
            }

            Dim lblBrandTitle As New Label() With {
                .Text = "Desktop Backup",
                .Font = UITheme.HeaderFont,
                .ForeColor = UITheme.PrimaryColor,
                .Dock = DockStyle.Top,
                .Height = 22
            }

            Dim lblBrandSub As New Label() With {
                .Text = "Enterprise Utility v1.0",
                .Font = UITheme.SmallFont,
                .ForeColor = UITheme.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 16
            }

            pnlBrand.Controls.AddRange({lblBrandSub, lblBrandTitle})

            Dim pnlNavButtons As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(8, 12, 8, 8)}

            btnNavDashboard = CreateNavButton("Dashboard", 0)
            btnNavJobs = CreateNavButton("Backup Jobs", 42)
            btnNavHistory = CreateNavButton("Backup History", 84)
            btnNavRestore = CreateNavButton("Restore", 126)
            btnNavLogs = CreateNavButton("System Logs", 168)
            btnNavSettings = CreateNavButton("Settings", 210)

            pnlNavButtons.Controls.AddRange({btnNavDashboard, btnNavJobs, btnNavHistory, btnNavRestore, btnNavLogs, btnNavSettings})

            pnlSidebar.Controls.Add(pnlNavButtons)
            pnlSidebar.Controls.Add(pnlBrand)

            pnlContent = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = UITheme.ContentBackground
            }

            viewDashboard = New DashboardView()
            viewJobs = New BackupJobsView()
            viewHistory = New BackupHistoryView()
            viewRestore = New RestoreView()
            viewLogs = New LogsView()
            viewSettings = New SettingsView()

            pnlContent.Controls.AddRange({viewDashboard, viewJobs, viewHistory, viewRestore, viewLogs, viewSettings})

            statusStrip = New StatusStrip() With {.BackColor = Color.FromArgb(243, 244, 246)}
            statusLabel = New ToolStripStatusLabel("Ready") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft, .ForeColor = UITheme.TextPrimary}
            schedulerStatusLabel = New ToolStripStatusLabel("Scheduler: Active") With {.ForeColor = UITheme.SuccessColor}
            statusStrip.Items.AddRange({statusLabel, schedulerStatusLabel})

            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlSidebar)
            Me.Controls.Add(statusStrip)
        End Sub

        Private Function CreateNavButton(text As String, top As Integer) As Button
            Dim btn As New Button() With {
                .Text = text,
                .Location = New Point(0, top),
                .Size = New Size(204, 38),
                .FlatStyle = FlatStyle.Flat,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Padding = New Padding(12, 0, 0, 0),
                .Font = UITheme.BoldFont,
                .ForeColor = UITheme.TextPrimary,
                .BackColor = Color.Transparent,
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 0
            AddHandler btn.Click, AddressOf OnNavButtonClick
            Return btn
        End Function

        Private Sub WireEvents()
            AddHandler viewDashboard.NavigateRequested, AddressOf OnDashboardNavigate
            AddHandler viewDashboard.RunJobRequested, AddressOf OnExecuteJobRequested
            AddHandler viewJobs.RunJobRequested, AddressOf OnExecuteJobRequested
            AddHandler viewHistory.RestoreFromRunRequested, AddressOf OnRestoreFromRun
            AddHandler AppContainer.Instance.SchedulerService.JobTriggered, AddressOf OnSchedulerJobTriggered
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Await AppContainer.Instance.InitializeAsync()
                SelectView("Dashboard", btnNavDashboard)
            Catch ex As Exception
                MessageBox.Show(Me, $"Failed to initialize application database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub OnNavButtonClick(sender As Object, e As EventArgs)
            Dim btn = CType(sender, Button)
            If btn Is btnNavDashboard Then
                SelectView("Dashboard", btn)
            ElseIf btn Is btnNavJobs Then
                SelectView("BackupJobs", btn)
            ElseIf btn Is btnNavHistory Then
                SelectView("History", btn)
            ElseIf btn Is btnNavRestore Then
                SelectView("Restore", btn)
            ElseIf btn Is btnNavLogs Then
                SelectView("Logs", btn)
            ElseIf btn Is btnNavSettings Then
                SelectView("Settings", btn)
            End If
        End Sub

        Private Sub OnDashboardNavigate(sender As Object, viewName As String)
            Select Case viewName
                Case "BackupJobs"
                    SelectView("BackupJobs", btnNavJobs)
                Case "Restore"
                    SelectView("Restore", btnNavRestore)
                Case "Logs"
                    SelectView("Logs", btnNavLogs)
            End Select
        End Sub

        Private Async Sub OnRestoreFromRun(sender As Object, runId As Long)
            SelectView("Restore", btnNavRestore)
            Await viewRestore.LoadRunsAsync(runId)
        End Sub

        Private Async Sub SelectView(viewName As String, navBtn As Button)
            If currentNavButton IsNot Nothing Then
                currentNavButton.BackColor = Color.Transparent
                currentNavButton.ForeColor = UITheme.TextPrimary
            End If

            currentNavButton = navBtn
            If currentNavButton IsNot Nothing Then
                currentNavButton.BackColor = UITheme.SidebarSelected
                currentNavButton.ForeColor = UITheme.PrimaryColor
            End If

            viewDashboard.Visible = (viewName = "Dashboard")
            viewJobs.Visible = (viewName = "BackupJobs")
            viewHistory.Visible = (viewName = "History")
            viewRestore.Visible = (viewName = "Restore")
            viewLogs.Visible = (viewName = "Logs")
            viewSettings.Visible = (viewName = "Settings")

            Select Case viewName
                Case "Dashboard"
                    Await viewDashboard.LoadDataAsync()
                Case "BackupJobs"
                    Await viewJobs.LoadJobsAsync()
                Case "History"
                    Await viewHistory.LoadHistoryAsync()
                Case "Restore"
                    Await viewRestore.LoadRunsAsync()
                Case "Logs"
                    Await viewLogs.LoadLogsAsync()
                Case "Settings"
                    Await viewSettings.LoadSettingsAsync()
            End Select
        End Sub

        Private Sub OnExecuteJobRequested(sender As Object, job As BackupJob)
            ExecuteBackupJob(job)
        End Sub

        Private Sub OnSchedulerJobTriggered(sender As Object, job As BackupJob)
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() ExecuteBackupJob(job))
            Else
                ExecuteBackupJob(job)
            End If
        End Sub

        Public Async Sub ExecuteBackupJob(job As BackupJob)
            If job Is Nothing Then Return

            If AppContainer.Instance.BackupEngine.IsRunning Then
                MessageBox.Show(Me, "Another backup operation is already running. Please wait for it to complete.", "Engine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            statusLabel.Text = $"Running backup for '{job.Name}'..."
            Dim cts As New CancellationTokenSource()

            Using progDlg As New ProgressDialog($"Running Backup: {job.Name}", cts)
                Dim progress = New Progress(Of BackupProgressReport)(Sub(rep) progDlg.UpdateBackupProgress(rep))
                progDlg.Show(Me)

                Try
                    Dim run = Await AppContainer.Instance.BackupEngine.RunBackupAsync(job, progress, cts.Token)
                    If run.Status = BackupStatus.Completed Then
                        progDlg.MarkCompleted(True, $"Backup completed successfully ({run.CopiedFiles} copied, {run.SkippedFiles} skipped, {UITheme.FormatBytes(run.CopiedBytes)}).")
                    ElseIf run.Status = BackupStatus.CompletedWithErrors Then
                        progDlg.MarkCompleted(False, $"Backup completed with warnings ({run.FailedFiles} files failed).")
                    ElseIf run.Status = BackupStatus.Cancelled Then
                        progDlg.MarkCompleted(False, "Backup was cancelled.")
                    Else
                        progDlg.MarkCompleted(False, $"Backup failed: {run.ErrorMessage}")
                    End If
                Catch ex As Exception
                    progDlg.MarkCompleted(False, $"Backup error: {ex.Message}")
                Finally
                    statusLabel.Text = "Ready"
                End Try

                If viewDashboard.Visible Then
                    Await viewDashboard.LoadDataAsync()
                ElseIf viewJobs.Visible Then
                    Await viewJobs.LoadJobsAsync()
                ElseIf viewHistory.Visible Then
                    Await viewHistory.LoadHistoryAsync()
                End If
            End Using
        End Sub
    End Class
End Namespace
