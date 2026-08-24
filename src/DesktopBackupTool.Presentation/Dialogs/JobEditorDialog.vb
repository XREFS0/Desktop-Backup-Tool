Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Dialogs
    Public Class JobEditorDialog
        Inherits Form

        Private ReadOnly _job As BackupJob
        Private ReadOnly _encryptionService As Application.Interfaces.IEncryptionService

        Private txtName As TextBox
        Private txtSource As TextBox
        Private btnBrowseSource As Button
        Private txtDestination As TextBox
        Private btnBrowseDest As Button
        Private cboType As ComboBox
        Private cboSchedule As ComboBox
        Private dtpTime As DateTimePicker
        Private chkDays As CheckedListBox
        Private numInterval As NumericUpDown
        Private cboCompression As ComboBox
        Private chkEnableEncryption As CheckBox
        Private txtPassword As TextBox
        Private txtConfirmPassword As TextBox
        Private numRetention As NumericUpDown
        Private txtExcludedDirs As TextBox
        Private txtExcludedExts As TextBox
        Private txtFileFilters As TextBox
        Private chkEnabled As CheckBox
        Private btnSave As Button
        Private btnCancel As Button
        Private lblError As Label
        Private pnlScheduleDetails As Panel
        Private pnlEncryptionDetails As Panel

        Public Property Job As BackupJob
            Get
                Return _job
            End Get
            Private Set(value As BackupJob)
            End Set
        End Property

        Public Sub New(job As BackupJob, encryptionService As Application.Interfaces.IEncryptionService)
            _job = If(job, New BackupJob())
            _encryptionService = encryptionService

            InitializeComponent()
            LoadJobData()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = If(_job.Id = 0, "Create Backup Job", "Edit Backup Job")
            Me.Size = New Size(620, 680)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont

            Dim tabControl As New TabControl()
            tabControl.Dock = DockStyle.Fill
            tabControl.Font = UITheme.MainFont

            Dim tabGeneral As New TabPage("General")
            Dim tabSchedule As New TabPage("Schedule")
            Dim tabSecurity As New TabPage("Compression & Security")
            Dim tabAdvanced As New TabPage("Filters & Retention")

            SetupGeneralTab(tabGeneral)
            SetupScheduleTab(tabSchedule)
            SetupSecurityTab(tabSecurity)
            SetupAdvancedTab(tabAdvanced)

            tabControl.TabPages.Add(tabGeneral)
            tabControl.TabPages.Add(tabSchedule)
            tabControl.TabPages.Add(tabSecurity)
            tabControl.TabPages.Add(tabAdvanced)

            Dim pnlBottom As New Panel()
            pnlBottom.Dock = DockStyle.Bottom
            pnlBottom.Height = 55
            pnlBottom.BackColor = Color.FromArgb(248, 250, 252)
            pnlBottom.Padding = New Padding(12)

            lblError = New Label()
            lblError.ForeColor = UITheme.DangerColor
            lblError.Location = New Point(12, 16)
            lblError.Size = New Size(380, 25)
            lblError.AutoEllipsis = True
            lblError.Font = UITheme.SmallFont

            btnSave = New Button()
            btnSave.Text = "Save Job"
            btnSave.Location = New Point(400, 10)
            btnSave.Size = New Size(95, 32)
            UITheme.ApplyPrimaryButtonStyle(btnSave)
            AddHandler btnSave.Click, AddressOf OnSaveClick

            btnCancel = New Button()
            btnCancel.Text = "Cancel"
            btnCancel.Location = New Point(505, 10)
            btnCancel.Size = New Size(85, 32)
            btnCancel.DialogResult = DialogResult.Cancel
            UITheme.ApplySecondaryButtonStyle(btnCancel)

            pnlBottom.Controls.Add(lblError)
            pnlBottom.Controls.Add(btnSave)
            pnlBottom.Controls.Add(btnCancel)

            Me.Controls.Add(tabControl)
            Me.Controls.Add(pnlBottom)
            Me.AcceptButton = btnSave
            Me.CancelButton = btnCancel
        End Sub

        Private Sub SetupGeneralTab(page As TabPage)
            page.BackColor = Color.White
            page.Padding = New Padding(16)

            Dim lblName As New Label() With {.Text = "Job Name:", .Location = New Point(16, 16), .AutoSize = True}
            txtName = New TextBox() With {.Location = New Point(16, 38), .Width = 550}

            Dim lblSource As New Label() With {.Text = "Source Directory:", .Location = New Point(16, 75), .AutoSize = True}
            txtSource = New TextBox() With {.Location = New Point(16, 97), .Width = 445}
            btnBrowseSource = New Button() With {.Text = "Browse...", .Location = New Point(470, 95), .Size = New Size(95, 27)}
            UITheme.ApplySecondaryButtonStyle(btnBrowseSource)
            AddHandler btnBrowseSource.Click, AddressOf OnBrowseSource

            Dim lblDest As New Label() With {.Text = "Destination Directory:", .Location = New Point(16, 135), .AutoSize = True}
            txtDestination = New TextBox() With {.Location = New Point(16, 157), .Width = 445}
            btnBrowseDest = New Button() With {.Text = "Browse...", .Location = New Point(470, 155), .Size = New Size(95, 27)}
            UITheme.ApplySecondaryButtonStyle(btnBrowseDest)
            AddHandler btnBrowseDest.Click, AddressOf OnBrowseDest

            Dim lblType As New Label() With {.Text = "Backup Type:", .Location = New Point(16, 195), .AutoSize = True}
            cboType = New ComboBox() With {.Location = New Point(16, 217), .Width = 260, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboType.Items.Add("Full (Copy all files)")
            cboType.Items.Add("Incremental (Copy modified only)")
            cboType.SelectedIndex = 0

            chkEnabled = New CheckBox() With {.Text = "Job is Enabled and Active", .Location = New Point(16, 265), .AutoSize = True, .Checked = True}

            page.Controls.AddRange({lblName, txtName, lblSource, txtSource, btnBrowseSource, lblDest, txtDestination, btnBrowseDest, lblType, cboType, chkEnabled})
        End Sub

        Private Sub SetupScheduleTab(page As TabPage)
            page.BackColor = Color.White
            page.Padding = New Padding(16)

            Dim lblSchedType As New Label() With {.Text = "Schedule Type:", .Location = New Point(16, 16), .AutoSize = True}
            cboSchedule = New ComboBox() With {.Location = New Point(16, 38), .Width = 260, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboSchedule.Items.AddRange({"Manual Only", "Daily", "Weekly", "Hourly Interval"})
            AddHandler cboSchedule.SelectedIndexChanged, AddressOf OnScheduleTypeChanged

            pnlScheduleDetails = New Panel() With {.Location = New Point(16, 80), .Size = New Size(550, 400)}

            Dim lblTime As New Label() With {.Text = "Scheduled Time (HH:mm):", .Location = New Point(0, 5), .AutoSize = True}
            dtpTime = New DateTimePicker() With {.Location = New Point(0, 27), .Format = DateTimePickerFormat.Custom, .CustomFormat = "HH:mm", .ShowUpDown = True, .Width = 120}

            Dim lblDays As New Label() With {.Text = "Days of the Week:", .Location = New Point(0, 65), .AutoSize = True}
            chkDays = New CheckedListBox() With {.Location = New Point(0, 87), .Size = New Size(260, 140), .CheckOnClick = True}
            chkDays.Items.AddRange({"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"})

            Dim lblInterval As New Label() With {.Text = "Repeat every (hours):", .Location = New Point(0, 240), .AutoSize = True}
            numInterval = New NumericUpDown() With {.Location = New Point(0, 262), .Minimum = 1, .Maximum = 720, .Value = 24, .Width = 120}

            pnlScheduleDetails.Controls.AddRange({lblTime, dtpTime, lblDays, chkDays, lblInterval, numInterval})

            page.Controls.AddRange({lblSchedType, cboSchedule, pnlScheduleDetails})
        End Sub

        Private Sub SetupSecurityTab(page As TabPage)
            page.BackColor = Color.White
            page.Padding = New Padding(16)

            Dim lblComp As New Label() With {.Text = "ZIP Compression:", .Location = New Point(16, 16), .AutoSize = True}
            cboCompression = New ComboBox() With {.Location = New Point(16, 38), .Width = 260, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboCompression.Items.AddRange({"None (Raw Folder Mirror)", "Fast Compression (ZIP)", "Optimal Compression (ZIP)"})
            cboCompression.SelectedIndex = 0

            chkEnableEncryption = New CheckBox() With {.Text = "Enable AES-256 Military Grade Encryption", .Location = New Point(16, 95), .AutoSize = True}
            AddHandler chkEnableEncryption.CheckedChanged, AddressOf OnEncryptionToggled

            pnlEncryptionDetails = New Panel() With {.Location = New Point(16, 130), .Size = New Size(550, 200), .Enabled = False}

            Dim lblPwd As New Label() With {.Text = "Encryption Password:", .Location = New Point(0, 5), .AutoSize = True}
            txtPassword = New TextBox() With {.Location = New Point(0, 27), .Width = 320, .UseSystemPasswordChar = True}

            Dim lblConfirm As New Label() With {.Text = "Confirm Password:", .Location = New Point(0, 65), .AutoSize = True}
            txtConfirmPassword = New TextBox() With {.Location = New Point(0, 87), .Width = 320, .UseSystemPasswordChar = True}

            pnlEncryptionDetails.Controls.AddRange({lblPwd, txtPassword, lblConfirm, txtConfirmPassword})

            page.Controls.AddRange({lblComp, cboCompression, chkEnableEncryption, pnlEncryptionDetails})
        End Sub

        Private Sub SetupAdvancedTab(page As TabPage)
            page.BackColor = Color.White
            page.Padding = New Padding(16)

            Dim lblRet As New Label() With {.Text = "Retention Policy (Keep last N successful backups):", .Location = New Point(16, 16), .AutoSize = True}
            numRetention = New NumericUpDown() With {.Location = New Point(16, 38), .Minimum = 1, .Maximum = 1000, .Value = 5, .Width = 120}

            Dim lblExDirs As New Label() With {.Text = "Excluded Folder Names (semicolon-separated):", .Location = New Point(16, 80), .AutoSize = True}
            txtExcludedDirs = New TextBox() With {.Location = New Point(16, 102), .Width = 550, .Text = "node_modules;.git;bin;obj;temp"}

            Dim lblExExts As New Label() With {.Text = "Excluded File Extensions (semicolon-separated):", .Location = New Point(16, 140), .AutoSize = True}
            txtExcludedExts = New TextBox() With {.Location = New Point(16, 162), .Width = 550, .Text = ".tmp;.log;.bak"}

            Dim lblFilters As New Label() With {.Text = "Include File Patterns (semicolon-separated):", .Location = New Point(16, 200), .AutoSize = True}
            txtFileFilters = New TextBox() With {.Location = New Point(16, 222), .Width = 550, .Text = "*.*"}

            page.Controls.AddRange({lblRet, numRetention, lblExDirs, txtExcludedDirs, lblExExts, txtExcludedExts, lblFilters, txtFileFilters})
        End Sub

        Private Sub LoadJobData()
            txtName.Text = _job.Name
            txtSource.Text = _job.SourcePath
            txtDestination.Text = _job.DestinationPath
            cboType.SelectedIndex = CInt(_job.BackupType)
            chkEnabled.Checked = _job.IsEnabled

            cboSchedule.SelectedIndex = CInt(_job.ScheduleType)
            If Not String.IsNullOrWhiteSpace(_job.ScheduleTime) Then
                Dim t As TimeSpan
                If TimeSpan.TryParse(_job.ScheduleTime, t) Then
                    dtpTime.Value = DateTime.Today.Add(t)
                End If
            End If

            If Not String.IsNullOrWhiteSpace(_job.ScheduleDaysOfWeek) Then
                Dim days = _job.ScheduleDaysOfWeek.Split(","c)
                For i As Integer = 0 To chkDays.Items.Count - 1
                    Dim dayName = chkDays.Items(i).ToString()
                    If days.Contains(dayName, StringComparer.OrdinalIgnoreCase) Then
                        chkDays.SetItemChecked(i, True)
                    End If
                Next
            End If

            numInterval.Value = Math.Max(1, Math.Min(720, _job.ScheduleIntervalHours))
            cboCompression.SelectedIndex = CInt(_job.CompressionLevel)
            chkEnableEncryption.Checked = _job.EnableEncryption
            pnlEncryptionDetails.Enabled = _job.EnableEncryption

            numRetention.Value = Math.Max(1, Math.Min(1000, _job.RetentionCount))
            txtExcludedDirs.Text = _job.ExcludedDirectories
            txtExcludedExts.Text = _job.ExcludedExtensions
            txtFileFilters.Text = If(String.IsNullOrWhiteSpace(_job.FileFilterPatterns), "*.*", _job.FileFilterPatterns)

            UpdateScheduleVisibility()
        End Sub

        Private Sub OnScheduleTypeChanged(sender As Object, e As EventArgs)
            UpdateScheduleVisibility()
        End Sub

        Private Sub UpdateScheduleVisibility()
            Select Case cboSchedule.SelectedIndex
                Case 0
                    pnlScheduleDetails.Visible = False
                Case 1
                    pnlScheduleDetails.Visible = True
                    dtpTime.Visible = True
                    chkDays.Visible = False
                    numInterval.Visible = False
                Case 2
                    pnlScheduleDetails.Visible = True
                    dtpTime.Visible = True
                    chkDays.Visible = True
                    numInterval.Visible = False
                Case 3
                    pnlScheduleDetails.Visible = True
                    dtpTime.Visible = False
                    chkDays.Visible = False
                    numInterval.Visible = True
            End Select
        End Sub

        Private Sub OnEncryptionToggled(sender As Object, e As EventArgs)
            pnlEncryptionDetails.Enabled = chkEnableEncryption.Checked
        End Sub

        Private Sub OnBrowseSource(sender As Object, e As EventArgs)
            Using fbd As New FolderBrowserDialog()
                fbd.Description = "Select Source Directory"
                fbd.UseDescriptionForTitle = True
                If Directory.Exists(txtSource.Text) Then
                    fbd.InitialDirectory = txtSource.Text
                End If
                If fbd.ShowDialog(Me) = DialogResult.OK Then
                    txtSource.Text = fbd.SelectedPath
                End If
            End Using
        End Sub

        Private Sub OnBrowseDest(sender As Object, e As EventArgs)
            Using fbd As New FolderBrowserDialog()
                fbd.Description = "Select Backup Destination Directory"
                fbd.UseDescriptionForTitle = True
                If Directory.Exists(txtDestination.Text) Then
                    fbd.InitialDirectory = txtDestination.Text
                End If
                If fbd.ShowDialog(Me) = DialogResult.OK Then
                    txtDestination.Text = fbd.SelectedPath
                End If
            End Using
        End Sub

        Private Sub OnSaveClick(sender As Object, e As EventArgs)
            lblError.Text = String.Empty

            If String.IsNullOrWhiteSpace(txtName.Text) Then
                lblError.Text = "Job name is required."
                Return
            End If

            If String.IsNullOrWhiteSpace(txtSource.Text) Then
                lblError.Text = "Source directory is required."
                Return
            End If

            If String.IsNullOrWhiteSpace(txtDestination.Text) Then
                lblError.Text = "Destination directory is required."
                Return
            End If

            If chkEnableEncryption.Checked Then
                If _job.Id = 0 OrElse Not String.IsNullOrEmpty(txtPassword.Text) Then
                    If String.IsNullOrEmpty(txtPassword.Text) Then
                        lblError.Text = "Password is required when encryption is enabled."
                        Return
                    End If
                    If txtPassword.Text.Length < 6 Then
                        lblError.Text = "Password must be at least 6 characters."
                        Return
                    End If
                    If Not txtPassword.Text.Equals(txtConfirmPassword.Text) Then
                        lblError.Text = "Passwords do not match."
                        Return
                    End If
                End If
            End If

            _job.Name = txtName.Text.Trim()
            _job.SourcePath = txtSource.Text.Trim()
            _job.DestinationPath = txtDestination.Text.Trim()
            _job.BackupType = CType(cboType.SelectedIndex, BackupType)
            _job.IsEnabled = chkEnabled.Checked
            _job.ScheduleType = CType(cboSchedule.SelectedIndex, ScheduleType)
            _job.ScheduleTime = dtpTime.Value.ToString("HH:mm")

            Dim selectedDays As New List(Of String)()
            For Each item In chkDays.CheckedItems
                selectedDays.Add(item.ToString())
            Next
            _job.ScheduleDaysOfWeek = String.Join(",", selectedDays)
            _job.ScheduleIntervalHours = CInt(numInterval.Value)

            _job.CompressionLevel = CType(cboCompression.SelectedIndex, CompressionLevelOption)
            _job.EnableEncryption = chkEnableEncryption.Checked

            If chkEnableEncryption.Checked AndAlso Not String.IsNullOrEmpty(txtPassword.Text) Then
                Dim salt = _encryptionService.GenerateSalt()
                Dim hash = _encryptionService.HashPassword(txtPassword.Text, salt)
                _job.EncryptionSalt = salt
                _job.EncryptionPasswordHash = hash
            ElseIf Not chkEnableEncryption.Checked Then
                _job.EncryptionSalt = String.Empty
                _job.EncryptionPasswordHash = String.Empty
            End If

            _job.RetentionCount = CInt(numRetention.Value)
            _job.ExcludedDirectories = txtExcludedDirs.Text.Trim()
            _job.ExcludedExtensions = txtExcludedExts.Text.Trim()
            _job.FileFilterPatterns = If(String.IsNullOrWhiteSpace(txtFileFilters.Text), "*.*", txtFileFilters.Text.Trim())

            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class
End Namespace
