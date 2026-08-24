Imports System.Drawing
Imports System.Windows.Forms
Imports DesktopBackupTool.Application.DTOs
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Controls
    Public Class DashboardView
        Inherits UserControl

        Private lblTotalJobsVal As Label
        Private lblActiveJobsVal As Label
        Private lblTotalSizeVal As Label
        Private lblLastBackupVal As Label
        Private lblStatusBanner As Label

        Private gridRecent As DataGridView
        Private lblNoActivity As Label

        Private btnNewJob As Button
        Private btnRunAll As Button
        Private btnRestore As Button
        Private btnLogs As Button
        Private btnRefresh As Button

        Public Event NavigateRequested(sender As Object, viewName As String)
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
            Dim lblTitle As New Label() With {.Text = "Dashboard", .Font = UITheme.HeaderFont, .ForeColor = UITheme.TextPrimary, .Location = New Point(0, 0), .AutoSize = True}
            btnRefresh = New Button() With {.Text = "Refresh", .Location = New Point(680, 0), .Size = New Size(80, 30)}
            UITheme.ApplySecondaryButtonStyle(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub() Await LoadDataAsync()

            pnlHeader.Controls.AddRange({lblTitle, btnRefresh})

            Dim pnlCards As New Panel() With {.Dock = DockStyle.Top, .Height = 90}
            Dim cardWidth = 180
            Dim cardHeight = 80
            Dim gap = 16

            Dim pnlCard1 = CreateStatCard("TOTAL JOBS", "0", 0, cardWidth, cardHeight, lblTotalJobsVal)
            Dim pnlCard2 = CreateStatCard("ACTIVE JOBS", "0", (cardWidth + gap) * 1, cardWidth, cardHeight, lblActiveJobsVal)
            Dim pnlCard3 = CreateStatCard("TOTAL SIZE", "0 B", (cardWidth + gap) * 2, cardWidth, cardHeight, lblTotalSizeVal)
            Dim pnlCard4 = CreateStatCard("LAST BACKUP", "Never", (cardWidth + gap) * 3, cardWidth, cardHeight, lblLastBackupVal)

            pnlCards.Controls.AddRange({pnlCard1, pnlCard2, pnlCard3, pnlCard4})

            lblStatusBanner = New Label() With {
                .Dock = DockStyle.Top,
                .Height = 36,
                .Font = UITheme.BoldFont,
                .ForeColor = UITheme.PrimaryColor,
                .BackColor = Color.FromArgb(240, 246, 255),
                .TextAlign = ContentAlignment.MiddleLeft,
                .Padding = New Padding(12, 0, 0, 0),
                .Text = "Status: Backup engine is active and ready."
            }

            Dim pnlQuickActions As New Panel() With {.Dock = DockStyle.Top, .Height = 55, .Padding = New Padding(0, 12, 0, 0)}
            btnNewJob = New Button() With {.Text = "+ Create New Job", .Location = New Point(0, 14), .Size = New Size(140, 32)}
            UITheme.ApplyPrimaryButtonStyle(btnNewJob)
            AddHandler btnNewJob.Click, Sub() RaiseEvent NavigateRequested(Me, "BackupJobs")

            btnRunAll = New Button() With {.Text = "Run All Active Jobs", .Location = New Point(150, 14), .Size = New Size(145, 32)}
            UITheme.ApplySecondaryButtonStyle(btnRunAll)
            AddHandler btnRunAll.Click, AddressOf OnRunAllClick

            btnRestore = New Button() With {.Text = "Restore Files", .Location = New Point(305, 14), .Size = New Size(120, 32)}
            UITheme.ApplySecondaryButtonStyle(btnRestore)
            AddHandler btnRestore.Click, Sub() RaiseEvent NavigateRequested(Me, "Restore")

            btnLogs = New Button() With {.Text = "View System Logs", .Location = New Point(435, 14), .Size = New Size(135, 32)}
            UITheme.ApplySecondaryButtonStyle(btnLogs)
            AddHandler btnLogs.Click, Sub() RaiseEvent NavigateRequested(Me, "Logs")

            pnlQuickActions.Controls.AddRange({btnNewJob, btnRunAll, btnRestore, btnLogs})

            Dim pnlActivityHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 35, .Padding = New Padding(0, 10, 0, 0)}
            Dim lblRecent As New Label() With {.Text = "Recent Backup Activity", .Font = UITheme.SubheaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Left, .AutoSize = True}
            pnlActivityHeader.Controls.Add(lblRecent)

            Dim pnlGridContainer As New Panel() With {.Dock = DockStyle.Fill}
            gridRecent = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridRecent)
            gridRecent.Columns.Add("Job", "Job Name")
            gridRecent.Columns.Add("Type", "Type")
            gridRecent.Columns.Add("Status", "Status")
            gridRecent.Columns.Add("Time", "Date & Time")
            gridRecent.Columns.Add("Duration", "Duration")
            gridRecent.Columns.Add("Files", "Files Copied")
            gridRecent.Columns.Add("Size", "Copied Size")
            gridRecent.Columns(0).Width = 170
            gridRecent.Columns(1).Width = 85
            gridRecent.Columns(2).Width = 100
            gridRecent.Columns(3).Width = 150
            gridRecent.Columns(4).Width = 85
            gridRecent.Columns(5).Width = 95
            gridRecent.Columns(6).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            lblNoActivity = New Label() With {
                .Text = "No backup activity recorded yet. Create a backup job to get started.",
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter,
                .ForeColor = UITheme.TextSecondary,
                .Visible = False
            }

            pnlGridContainer.Controls.Add(gridRecent)
            pnlGridContainer.Controls.Add(lblNoActivity)

            Me.Controls.Add(pnlGridContainer)
            Me.Controls.Add(pnlActivityHeader)
            Me.Controls.Add(pnlQuickActions)
            Me.Controls.Add(lblStatusBanner)
            Me.Controls.Add(pnlCards)
            Me.Controls.Add(pnlHeader)
        End Sub

        Private Function CreateStatCard(title As String, defaultValue As String, xPos As Integer, width As Integer, height As Integer, ByRef outValLabel As Label) As Panel
            Dim pnl As New Panel() With {
                .Location = New Point(xPos, 0),
                .Size = New Size(width, height),
                .BackColor = UITheme.CardBackground,
                .BorderStyle = BorderStyle.FixedSingle
            }

            Dim lblTitle As New Label() With {
                .Text = title,
                .Font = UITheme.SmallFont,
                .ForeColor = UITheme.TextSecondary,
                .Location = New Point(10, 8),
                .AutoSize = True
            }

            outValLabel = New Label() With {
                .Text = defaultValue,
                .Font = UITheme.StatValueFont,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(8, 28),
                .Size = New Size(width - 16, 32),
                .AutoEllipsis = True
            }

            pnl.Controls.AddRange({lblTitle, outValLabel})
            Return pnl
        End Function

        Public Async Function LoadDataAsync() As Task
            Dim container = AppContainer.Instance
            Dim stats = Await container.JobService.GetDashboardStatsAsync().ConfigureAwait(True)

            lblTotalJobsVal.Text = stats.TotalJobs.ToString()
            lblActiveJobsVal.Text = stats.EnabledJobs.ToString()
            lblTotalSizeVal.Text = UITheme.FormatBytes(stats.TotalBackupSizeBytes)

            If stats.LastSuccessfulBackup.HasValue Then
                lblLastBackupVal.Text = stats.LastSuccessfulBackup.Value.ToLocalTime().ToString("MMM dd, HH:mm")
            Else
                lblLastBackupVal.Text = "Never"
            End If

            If container.BackupEngine.IsRunning Then
                lblStatusBanner.Text = "Status: Backup operation currently in progress..."
                lblStatusBanner.ForeColor = UITheme.WarningColor
                lblStatusBanner.BackColor = UITheme.WarningBg
            Else
                lblStatusBanner.Text = "Status: Backup engine is active and ready."
                lblStatusBanner.ForeColor = UITheme.PrimaryColor
                lblStatusBanner.BackColor = Color.FromArgb(240, 246, 255)
            End If

            gridRecent.Rows.Clear()
            If stats.RecentRuns.Count = 0 Then
                lblNoActivity.Visible = True
                gridRecent.Visible = False
            Else
                lblNoActivity.Visible = False
                gridRecent.Visible = True

                For Each run In stats.RecentRuns
                    Dim durationStr = $"{run.Duration.TotalSeconds:F1}s"
                    Dim rowIndex = gridRecent.Rows.Add(
                        run.JobName,
                        run.BackupType.ToString(),
                        run.Status.ToString(),
                        run.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                        durationStr,
                        $"{run.CopiedFiles} / {run.TotalFiles}",
                        UITheme.FormatBytes(run.CopiedBytes)
                    )

                    Dim row = gridRecent.Rows(rowIndex)
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

        Private Async Sub OnRunAllClick(sender As Object, e As EventArgs)
            Dim container = AppContainer.Instance
            Dim enabledJobs = Await container.JobRepository.GetEnabledJobsAsync()
            If enabledJobs.Count = 0 Then
                MessageBox.Show(Me, "There are no active enabled backup jobs configured.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            For Each job In enabledJobs
                RaiseEvent RunJobRequested(Me, job)
            Next
        End Sub
    End Class
End Namespace
