Imports System.Drawing
Imports System.Windows.Forms
Imports DesktopBackupTool.Domain.Entities
Imports DesktopBackupTool.Domain.Enums
Imports DesktopBackupTool.Presentation.UI

Namespace DesktopBackupTool.Presentation.Controls
    Public Class LogsView
        Inherits UserControl

        Private cboLevel As ComboBox
        Private txtSearch As TextBox
        Private btnSearch As Button
        Private btnClearFilter As Button
        Private btnClearLogs As Button
        Private btnRefresh As Button
        Private gridLogs As DataGridView
        Private txtDetails As TextBox
        Private _logsList As New List(Of AppLog)()

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBackground
            Me.Font = UITheme.MainFont
            Me.Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Application Logs", .Font = UITheme.HeaderFont, .ForeColor = UITheme.TextPrimary, .Dock = DockStyle.Left, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlToolbar As New Panel() With {.Dock = DockStyle.Top, .Height = 45}

            Dim lblLvl As New Label() With {.Text = "Level:", .Location = New Point(0, 8), .AutoSize = True}
            cboLevel = New ComboBox() With {.Location = New Point(42, 5), .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboLevel.Items.AddRange({"All Levels", "Information", "Warning", "Error"})
            cboLevel.SelectedIndex = 0
            AddHandler cboLevel.SelectedIndexChanged, Async Sub() Await LoadLogsAsync()

            Dim lblSrch As New Label() With {.Text = "Search:", .Location = New Point(175, 8), .AutoSize = True}
            txtSearch = New TextBox() With {.Location = New Point(225, 6), .Width = 180}

            btnSearch = New Button() With {.Text = "Filter", .Location = New Point(415, 5), .Size = New Size(65, 27)}
            UITheme.ApplySecondaryButtonStyle(btnSearch)
            AddHandler btnSearch.Click, Async Sub() Await LoadLogsAsync()

            btnClearFilter = New Button() With {.Text = "Reset", .Location = New Point(485, 5), .Size = New Size(65, 27)}
            UITheme.ApplySecondaryButtonStyle(btnClearFilter)
            AddHandler btnClearFilter.Click, Async Sub()
                                                 txtSearch.Text = String.Empty
                                                 cboLevel.SelectedIndex = 0
                                                 Await LoadLogsAsync()
                                             End Sub

            btnClearLogs = New Button() With {.Text = "Clear Logs", .Location = New Point(560, 5), .Size = New Size(90, 27)}
            UITheme.ApplyDangerButtonStyle(btnClearLogs)
            AddHandler btnClearLogs.Click, AddressOf OnClearLogsClick

            btnRefresh = New Button() With {.Text = "Refresh", .Location = New Point(655, 5), .Size = New Size(75, 27)}
            UITheme.ApplySecondaryButtonStyle(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub() Await LoadLogsAsync()

            pnlToolbar.Controls.AddRange({lblLvl, cboLevel, lblSrch, txtSearch, btnSearch, btnClearFilter, btnClearLogs, btnRefresh})

            Dim splitContainer As New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Horizontal,
                .SplitterDistance = 300,
                .SplitterWidth = 6
            }

            gridLogs = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridLogs)
            gridLogs.Columns.Add("Id", "ID")
            gridLogs.Columns.Add("Timestamp", "Timestamp")
            gridLogs.Columns.Add("Level", "Level")
            gridLogs.Columns.Add("Context", "Source")
            gridLogs.Columns.Add("Message", "Message")

            gridLogs.Columns(0).Visible = False
            gridLogs.Columns(1).Width = 160
            gridLogs.Columns(2).Width = 90
            gridLogs.Columns(3).Width = 130
            gridLogs.Columns(4).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            AddHandler gridLogs.SelectionChanged, AddressOf OnLogSelectionChanged

            splitContainer.Panel1.Controls.Add(gridLogs)

            Dim pnlDetails As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}
            Dim lblDetailsHeader As New Label() With {.Text = "Log Entry Details & Exception Trace", .Dock = DockStyle.Top, .Height = 22, .Font = UITheme.BoldFont}
            txtDetails = New TextBox() With {.Dock = DockStyle.Fill, .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .BackColor = Color.FromArgb(248, 250, 252), .Font = New Font("Consolas", 8.5!)}

            pnlDetails.Controls.Add(txtDetails)
            pnlDetails.Controls.Add(lblDetailsHeader)

            splitContainer.Panel2.Controls.Add(pnlDetails)

            Me.Controls.Add(splitContainer)
            Me.Controls.Add(pnlToolbar)
            Me.Controls.Add(pnlHeader)
        End Sub

        Public Async Function LoadLogsAsync() As Task
            Dim container = AppContainer.Instance
            Dim minLvl As Nullable(Of LogLevel) = Nothing
            Select Case cboLevel.SelectedIndex
                Case 1
                    minLvl = LogLevel.Information
                Case 2
                    minLvl = LogLevel.Warning
                Case 3
                    minLvl = LogLevel.Error
            End Select

            Dim search = If(String.IsNullOrWhiteSpace(txtSearch.Text), Nothing, txtSearch.Text.Trim())
            Dim logs = Await container.LogService.GetLogsAsync(200, minLvl, search).ConfigureAwait(True)
            _logsList = logs.ToList()

            gridLogs.Rows.Clear()
            txtDetails.Text = String.Empty

            For Each log In _logsList
                Dim rowIndex = gridLogs.Rows.Add(
                    log.Id,
                    log.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    log.Level.ToString(),
                    log.Context,
                    log.Message
                )

                Dim row = gridLogs.Rows(rowIndex)
                If log.Level = LogLevel.Error Then
                    row.DefaultCellStyle.ForeColor = UITheme.DangerColor
                ElseIf log.Level = LogLevel.Warning Then
                    row.DefaultCellStyle.ForeColor = UITheme.WarningColor
                End If
            Next
        End Function

        Private Sub OnLogSelectionChanged(sender As Object, e As EventArgs)
            If gridLogs.SelectedRows.Count = 0 Then
                txtDetails.Text = String.Empty
                Return
            End If

            Dim id = Convert.ToInt64(gridLogs.SelectedRows(0).Cells("Id").Value)
            Dim log = _logsList.FirstOrDefault(Function(l) l.Id = id)
            If log IsNot Nothing Then
                Dim fullDetails = $"Timestamp: {log.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}" &
                                  $"Level:     {log.Level}{Environment.NewLine}" &
                                  $"Source:    {log.Context}{Environment.NewLine}" &
                                  $"Message:   {log.Message}{Environment.NewLine}"

                If Not String.IsNullOrEmpty(log.ExceptionDetails) Then
                    fullDetails &= $"{Environment.NewLine}Exception Stack Trace:{Environment.NewLine}{log.ExceptionDetails}"
                End If

                txtDetails.Text = fullDetails
            End If
        End Sub

        Private Async Sub OnClearLogsClick(sender As Object, e As EventArgs)
            Dim result = MessageBox.Show(Me, "Are you sure you want to clear all application logs?", "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Await AppContainer.Instance.LogService.ClearLogsAsync()
                Await LoadLogsAsync()
            End If
        End Sub
    End Class
End Namespace
