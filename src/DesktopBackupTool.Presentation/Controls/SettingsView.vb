Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Controls
    Public Class SettingsView
        Inherits UserControl

        Private lblDbPath As Label
        Private lblDbSize As Label
        Private btnVacuum As Button
        Private numDefaultRetention As NumericUpDown
        Private cboDefaultCompression As ComboBox
        Private chkAutoStartScheduler As CheckBox
        Private btnSaveSettings As Button
        Private lblAppInfo As Label

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont
            Me.Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Application Settings", .Font = UITheme.HeaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Left, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlContent As New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True}

            Dim grpDb As New GroupBox() With {.Text = "Database & Storage", .Location = New Point(0, 10), .Size = New Size(650, 130), .Font = UITheme.BoldFont}
            Dim lblDbPathTitle As New Label() With {.Text = "Database Location:", .Location = New Point(16, 28), .AutoSize = True, .Font = UITheme.MainFont}
            lblDbPath = New Label() With {.Text = "", .Location = New Point(150, 28), .Size = New Size(480, 20), .AutoEllipsis = True, .Font = UITheme.MainFont, .ForeColor = UITheme.TextSecondary}

            Dim lblDbSizeTitle As New Label() With {.Text = "Database Size:", .Location = New Point(16, 56), .AutoSize = True, .Font = UITheme.MainFont}
            lblDbSize = New Label() With {.Text = "", .Location = New Point(150, 56), .Size = New Size(200, 20), .Font = UITheme.MainFont, .ForeColor = UITheme.TextSecondary}

            btnVacuum = New Button() With {.Text = "Optimize & Vacuum Database", .Location = New Point(16, 85), .Size = New Size(210, 30)}
            UITheme.ApplySecondaryButtonStyle(btnVacuum)
            AddHandler btnVacuum.Click, AddressOf OnVacuumClick

            grpDb.Controls.AddRange({lblDbPathTitle, lblDbPath, lblDbSizeTitle, lblDbSize, btnVacuum})

            Dim grpDefaults As New GroupBox() With {.Text = "Backup Defaults", .Location = New Point(0, 155), .Size = New Size(650, 160), .Font = UITheme.BoldFont}

            Dim lblRetTitle As New Label() With {.Text = "Default Retention Count:", .Location = New Point(16, 30), .AutoSize = True, .Font = UITheme.MainFont}
            numDefaultRetention = New NumericUpDown() With {.Location = New Point(200, 28), .Minimum = 1, .Maximum = 1000, .Value = 5, .Width = 100, .Font = UITheme.MainFont}

            Dim lblCompTitle As New Label() With {.Text = "Default Compression Level:", .Location = New Point(16, 68), .AutoSize = True, .Font = UITheme.MainFont}
            cboDefaultCompression = New ComboBox() With {.Location = New Point(200, 66), .Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.MainFont}
            cboDefaultCompression.Items.AddRange({"None", "Fast", "Optimal"})
            cboDefaultCompression.SelectedIndex = 0

            chkAutoStartScheduler = New CheckBox() With {.Text = "Enable automatic background scheduling on startup", .Location = New Point(16, 110), .AutoSize = True, .Checked = True, .Font = UITheme.MainFont}

            grpDefaults.Controls.AddRange({lblRetTitle, numDefaultRetention, lblCompTitle, cboDefaultCompression, chkAutoStartScheduler})

            Dim grpAppInfo As New GroupBox() With {.Text = "System & Environment", .Location = New Point(0, 330), .Size = New Size(650, 100), .Font = UITheme.BoldFont}
            lblAppInfo = New Label() With {.Dock = DockStyle.Fill, .Font = UITheme.MainFont, .Padding = New Padding(12), .ForeColor = UITheme.TextSecondary}
            grpAppInfo.Controls.Add(lblAppInfo)

            btnSaveSettings = New Button() With {.Text = "Save Settings", .Location = New Point(0, 445), .Size = New Size(130, 34)}
            UITheme.ApplyPrimaryButtonStyle(btnSaveSettings)
            AddHandler btnSaveSettings.Click, AddressOf OnSaveSettingsClick

            pnlContent.Controls.AddRange({grpDb, grpDefaults, grpAppInfo, btnSaveSettings})

            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlHeader)
        End Sub

        Public Async Function LoadSettingsAsync() As Task
            Dim container = AppContainer.Instance
            Dim dbPath = container.DbContext.DatabasePath
            lblDbPath.Text = dbPath

            If File.Exists(dbPath) Then
                Dim fi As New FileInfo(dbPath)
                lblDbSize.Text = UITheme.FormatBytes(fi.Length)
            Else
                lblDbSize.Text = "0 B"
            End If

            Dim retValStr = Await container.SettingsRepository.GetValueAsync("DefaultRetention", "5")
            Dim retVal As Integer
            If Integer.TryParse(retValStr, retVal) Then
                numDefaultRetention.Value = Math.Max(1, Math.Min(1000, retVal))
            End If

            Dim compValStr = Await container.SettingsRepository.GetValueAsync("DefaultCompression", "None")
            Dim compIdx = cboDefaultCompression.FindStringExact(compValStr)
            cboDefaultCompression.SelectedIndex = If(compIdx >= 0, compIdx, 0)

            Dim autoStartStr = Await container.SettingsRepository.GetValueAsync("AutoStartScheduler", "True")
            chkAutoStartScheduler.Checked = (autoStartStr = "True")

            lblAppInfo.Text = $"Application: Desktop Backup Tool v1.0.0{Environment.NewLine}" &
                              $"Runtime:     .NET 8.0 Windows Desktop Runtime{Environment.NewLine}" &
                              $"OS Platform: {Environment.OSVersion.VersionString} ({If(Environment.Is64BitOperatingSystem, "64-bit", "32-bit")})"
        End Function

        Private Async Sub OnVacuumClick(sender As Object, e As EventArgs)
            btnVacuum.Enabled = False
            Try
                Using conn = AppContainer.Instance.DbContext.CreateConnection()
                    Await conn.OpenAsync()
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "VACUUM;"
                        Await cmd.ExecuteNonQueryAsync()
                    End Using
                End Using
                Await LoadSettingsAsync()
                MessageBox.Show(Me, "Database optimized and vacuumed successfully.", "Database Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(Me, $"Failed to optimize database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnVacuum.Enabled = True
            End Try
        End Sub

        Private Async Sub OnSaveSettingsClick(sender As Object, e As EventArgs)
            btnSaveSettings.Enabled = False
            Try
                Dim container = AppContainer.Instance
                Await container.SettingsRepository.SetValueAsync("DefaultRetention", numDefaultRetention.Value.ToString())
                Await container.SettingsRepository.SetValueAsync("DefaultCompression", cboDefaultCompression.SelectedItem.ToString())
                Await container.SettingsRepository.SetValueAsync("AutoStartScheduler", chkAutoStartScheduler.Checked.ToString())

                If chkAutoStartScheduler.Checked Then
                    container.SchedulerService.Start()
                Else
                    container.SchedulerService.Stop()
                End If

                MessageBox.Show(Me, "Settings saved successfully.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(Me, $"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnSaveSettings.Enabled = True
            End Try
        End Sub
    End Class
End Namespace
