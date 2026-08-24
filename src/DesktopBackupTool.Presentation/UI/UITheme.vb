Imports System.Drawing
Imports System.Windows.Forms

Namespace DesktopBackupTool.Presentation.UI
    Public Module UITheme
        Public ReadOnly PrimaryColor As Color = Color.FromArgb(0, 102, 178)
        Public ReadOnly PrimaryHoverColor As Color = Color.FromArgb(0, 85, 150)
        Public ReadOnly SidebarBackground As Color = Color.FromArgb(243, 244, 246)
        Public ReadOnly SidebarSelected As Color = Color.FromArgb(229, 231, 235)
        Public ReadOnly ContentBackground As Color = Color.FromArgb(255, 255, 255)
        Public ReadOnly CardBackground As Color = Color.FromArgb(250, 250, 250)
        Public ReadOnly BorderColor As Color = Color.FromArgb(226, 232, 240)
        Public ReadOnly TextPrimary As Color = Color.FromArgb(30, 41, 59)
        Public ReadOnly TextSecondary As Color = Color.FromArgb(100, 116, 139)
        Public ReadOnly SuccessColor As Color = Color.FromArgb(22, 101, 52)
        Public ReadOnly SuccessBg As Color = Color.FromArgb(240, 253, 244)
        Public ReadOnly WarningColor As Color = Color.FromArgb(180, 83, 9)
        Public ReadOnly WarningBg As Color = Color.FromArgb(254, 243, 199)
        Public ReadOnly DangerColor As Color = Color.FromArgb(185, 28, 28)
        Public ReadOnly DangerBg As Color = Color.FromArgb(254, 242, 242)

        Public ReadOnly HeaderFont As New Font("Segoe UI", 12.0!, FontStyle.Bold)
        Public ReadOnly SubheaderFont As New Font("Segoe UI", 10.0!, FontStyle.Bold)
        Public ReadOnly MainFont As New Font("Segoe UI", 9.0!, FontStyle.Regular)
        Public ReadOnly BoldFont As New Font("Segoe UI", 9.0!, FontStyle.Bold)
        Public ReadOnly SmallFont As New Font("Segoe UI", 8.25!, FontStyle.Regular)
        Public ReadOnly StatValueFont As New Font("Segoe UI", 16.0!, FontStyle.Bold)

        Public Sub ApplyGridStyle(grid As DataGridView)
            grid.EnableHeadersVisualStyles = False
            grid.BackgroundColor = Color.White
            grid.BorderStyle = BorderStyle.FixedSingle
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            grid.GridColor = BorderColor
            grid.RowHeadersVisible = False
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            grid.MultiSelect = False
            grid.AllowUserToAddRows = False
            grid.AllowUserToDeleteRows = False
            grid.AllowUserToResizeRows = False
            grid.RowTemplate.Height = 32
            grid.Font = MainFont

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249)
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont
            grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(6, 4, 6, 4)
            grid.ColumnHeadersHeight = 34
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single

            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 238, 249)
            grid.DefaultCellStyle.SelectionForeColor = TextPrimary
            grid.DefaultCellStyle.Padding = New Padding(6, 2, 6, 2)
        End Sub

        Public Sub ApplyPrimaryButtonStyle(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = PrimaryColor
            btn.ForeColor = Color.White
            btn.Font = BoldFont
            btn.Cursor = Cursors.Hand
            btn.Height = 32
            btn.Padding = New Padding(8, 0, 8, 0)
        End Sub

        Public Sub ApplySecondaryButtonStyle(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = BorderColor
            btn.FlatAppearance.BorderSize = 1
            btn.BackColor = Color.White
            btn.ForeColor = TextPrimary
            btn.Font = MainFont
            btn.Cursor = Cursors.Hand
            btn.Height = 32
            btn.Padding = New Padding(8, 0, 8, 0)
        End Sub

        Public Sub ApplyDangerButtonStyle(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = DangerColor
            btn.ForeColor = Color.White
            btn.Font = BoldFont
            btn.Cursor = Cursors.Hand
            btn.Height = 32
            btn.Padding = New Padding(8, 0, 8, 0)
        End Sub

        Public Function FormatBytes(bytes As Long) As String
            If bytes < 1024 Then
                Return $"{bytes} B"
            ElseIf bytes < 1048576 Then
                Return $"{bytes / 1024.0:F1} KB"
            ElseIf bytes < 1073741824 Then
                Return $"{bytes / 1048576.0:F2} MB"
            Else
                Return $"{bytes / 1073741824.0:F2} GB"
            End If
        End Function
    End Module
End Namespace
