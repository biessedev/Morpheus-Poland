﻿Option Explicit On
Option Compare Text
Imports MySql.Data.MySqlClient
Imports System.IO
Imports System.Configuration

Public Class FormTimeShow
    Dim yelloDelay As Integer = 5
    Dim tblTP As DataTable
    Dim DsTP As New DataSet
    Dim tbltp_static As DataTable
    Dim Dstp_static As New DataSet

    Private Sub FormShow_Disposed(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Disposed
        FormStart.Show()
    End Sub

    Private Sub FormTimeShow_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load

        TreeViewProjectList.HideSelection = False
        Dim builder As New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
            Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
                AdapterTP.Fill(DsTP, "TimeProject")
                tblTP = DsTP.Tables("TimeProject")
            End Using
        End Using
        FillCustomerCombo()
        FillAreaCombo()
        FillResponsibleCombo()
        ComboBoxStatusFilter.Items.Add("")
        ComboBoxStatusFilter.Items.Add("OPEN")
        ComboBoxStatusFilter.Items.Add("CLOSED")
        ComboBoxStatusFilter.Items.Add("STANDBY")
        TimerShow.Enabled = False
        UpdateTreeProjectList(True)
        TreeViewProjectList.SelectedNode = TreeViewProjectList.TopNode
    End Sub

    Function quality(ByVal id As String, ByVal refresh As Boolean) As Integer
        quality = 0
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
                Dim i As Integer = tbltp_static.Rows.Count
            Catch ex As Exception
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
		            AdapterTP.Fill(Dstp_static, "TimeProject")
                    tbltp_static = Dstp_static.Tables("TimeProject")
	            End Using
            End Try

            If refresh Then
                Dstp_static.Clear()
                tbltp_static.Clear()
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
		            AdapterTP.Fill(Dstp_static, "TimeProject")
                    tblTP = Dstp_static.Tables("TimeProject")
	            End Using
            End If
        End Using
        quality = 100
        Dim rowShow As DataRow() = tbltp_static.Select("Project = '" & id & "'")
        If rowShow.Length > 0 Then
            For Each row In rowShow
                If Val(row("quality").ToString) < quality Then quality = Val(row("quality").ToString)
            Next
        Else
            quality = 100
        End If

    End Function

    Function ProjectLeader(ByVal id As String, ByVal refresh As Boolean) As String

        ProjectLeader = ""
        Dim builder As New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
            Try
                Dim i As Integer = tbltp_static.Rows.Count
            Catch ex As Exception
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
                    AdapterTP.Fill(Dstp_static, "TimeProject")
                    tbltp_static = Dstp_static.Tables("TimeProject")
                End Using
            End Try

            If refresh Then
                Dstp_static.Clear()
                tbltp_static.Clear()
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
                    AdapterTP.Fill(Dstp_static, "TimeProject")
                    tblTP = Dstp_static.Tables("TimeProject")
                End Using
            End If
        End Using

        Dim rowShow As DataRow() = tbltp_static.Select("Project = '" & id & "'")

        For Each row In rowShow
            ProjectLeader = row("ProjectLeader").ToString
        Next
    End Function

    Private Sub TimerShow_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles TimerShow.Tick

        DsTP.Clear()
        tblTP.Clear()

        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
		        AdapterTP.Fill(DsTP, "TimeProject")
                tblTP = DsTP.Tables("TimeProject")
	        End Using
        End Using

        If TreeViewProjectList.Nodes.Count > 0 Then

            Try
                If TreeViewProjectList.SelectedNode.Index = TreeViewProjectList.Nodes.Count - 1 Then
                    UpdateTreeProjectList(True)
                    TreeViewProjectList.SelectedNode = TreeViewProjectList.TopNode
                    Application.DoEvents()
                Else

                    TreeViewProjectList.SelectedNode = TreeViewProjectList.SelectedNode.NextNode
                    Application.DoEvents()
                End If

            Catch ex As Exception
                TreeViewProjectList.SelectedNode = TreeViewProjectList.TopNode
                Application.DoEvents()
            End Try
        Else
        End If

        TreeViewProjectList.Focus()
        Try
            If ProjectStatus(TreeViewProjectList.SelectedNode.Text, True) = "DELAY" Then
                TimerShow.Interval = 13000
            Else
                TimerShow.Interval = 8000
            End If
        Catch ex As Exception
            If TreeViewProjectList.Nodes.Count > 0 Then
                If ProjectStatus(TreeViewProjectList.SelectedNode.Text, True) = "DELAY" Then
                    TimerShow.Interval = 13000
                Else
                    TimerShow.Interval = 8000
                End If
            End If
        End Try

    End Sub

    Function TimingTS(ByVal Taskstart As String, ByVal Taskend As String, ByVal compleated As String, ByVal taskStatus As String) As String
        If Len(Taskstart) = 10 And Len(Taskend) = 10 Then
            TimingTS = "ONTIME"
            Dim TotalTimeTask As Integer = DateDiff("d", string_to_date(Taskstart), string_to_date(Taskend))
            Dim EquivalentTime As Date = DateAdd("d", Int(Val(compleated) * TotalTimeTask / 100), string_to_date(Taskstart))
            If DateDiff("d", Today, EquivalentTime) < 0 Then
                TimingTS = "DELAY"
            End If

        Else
            TimingTS = ""
        End If
        If compleated = "100" Then TimingTS = "ONTIME"
        If taskStatus = "STANDBY" Then TimingTS = "STANDBY"
    End Function

    Function area(ByVal id As String, ByVal refresh As Boolean) As String

        area = ""
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using

        Dim rowShow As DataRow() = tbltp_static.Select("Project = '" & id & "'")

        For Each row In rowShow
            If row("area").ToString <> "" Then area = row("area").ToString
        Next

    End Function

    Private Sub TreeViewProjectList_AfterSelect(ByVal sender As Object, ByVal e As TreeViewEventArgs) Handles TreeViewProjectList.AfterSelect
        LabelDelayAdvance.Text = ""
        LabelProject.Text = Mid(area(TreeViewProjectList.SelectedNode.Text, True), 3) & "  -  " & TreeViewProjectList.SelectedNode.Text
        ProgressBarProject.Width = Int(ProjectCompleated(TreeViewProjectList.SelectedNode.Text, False) * 1079 / 100)

        Dim projectStatusStr As String = ProjectStatus(TreeViewProjectList.SelectedNode.Text, False)
        If projectStatusStr = "ONTIME" Then ProgressBarProject.BackColor = Color.Green
        If projectStatusStr = "DELAY" Then ProgressBarProject.BackColor = Color.Red
        If projectStatusStr = "CLOSED" Then ProgressBarProject.BackColor = Color.SeaGreen
        If projectStatusStr = "STANDBY" Then ProgressBarProject.BackColor = Color.Blue
        If projectStatusStr = "CRITIC" Then ProgressBarProject.BackColor = Color.Orange

        LabelStart.Text = Replace(ProjectStart(TreeViewProjectList.SelectedNode.Text, False), "/", ".")
        LabelEnd.Text = Replace(ProjectEnd(TreeViewProjectList.SelectedNode.Text, False), "/", ".")
        LabelProjectLeader.Text = ProjectLeader(TreeViewProjectList.SelectedNode.Text, False)
        If DelayAdvanceTime(TreeViewProjectList.SelectedNode.Text, False) > 0 Then LabelDelayAdvance.Text = "Advance Time: "
        If DelayAdvanceTime(TreeViewProjectList.SelectedNode.Text, False) <= 0 Then LabelDelayAdvance.Text = "Delay: "
        If projectStatusStr = "STANDBY" Then LabelDelayAdvance.Text = "STANDBY "
        If projectStatusStr = "CLOSED" Then LabelDelayAdvance.Text = "CLOSED "

        If projectStatusStr = "CRITIC" Or projectStatusStr = "DELAY" Or projectStatusStr = "ONTIME" Then LabelDelayAdvance.Text = LabelDelayAdvance.Text & -DelayAdvanceTime(TreeViewProjectList.SelectedNode.Text, False) & "  Days"
        LabelProjectTotalTime.Text = "Total Time: " & DateDiff("d", string_to_date(ProjectStart(TreeViewProjectList.SelectedNode.Text, False)), string_to_date(ProjectEnd(TreeViewProjectList.SelectedNode.Text, False))) & " Days"
        Application.DoEvents()
        ProgressBarQuality.Width = Int(Val(quality(TreeViewProjectList.SelectedNode.Text, False)) * 378 / 100)

        ProgressBarQuality.BackColor = Color.Green
        If ProgressBarQuality.Width < 0.95 * 378 Then ProgressBarQuality.BackColor = Color.Green
        If ProgressBarQuality.Width < 0.9 * 378 Then ProgressBarQuality.BackColor = Color.LightGreen
        If ProgressBarQuality.Width < 0.85 * 378 Then ProgressBarQuality.BackColor = Color.Yellow
        If ProgressBarQuality.Width < 0.8 * 378 Then ProgressBarQuality.BackColor = Color.Orange
        If ProgressBarQuality.Width < 0.75 * 378 Then ProgressBarQuality.BackColor = Color.Red
        Try
            PictureBoxB.ImageLocation = ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_B.jpg"
            PictureBoxT.ImageLocation = ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_T.jpg"
            PictureBoxC.ImageLocation = ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_C.jpg"
            PictureBoxF.ImageLocation = ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_F.jpg"
            Application.DoEvents()

            If File.Exists(ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_b.jpg") Then
                PictureBoxB.Visible = True
            Else
                PictureBoxB.Visible = False
            End If
            If File.Exists(ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_t.jpg") Then
                PictureBoxT.Visible = True
            Else
                PictureBoxT.Visible = False
            End If
            If File.Exists(ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_c.jpg") Then
                PictureBoxC.Visible = True
            Else
                PictureBoxC.Visible = False
            End If
            If File.Exists(ParameterTable("PathMorpheus") & ParameterTable("PathNPI") & ParameterTable("PathPicture") & TreeViewProjectList.SelectedNode.Text & "_f.jpg") Then
                PictureBoxF.Visible = True
            Else
                PictureBoxF.Visible = False
            End If

            Application.DoEvents()

        Catch ex As Exception

        End Try
        UpdateTreeTaskList(True)

    End Sub

    Function ProjectStart(ByVal id As String, ByVal refresh As Boolean) As String

        ProjectStart = ""
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using

        Dim rowShow As DataRow() = tbltp_static.Select("Project = '" & id & "'")
        If rowShow.Length > 0 Then
            For Each row In rowShow
                If ProjectStart = "" Or DateDiff("d", string_to_date(ProjectStart), string_to_date(row("start").ToString)) < 0 Then
                    ProjectStart = row("start").ToString
                End If
            Next
        End If

    End Function

    Function ProjectEnd(ByVal id As String, ByVal refresh As Boolean) As String

        ProjectEnd = ""
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using


        Dim rowShow As DataRow() = tbltp_static.Select("Project = '" & id & "'")
        If rowShow.Length > 0 Then
            For Each row In rowShow
                If ProjectEnd = "" Or DateDiff("d", string_to_date(ProjectEnd), string_to_date(row("end").ToString)) > 0 Then
                    ProjectEnd = row("end").ToString
                End If
            Next
        End If

    End Function

    Sub UpdateTreeTaskList(ByVal refresh As Boolean)
        TreeViewTaskList.Nodes.Clear()

        If refresh Then
            DsTP.Clear()
            tblTP.Clear()
            Dim  builder As  New Common.DbConnectionStringBuilder()
            builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
            Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	            Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
		            AdapterTP.Fill(DsTP, "TimeProject")
                    tblTP = DsTP.Tables("TimeProject")
	            End Using
            End Using
        End If
        Dim sql As String = IIf(ComboBoxAreaFilter.Text = "", "area like '*' and ", "area LIKE '" & ComboBoxAreaFilter.Text & "*' and ") &
                            IIf(ComboBoxStatusFilter.Text = "", "status like '*'  and ", "status = '" & ComboBoxCustomerFilter.Text & "'  and ") &
                            IIf(ComboBoxCustomerFilter.Text = "", "customer like '*'  and ", "customer = '" & ComboBoxCustomerFilter.Text & "'  and ") &
                            IIf(True, "not project like '*template*'  and ", "") &
                            " project = '" & TreeViewProjectList.SelectedNode.Text & "'  and " &
                            IIf(ComboBoxResponsibleFilter.Text = "", "ProjectLeader like '*'  ", "projectleader = '" & ComboBoxResponsibleFilter.Text & "'  ")
        Dim rowShow As DataRow() = tblTP.Select(sql, "project, id")
        For Each row In rowShow
            Dim rootNode As TreeNode = New TreeNode(Mid(row("taskname").ToString, 1, 42) & Mid("  --------------------------------------------------", 1, 43 - Len(Mid(row("taskname").ToString, 1, 42))) & "> " & row("taskleader").ToString)
            If row("status").ToString = ("CLOSED") Then rootNode.ForeColor = Color.SeaGreen
            If row("status").ToString = ("STANDBY") Then rootNode.ForeColor = Color.Blue
            If row("status").ToString = ("OPEN") Then
                If Len(row("start").ToString) <> 10 And Len(row("end").ToString) <> 10 Then

                ElseIf TimingTS(row("start").ToString, row("end").ToString, row("compleated").ToString, row("status").ToString) = ("ONTIME") Then
                    rootNode.ForeColor = Color.Green
                ElseIf DelayAdvanceTimeTask(row("taskname").ToString, row("project").ToString, True) > -yelloDelay Then
                    rootNode.ForeColor = Color.Orange
                Else
                    rootNode.ForeColor = Color.Red
                End If
            End If

            TreeViewTaskList.Nodes.Add(rootNode)
            TreeViewTaskList.ResumeLayout()
            refresh = False
        Next

    End Sub

    Sub UpdateTreeProjectList(ByVal refresh As Boolean)
        TreeViewProjectList.Nodes.Clear()

        If refresh Then
            DsTP.Clear()
            tblTP.Clear()
            Dim builder As New Common.DbConnectionStringBuilder()
            builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
            Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
                    AdapterTP.Fill(DsTP, "TimeProject")
                    tblTP = DsTP.Tables("TimeProject")
                End Using
            End Using
        End If
        Dim sql As String = IIf(ComboBoxAreaFilter.Text = "", "area like '*' and ", "area like '" & ComboBoxAreaFilter.Text & "*' and ") &
                            IIf(ComboBoxStatusFilter.Text = "", "status like '*'  and ", "status = '" & ComboBoxStatusFilter.Text & "'  and ") &
                            IIf(ComboBoxCustomerFilter.Text = "", "customer like '*'  and ", "customer = '" & ComboBoxCustomerFilter.Text & "'  and ") &
                            IIf(True, "not project like '*template*'  and ", "") &
                            IIf(ComboBoxResponsibleFilter.Text = "", "ProjectLeader like '*'  ", "projectleader = '" & ComboBoxResponsibleFilter.Text & "'  ")
        Dim rowShow As DataRow() = tblTP.Select(sql, "project, id")

        Dim Project As String = ""
        Dim projectStatusStr As String = ""
        For Each row In rowShow

            If row("project").ToString <> Project Then
                Dim rootNode As TreeNode = New TreeNode(row("project").ToString)
                TreeViewProjectList.BeginUpdate()
                TreeViewProjectList.Nodes.Add(rootNode)
                TreeViewProjectList.EndUpdate()
                TreeViewProjectList.ResumeLayout()
                Project = row("project").ToString
                projectStatusStr = ProjectStatus(row("project").ToString, refresh)
                If projectStatusStr = "ONTIME" Then rootNode.ForeColor = Color.Green
                If projectStatusStr = "DELAY" Then rootNode.ForeColor = Color.Red
                If projectStatusStr = "CLOSED" Then rootNode.ForeColor = Color.SeaGreen
                If projectStatusStr = "STANDBY" Then rootNode.ForeColor = Color.Blue
                If projectStatusStr = "CRITIC" Then rootNode.ForeColor = Color.Orange
            End If

            TreeViewProjectList.ResumeLayout()
            refresh = False
        Next
    End Sub

    Sub FillCustomerCombo()
        Dim customer = ""

        ComboBoxCustomerFilter.Items.Clear()
        ComboBoxCustomerFilter.Items.Add("")

        Dim rowResults As DataRow() = tblTP.Select("project like '*'", "customer")
        For Each row In rowResults
            If customer <> row("customer").ToString Then ComboBoxCustomerFilter.Items.Add(UCase(row("customer").ToString))
            customer = row("customer").ToString
        Next
        ComboBoxCustomerFilter.Sorted = True
    End Sub

    Sub FillAreaCombo()
        Dim Area = ""
        ComboBoxAreaFilter.Items.Clear()
        ComboBoxAreaFilter.Items.Add("")
        Dim rowResults As DataRow() = tblTP.Select("project like '*'", "area")
        For Each row In rowResults
            If Area <> row("Area").ToString Then
                If Not ComboBoxAreaFilter.Items.Contains(Mid(row("Area").ToString, 1, 1)) Then
                    ComboBoxAreaFilter.Items.Add(UCase(Mid(row("Area").ToString, 1, 1)))
                End If
            End If
            Area = row("Area").ToString
        Next
        ComboBoxAreaFilter.Sorted = True
    End Sub

    Sub FillResponsibleCombo()
        Dim ProjectLeader = ""

        ComboBoxResponsibleFilter.Items.Clear()
        ComboBoxResponsibleFilter.Items.Add("")

        Dim rowResults As DataRow() = tblTP.Select("project like '*'", "ProjectLeader")
        For Each row In rowResults
            If ProjectLeader <> row("ProjectLeader").ToString Then ComboBoxResponsibleFilter.Items.Add(UCase(row("ProjectLeader").ToString))
            ProjectLeader = row("ProjectLeader").ToString
        Next
        ComboBoxResponsibleFilter.Sorted = True
    End Sub

    Function ProjectStatus(ByVal id As String, ByVal refresh As Boolean) As String
        ProjectStatus = "MISSING"
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
                Dim i As Integer = tbltp.Rows.Count
            Catch ex As Exception
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
		            AdapterTP.Fill(Dstp, "TimeProject")
                    tbltp = Dstp.Tables("TimeProject")
	            End Using
            End Try

            If refresh Then
                Dstp.Clear()
                tbltp.Clear()
                Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
		            AdapterTP.Fill(Dstp, "TimeProject")
                    tbltp = Dstp.Tables("TimeProject")
	            End Using
            End If
        End Using
        Dim rowShow As DataRow() = tbltp.Select("Project = '" & id & "'")

        For Each row In rowShow

            Dim timingTStr As Object = TimingTS(row("start").ToString, row("end").ToString, row("compleated").ToString, row("status").ToString)
            Application.DoEvents()
            If timingTStr = "ONTIME" And (ProjectStatus = "ONTIME" Or ProjectStatus = "CLOSED" Or ProjectStatus = "MISSING") Then
                ProjectStatus = "ONTIME"
            ElseIf timingTStr = "DELAY" Or ProjectStatus = "CRITIC" Then
                If (ProjectStatus <> "DELAY") And DelayAdvanceTimeTask(row("taskname").ToString, row("project").ToString, True) > -yelloDelay Then
                    ProjectStatus = "CRITIC"
                Else
                    ProjectStatus = "DELAY"
                    Exit For
                End If
            ElseIf timingTStr = "STANDBY" Then
                ProjectStatus = "STANDBY"
                Exit For
            ElseIf timingTStr = "CLOSED" And ProjectStatus = "CLOSED" Or ProjectStatus = "MISSING" Then
                ProjectStatus = "CLOSED"
            Else
                ProjectStatus = ""
            End If
        Next

    End Function

    Function DelayAdvanceTime(ByVal id As String, ByVal refresh As Boolean) As Integer
        Dim totaltime As Integer = DateDiff("d", string_to_date(ProjectStart(id, False)), string_to_date(ProjectEnd(id, False)))
        DelayAdvanceTime = DateDiff("d", Today, DateAdd("d", Int(totaltime * (ProjectCompleated(id, False)) / 100), string_to_date(ProjectStart(id, False))))
    End Function

    Function DelayAdvanceTimeTask(ByVal id As String, ByVal idp As String, ByVal refresh As Boolean) As Integer
        Dim totaltime As Integer = DateDiff("d", string_to_date(TaskStart(id, idp, True)), string_to_date(TaskEnd(id, idp, True)))
        DelayAdvanceTimeTask = DateDiff("d", Today, DateAdd("d", Int(totaltime * Val(TaskCompleated(id, idp, False)) / 100), string_to_date(TaskStart(id, idp, False))))
    End Function

    Function TaskCompleated(ByVal id As String, ByVal idp As String, ByVal refresh As Boolean) As String
        TaskCompleated = ""
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using

        Dim rowShow As DataRow() = tbltp_static.Select("taskname = '" & id & "' and project = '" & idp & "'")

        For Each row In rowShow
            TaskCompleated = row("Compleated").ToString
        Next

    End Function

    Function ProjectCompleated(ByVal id As String, ByVal refresh As Boolean) As Integer

        ProjectCompleated = 0
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using

        Dim TotalTime As Integer = 0
        Dim rowShow As DataRow() = tbltp_static.Select("Project = '" & id & "'")
        If rowShow.Length > 0 Then
            For Each row In rowShow
                TotalTime = TotalTime + DateDiff("d", string_to_date(row("start").ToString), string_to_date(row("end").ToString))
                ProjectCompleated = ProjectCompleated + Val(row("Compleated").ToString) * DateDiff("d", string_to_date(row("start").ToString), string_to_date(row("end").ToString))
            Next

            If TotalTime > 0 Then ProjectCompleated = ProjectCompleated / TotalTime
        Else
            ProjectCompleated = 100
        End If

    End Function

    Private Sub ButtonShow_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ButtonShow.Click
        If ButtonShow.Text = "Show" Then
            ButtonShow.Text = "Stop"
            TimerShow.Enabled = True
            TimerShow.Start()
            LabelStatusFilter.Visible = False
            LabelCustomerFilter.Visible = False
            LabelResponsibleFilter.Visible = False
            LabelAreaFilter.Visible = False
            ComboBoxStatusFilter.Visible = False
            ComboBoxResponsibleFilter.Visible = False
            ComboBoxCustomerFilter.Visible = False
            ComboBoxAreaFilter.Visible = False
        Else
            ButtonShow.Text = "Show"
            TimerShow.Enabled = False
            TimerShow.Stop()
            LabelStatusFilter.Visible = True
            LabelCustomerFilter.Visible = True
            LabelResponsibleFilter.Visible = True
            LabelAreaFilter.Visible = True
            ComboBoxStatusFilter.Visible = True
            ComboBoxResponsibleFilter.Visible = True
            ComboBoxCustomerFilter.Visible = True
            ComboBoxAreaFilter.Visible = True

        End If
        UpdateTreeProjectList(True)
    End Sub

    Function TaskStart(ByVal id As String, ByVal idp As String, ByVal refresh As Boolean) As String

        TaskStart = ""
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using


        Dim rowShow As DataRow() = tbltp_static.Select("taskname = '" & id & "' and project = '" & idp & "'")
        For Each row In rowShow
            TaskStart = row("Start").ToString
        Next

    End Function

    Function TaskEnd(ByVal id As String, ByVal idp As String, ByVal refresh As Boolean) As String

        TaskEnd = ""
        Dim  builder As  New Common.DbConnectionStringBuilder()
        builder.ConnectionString = ConfigurationManager.ConnectionStrings(hostName).ConnectionString
        Using con = NewConnectionMySql(builder("host"), builder("database"), builder("username"), builder("password"))
	        Try
		        Dim i As Integer = tbltp_static.Rows.Count
	        Catch ex As Exception
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tbltp_static = Dstp_static.Tables("TimeProject")
		        End Using
	        End Try

	        If refresh Then
		        Dstp_static.Clear()
		        tbltp_static.Clear()
		        Using AdapterTP As New MySqlDataAdapter("SELECT * FROM TimeProject", con)
			        AdapterTP.Fill(Dstp_static, "TimeProject")
			        tblTP = Dstp_static.Tables("TimeProject")
		        End Using
	        End If
        End Using

        Dim rowShow As DataRow() = tbltp_static.Select("taskname = '" & id & "' and project = '" & idp & "'")

        For Each row In rowShow
            TaskEnd = row("End").ToString
        Next

    End Function

    Private Sub ButtonClose_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ButtonClose.Click
        Me.Dispose()
    End Sub

    Private Sub FormTimeShow_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseClick
        ButtonClose.Visible = Not ButtonClose.Visible
        ButtonShow.Visible = Not ButtonShow.Visible

    End Sub

    Private Sub TimerProjectList_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles TimerProjectList.Tick
        UpdateTreeProjectList(True)
    End Sub

End Class