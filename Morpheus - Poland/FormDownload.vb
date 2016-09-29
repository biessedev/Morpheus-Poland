﻿Option Explicit On
Option Compare Text
Imports MySql.Data.MySqlClient
Imports System.IO
Imports System.Data.SqlClient
Imports System.Text.RegularExpressions
Imports System.Threading


Public Class FormDownload
    Public loadDoc As Boolean
    Dim AdapterDoc As New MySqlDataAdapter("SELECT * FROM doc", MySqlconnection)
    Dim AdapterDocType As New MySqlDataAdapter("SELECT * FROM Doctype", MySqlconnection)
    Dim AdapterDocProd As New MySqlDataAdapter("SELECT * FROM Product", MySqlconnection)
    Dim AdapterCust As New MySqlDataAdapter("SELECT * FROM customer", MySqlconnection)
    Dim AdapterBom As New MySqlDataAdapter("SELECT * FROM sigip", MySqlconnection)
    Dim tblDoc As DataTable, tblDocType As DataTable, tblDocProd As DataTable, tblDocCust As DataTable, tlbDocGru As New DataTable, tlbDocGrutype As New DataTable
    Dim DsDoc As New DataSet, DsDocType As New DataSet, DsDocProd As New DataSet, DsDocCust As New DataSet, DsDocGru As New DataSet, DsDocGrutype As New DataSet
    Dim DsDocComp As New DataSet, DsBom As New DataSet
    Dim tblDocComp As New DataTable, tblBom As DataTable
    Dim BooAvvio As Boolean = True, stopEvent As Boolean
    Dim trd As Thread
    Dim Autoupdate As Boolean
    Dim trdFinish As Boolean = False
    Dim OrcadDBAdr = ParameterTable("OrcadDBAdr")
    Dim OrcadDBName = ParameterTable("OrcadDBName")
    Dim OrcadDBUser = ParameterTable("OrcadDBUser")
    Dim OrcadDBPwd = ParameterTable("OrcadDBPwd")

    Private Sub FormDownload_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        FormStart.Show()

        If trdFinish Then CloseConnectionMySqlGru()

    End Sub

    Public Sub ThreadTask()
        ButtonConnection.BackColor = Color.Red
        ButtonOrcad.BackColor = Color.Red

        Try
            OpenConnectionMySqlGru("10.10.10.15", "Gestdoc", "chinadoc", "china")
            Dim AdapterDocGruProdType As New MySqlDataAdapter("SELECT * FROM tipodoc", MySqlconnectionGru)
            AdapterDocGruProdType.Fill(DsDocGrutype, "tipodoc")
            tlbDocGrutype = DsDocGrutype.Tables("tipodoc")
            ButtonConnection.BackColor = Color.Green
            Application.DoEvents()

        Catch ex As Exception
            MsgBox("Database Account error, server Grugliasco open procedure")

        End Try

        Try
            DsDocComp.Clear()
            tblDocComp.Clear()

        Catch ex As Exception

        End Try

        If trdFinish Then

            Try
                Dim AdapterDocComp As New SqlDataAdapter("SELECT * FROM orcadw.T_orcadcis where not valido = 'no_valido'", SQLconnectionOrcad)

                AdapterDocComp.Fill(DsDocComp, "orcadw.T_orcadcis")

                tblDocComp = DsDocComp.Tables("orcadw.T_orcadcis")
            Catch ex As Exception

                Try
                    CloseConnectionSqlOrcad()
                    'OpenConnectionMySqlOrcad("10.10.10.36", "orcad1", "orcadw", "orcadw")
                    OpenConnectionMySqlOrcad(OrcadDBAdr, OrcadDBName, OrcadDBUser, OrcadDBPwd)

                    Dim AdapterDocComp As New SqlDataAdapter("SELECT * FROM orcadw.T_orcadcis where not valido = 'no_valido'", SQLconnectionOrcad)

                    AdapterDocComp.Fill(DsDocComp, "orcadw.T_orcadcis")

                    tblDocComp = DsDocComp.Tables("orcadw.T_orcadcis")
                Catch
                    MsgBox("Error in opening Orcad Database! Now - " & ex.Message)

                End Try

            End Try

        End If

        If MySqlconnectionGru.State = ConnectionState.Closed Then CheckGru.Checked = False

        Dim RowSearchDoc As DataRow()
        AdapterBom.Fill(DsBom, "sigip")
        tblBom = DsBom.Tables("sigip")
        RowSearchDoc = tblBom.Select("doc=''")
        If RowSearchDoc.Length > 0 Then
            MsgBox("Is possible missing result please refresh sigip bom import")
        End If

        trdFinish = True

    End Sub
    Private Sub FormDownload_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Autoupdate = True

        trd = New Thread(AddressOf ThreadTask)
        trd.IsBackground = True
        trd.Start()

        Me.Focus()
        RadioButtonGeneralSearch.Checked = True
        RadioButtonProductSearch.Checked = False
        FillComboSign()
        FillComboRevision()

        Label1LastBomUpdate.Text = "Last bom update " & ParameterTable("LAST_SIGIP_BOM_UPDATE")
        AdapterDoc.Fill(DsDoc, "doc")
        tblDoc = DsDoc.Tables("doc")

        AdapterDocType.Fill(DsDocType, "DocType")
        tblDocType = DsDocType.Tables("DocType")

        AdapterDocProd.Fill(DsDocProd, "Product")
        tblDocProd = DsDocProd.Tables("Product")

        AdapterCust.Fill(DsDocCust, "customer")
        tblDocCust = DsDocCust.Tables("customer")

        FillComboEcrNull()
        FillComboEcrPending()

        FillComboFirstType()
        BooAvvio = False

        FillComboCust()
        FillComboProd()

        ComboBoxStatus.Items.Add("ALL - STATUS")
        ComboBoxStatus.Items.Add("SOP_SAMPLE")
        ComboBoxStatus.Items.Add("OBSOLETE")
        ComboBoxStatus.Items.Add("R&D_APPROVED")
        ComboBoxStatus.Items.Add("LOGISTIC_APPROVED")
        ComboBoxStatus.Items.Add("CUSTOMER_APPROVED")
        ComboBoxStatus.Items.Add("PURCHASING_APPROVED")
        ComboBoxStatus.Items.Add("ENGINEERING_APPROVED")
        ComboBoxStatus.Items.Add("PROCESS_ENG_APPROVED")
        ComboBoxStatus.Items.Add("IDN_ENG_APPROVED")
        ComboBoxStatus.Items.Add("PRODUCTION_APPROVED")
        ComboBoxStatus.Items.Add("FINANCIAL_APPROVED")
        ComboBoxStatus.Items.Add("MPA_APPROVED")

        Me.Focus()
        ListView1.Columns.Clear()
        Autoupdate = False
    End Sub
    Private Sub ComboBoxFirstType_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBoxFirstType.TextChanged
        Dim strOld As String = ""
        ComboBoxSecondType.Items.Clear()
        Dim returnValue As DataRow()
        returnValue = tblDocType.Select("FirstType='" & ComboBoxFirstType.Text & "'", "SecondType DESC")
        For Each row In returnValue
            If StrComp(strOld, row("SecondType").ToString) <> 0 Then
                strOld = row("SecondType").ToString
                ComboBoxSecondType.Items.Add(row("SecondType"))
            End If
        Next
        ComboBoxSecondType.Items.Add("")
        ComboBoxSecondType.Sorted = True
        ComboBoxSecondType.Text = ""
        ComboBoxThirdType.Text = ""
    End Sub
    Private Sub ComboBoxSecondType_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBoxSecondType.TextChanged
        Dim returnValue As DataRow()
        ComboBoxThirdType.Items.Clear()
        Dim strOld As String = ""
        returnValue = tblDocType.Select("FirstType='" & ComboBoxFirstType.Text & "' and SecondType='" & ComboBoxSecondType.Text & "'", "SecondType DESC")
        For Each row In returnValue
            If StrComp(strOld, row("ThirdType").ToString) <> 0 Then
                strOld = row("ThirdType").ToString
                ComboBoxThirdType.Items.Add(row("ThirdType"))
            End If
        Next
        ComboBoxThirdType.Items.Add("")
        ComboBoxThirdType.Sorted = True
        ComboBoxThirdType.Text = ""
    End Sub
    Private Sub ButtonQuery_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonQuery.Click
        Dim Inconsistent As Boolean = False
        If Autoupdate = False Then
            Dim I As Integer, J As Integer, oldBitronPn As String
            Dim RowSearch As DataRow(), rowType As DataRow(), strPcbCode As String = "", strPiastraCode As String = "", SQL As String = "", assieme As String = ""
            Dim mech As String, strSoftwareCode As String = "", strLabelCode As String = "", resultProdList As DataRow(), LastRowList As Integer
            Dim dima As Boolean, sw As Boolean, listLengh As Long

            WriteFile("Info " & Now.ToString, False)
            stopEvent = False
            ListBoxLog.Items.Clear()
            ListView1.Clear()
            ListBoxLog.Items.Add("Query started.....")
            ListBoxLog.SelectedIndex = ListBoxLog.Items.Count - 1

            AdapterBom = New MySqlDataAdapter("SELECT * FROM sigip ", MySqlconnection)
            AdapterBom.Fill(DsBom, "sigip")
            tblBom = DsBom.Tables("sigip")
            tblBom.Clear()
            DsBom.Clear()
            If Not BooAvvio Then

                AdapterDoc.SelectCommand = New MySqlCommand("SELECT * FROM DOC;", MySqlconnection)
                DsDoc.Clear()
                tblDoc.Clear()
                AdapterDoc.Fill(DsDoc, "doc")
                tblDoc = DsDoc.Tables("doc")

                If RadioButtonGeneralSearch.Checked Then
                    'Try

                    'SQL = "(ecrNull like '*" & ComboBoxEcrNull.Text & "*') and  (ecrpending like '*" & ComboBoxEcrPending.Text & "*') and  (filename like '*" & TextBoxfileName.Text & "*') and ( header like '" & IIf(Mid(ComboBoxFirstType.Text, 1, 3) = "", "*", Mid(ComboBoxFirstType.Text, 1, 3) & "_") _
                    '              & IIf(Mid(ComboBoxSecondType.Text, 1, 3) = "", "*", Mid(ComboBoxSecondType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxThirdType.Text, 1, 3) = "", "*", Mid(ComboBoxThirdType.Text, 1, 3)) & "')"

                    SQL = "(filename like '*" & TextBoxfileName.Text & "*') and ( header like '" & IIf(Mid(ComboBoxFirstType.Text, 1, 3) = "", "*", Mid(ComboBoxFirstType.Text, 1, 3) & "_") _
                                  & IIf(Mid(ComboBoxSecondType.Text, 1, 3) = "", "*", Mid(ComboBoxSecondType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxThirdType.Text, 1, 3) = "", "*", Mid(ComboBoxThirdType.Text, 1, 3)) & "')"

                    RowSearch = tblDoc.Select(SQL, "id")

                    FillListView(RowSearch)

                    'Catch ex As Exception
                    '    MsgBox("Error in file name! Please change.")
                    'End Try

                Else
                    Dim ProdControl As String
                    ListView1.Clear()
                    If ComboBoxProd.Text <> "" Then

                        resultProdList = tblDocProd.Select(IIf(CheckBoxObsolete.Checked = True, "(status like '*') ", " (not status = 'OBSOLETE') ") & _
                        " AND (status " & IIf(ComboBoxStatus.Text = "ALL - STATUS", "like '*')", "like '" & ComboBoxStatus.Text & "')") & "  and bitronpn LIKE '" & IIf(Mid(ComboBoxProd.Text, 1, 3) = "ALL", "*", Trim(Mid(ComboBoxProd.Text, 1, InStr(1, ComboBoxProd.Text, "-", CompareMethod.Text) - 1))) & _
                                        "' and customer like '" & IIf(ComboBoxCustomer.Text = "ALL - CUSTOMER", "*", ComboBoxCustomer.Text) & "'")
                        For Each rowPrdList In resultProdList
                            Application.DoEvents()
                            If ComboBoxSign.Text = "UNSIGNED" Then
                                CheckGru.Enabled = False
                                CheckComp.Enabled = False
                                TextBoxCompPn.Enabled = False
                            ElseIf ComboBoxSign.Text = "SIGNED" Then
                                CheckGru.Enabled = True
                                CheckComp.Enabled = True
                                If CheckGru.Checked And Not stopEvent And MySqlconnectionGru.State = ConnectionState.Open Then
                                    DsDocGru.Clear()
                                    tlbDocGru.Clear()
                                    Application.DoEvents()
                                    Dim prodDoc As String, proddocAux As String

                                    prodDoc = rowPrdList("bitronpn").ToString
                                    proddocAux = rowPrdList("piastracode").ToString
                                    Try
                                        Dim AdapterDocGruProd As New MySqlDataAdapter("SELECT * FROM documento where codicepf = '" & prodDoc & "' or codicepf = '" & proddocAux & "'", MySqlconnectionGru)
                                        AdapterDocGruProd.Fill(DsDocGru, "documento")
                                    Catch ex As Exception
                                        'ListBoxLog.Items.Add("Connection lost, need waithing 20 second...")
                                        'CloseConnectionMySqlGru()
                                        'OpenConnectionMySqlGru("10.10.10.15", "Gestdoc", "chinadoc", "china")
                                        'If MySqlconnectionGru.State = ConnectionState.Open Then
                                        '    ListBoxLog.Items.Add("Connection enstabilish...Done!")
                                        '    Dim AdapterDocGruProd As New MySqlDataAdapter("SELECT * FROM documento where codicepf = '" & prodDoc & "' or codicepf = '" & proddocAux & "'", MySqlconnectionGru)
                                        '    AdapterDocGruProd.Fill(DsDocGru, "documento")
                                        'End If
                                    End Try
                                    If MySqlconnectionGru.State = ConnectionState.Open Then
                                        tlbDocGru = DsDocGru.Tables("documento")
                                        If prodDoc = "" Then prodDoc = proddocAux
                                        If proddocAux = "" Then proddocAux = prodDoc
                                        RowSearch = tlbDocGru.Select("( codicepf LIKE '" & prodDoc & "' or codicepf LIKE '" & proddocAux & "')  ")

                                        dima = False
                                        sw = False
                                        ProdControl = fControl(rowPrdList("bitronpn").ToString, strPcbCode, strPiastraCode)
                                        listLengh = ListView1.Items.Count
                                        Inconsistent = False
                                        WriteFile(rowPrdList("bitronpn").ToString & "   " & rowPrdList("name").ToString & " <<-->> " & rowPrdList("status").ToString, True)

                                        For Each row In RowSearch
                                            Application.DoEvents()
                                            If (row("visto").ToString <> "" And row("data_obso").ToString = "") And (row("codicepf").ToString = prodDoc Or row("codicepf").ToString = proddocAux) Then

                                                Dim ssr(tblDoc.Columns.Count) As String
                                                ssr(0) = "GRU"
                                                If InStr(tipodoc(row("coddoc").ToString), "dima", CompareMethod.Text) > 0 Then dima = True
                                                If InStr(tipodoc(row("coddoc").ToString), "sw", CompareMethod.Text) > 0 Then sw = True
                                                ssr(1) = prodDoc & " -- " & tipodoc(row("coddoc").ToString)
                                                ssr(2) = row("allegato").ToString
                                                ssr(11) = FileNameDes(rowPrdList("BITRONPN").ToString)
                                                Dim kk As New ListViewItem(ssr)
                                                ListView1.Items.Add(kk)
                                                ListView1.Items(ListView1.Items.Count - 1).BackColor = Color.Aqua

                                                For Each rowCk In RowSearch
                                                    Application.DoEvents()
                                                    If InStr(rowCk("allegato").ToString, "ECR", CompareMethod.Text) = 0 And _
                                                        InStr(rowCk("allegato").ToString, "RMP", CompareMethod.Text) = 0 And (rowCk("data_obso").ToString = "" And rowCk("visto").ToString <> "" And row("visto").ToString <> "" And row("data_obso").ToString = "") Then
                                                        Try
                                                            If (Mid(rowCk("allegato").ToString, 1, InStrRev(rowCk("allegato").ToString, "_") - 1) = Mid(row("allegato").ToString, 1, InStrRev(row("allegato").ToString, "_") - 1)) And _
                                                                (Mid(rowCk("allegato").ToString, InStr(rowCk("allegato").ToString, ".") + 1) = Mid(row("allegato").ToString, InStr(row("allegato").ToString, ".") + 1)) And _
                                                                rowCk("allegato").ToString <> row("allegato").ToString Then
                                                                ListBoxLog.Items.Add("Inconsistent file : " & row("allegato").ToString & "  and  " & rowCk("allegato").ToString)
                                                                WriteFile(("--> Inconsistent file : " & row("allegato").ToString & "  and  " & rowCk("allegato").ToString), True)
                                                                Inconsistent = True
                                                            End If
                                                        Catch ex As Exception

                                                        End Try
                                                    End If
                                                Next

                                            End If

                                        Next
                                        dima = True

                                        If (Presence("F", ProdControl) = "1" And Not sw) Or Not dima Or listLengh = ListView1.Items.Count Or Inconsistent Then
                                        Else
                                            WriteFile(" --> All Doc OK!", True)
                                        End If
                                        If listLengh = ListView1.Items.Count Then
                                            ListBoxLog.Items.Add(rowPrdList("bitronpn").ToString & " - Document NOT found in Intranet!")
                                            WriteFile(" --> Document not Find In Intranet!", True)
                                        End If

                                        If Not dima Then
                                            'ListBoxLog.Items.Add(rowPrdList("bitronpn").ToString & " - Dima PCB not Find!")
                                            'WriteFile(" --> Dima PCB not Find!!", True)
                                        End If

                                        If Presence("F", ProdControl) = "1" Then
                                            If Not sw Then ListBoxLog.Items.Add(rowPrdList("bitronpn").ToString & " - Software NOT found in Intranet!")
                                            If Not sw Then WriteFile(" --> Software NOT found in Intranet!", True)
                                        End If
                                        If Presence("F", ProdControl) = "1" Or Not dima Or listLengh = ListView1.Items.Count Then WriteFile("", True)
                                    End If
                                End If
                                If CheckComp.Checked And Not stopEvent Then

                                    Dim prodDoc As String
                                    prodDoc = rowPrdList("bitronpn").ToString

                                    AdapterBom = New MySqlDataAdapter("SELECT * FROM sigip where bom = '" & prodDoc & "' and ACQ_FAB like 'ACQ' ", MySqlconnection)
                                    AdapterBom.Fill(DsBom, "sigip")
                                    tblBom = DsBom.Tables("sigip")


                                End If
                            End If

                            If Not stopEvent Then

                                ProdControl = fControl(rowPrdList("bitronpn").ToString, strPcbCode, strPiastraCode)

                                'RowSearch = tblDoc.Select("ecrPending like '*" & ComboBoxEcrPending.Text & "*' and ecrNull like '*" & _
                                '            ComboBoxEcrNull.Text & "*' AND ( header like '" & IIf(Mid(ComboBoxFirstType.Text, 1, 3) = "", "*", Mid(ComboBoxFirstType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxSecondType.Text, 1, 3) = "", "*", Mid(ComboBoxSecondType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxThirdType.Text, 1, 3) = "", "*", Mid(ComboBoxThirdType.Text, 1, 3)) & "') AND (filename like '" & _
                                '            ComboBoxProd.Text & "' or filename like '*" & rowPrdList("bitronpn").ToString & "*')", "rev DESC")


                                RowSearch = tblDoc.Select("header like '" & IIf(Mid(ComboBoxFirstType.Text, 1, 3) = "", "*", Mid(ComboBoxFirstType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxSecondType.Text, 1, 3) = "", "*", Mid(ComboBoxSecondType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxThirdType.Text, 1, 3) = "", "*", Mid(ComboBoxThirdType.Text, 1, 3)) & "' AND (filename like '" & _
                                            ComboBoxProd.Text & "' or filename like '*" & rowPrdList("bitronpn").ToString & "*')", "rev DESC")


                                ' or filename like '" & rowPrdList("bitronpn").ToString & " 
                                FillListView(RowSearch, True, rowPrdList("BITRONPN").ToString)
                                'FillListView(RowSearch, True)

                                ' IMMISSIONE GRUPPI
                                GroupList = rowPrdList("GROUPLIST").ToString()

                                If GroupList <> "" Then
                                    Dim str(2) As String
                                    I = 1
                                    J = InStr(GroupList, "]", CompareMethod.Text)
                                    While J > 0
                                        RowSearch = tblDoc.Select("(HEADER = '" & Mid(GroupList, I, 11) & "' AND filename = '" & Mid(GroupList, I + 12, J - 12 - I) _
             & "') and ( header like '" & IIf(Mid(ComboBoxFirstType.Text, 1, 3) = "", "*", Mid(ComboBoxFirstType.Text, 1, 3) & "_") _
                                        & IIf(Mid(ComboBoxSecondType.Text, 1, 3) = "", "*", Mid(ComboBoxSecondType.Text, 1, 3) & "_") _
                                        & IIf(Mid(ComboBoxThirdType.Text, 1, 3) = "", "*", Mid(ComboBoxThirdType.Text, 1, 3)) & "')")
                                        If RowSearch.Length > 0 Then
                                            FillListView(RowSearch, True, rowPrdList("BITRONPN").ToString)
                                        End If
                                        Dim ii As New ListViewItem(str)
                                        I = J + 2
                                        J = InStr(I + 1, GroupList, "]", CompareMethod.Text)
                                    End While
                                End If

                                If ProdControl <> "" Then
                                    rowType = tblDocType.Select("control like '*P*' AND ( header like '" & IIf(Mid(ComboBoxFirstType.Text, 1, 3) = "", "*", Mid(ComboBoxFirstType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxSecondType.Text, 1, 3) = "", "*", Mid(ComboBoxSecondType.Text, 1, 3) & "_") & IIf(Mid(ComboBoxThirdType.Text, 1, 3) = "", "*", Mid(ComboBoxThirdType.Text, 1, 3)) & "')")
                                    Dim row As DataRow
                                    For Each row In rowType
                                        If Presence(Presence("Y", row("Control").ToString), ProdControl) = "1" Then
                                            Dim ListPresence As Boolean = False
                                            For I = LastRowList To ListView1.Items.Count - 1
                                                If ListView1.Items(I).SubItems(1).Text = row("header").ToString Then
                                                    ListPresence = True
                                                End If
                                            Next
                                            If Not ListPresence And ComboBoxSign.Text = "SIGNED" Then
                                                If row("header").ToString <> ParameterTable("plant") & "R_PRO_MED" Then
                                                    Dim str(tblDoc.Columns.Count) As String
                                                    str(1) = row("header").ToString
                                                    str(2) = rowPrdList("BITRONPN").ToString & " -- File required But Missing!"
                                                    ' If row("header").ToString = parametertable("plant") & "R_PRO_DNS" Then str(2) = rowPrdList("BITRONPN").ToString & " -- File recommended But Missing!"
                                                    ' If row("header").ToString = parametertable("plant") & "R_PRO_ASR" Then str(2) = rowPrdList("BITRONPN").ToString & " -- File recommended But Missing!"
                                                    str(11) = FileNameDes(rowPrdList("bitronpn").ToString)
                                                    Dim ii As New ListViewItem(str)
                                                    ListView1.Items.Add(ii)
                                                    ListView1.Items(ListView1.Items.Count - 1).BackColor = Color.Yellow
                                                    ' If row("header").ToString = parametertable("plant") & "R_PRO_DNS" Then ListView1.Items(ListView1.Items.Count - 1).BackColor = Color.YellowGreen
                                                    ' If row("header").ToString = parametertable("plant") & "R_PRO_ASR" Then ListView1.Items(ListView1.Items.Count - 1).BackColor = Color.YellowGreen
                                                End If
                                            End If
                                        End If
                                    Next

                                    ' Find mech element

                                    Dim ResultDoc As DataRow(), ResultProd As DataRow(), mchElement As String

                                    ResultProd = tblDocProd.Select("bitronpn = '" & rowPrdList("BITRONPN").ToString & "'")

                                    If ParameterTable("plant") & "R_PRO_MED" = Mid(ComboBoxFirstType.Text, 1, 3) & "_" & Mid(ComboBoxSecondType.Text, 1, 3) & "_" & Mid(ComboBoxThirdType.Text, 1, 3) Or _
                                    ParameterTable("plant") & "R_PRO_" = Mid(ComboBoxFirstType.Text, 1, 3) & "_" & Mid(ComboBoxSecondType.Text, 1, 3) & "_" & Mid(ComboBoxThirdType.Text, 1, 3) Or _
                                    ParameterTable("plant") & "R__" = Mid(ComboBoxFirstType.Text, 1, 3) & "_" & Mid(ComboBoxSecondType.Text, 1, 3) & "_" & Mid(ComboBoxThirdType.Text, 1, 3) Or _
                                    "__" = Mid(ComboBoxFirstType.Text, 1, 3) & "_" & Mid(ComboBoxSecondType.Text, 1, 3) & "_" & Mid(ComboBoxThirdType.Text, 1, 3) Then
                                        mech = ResultProd(0).Item("mchelement").ToString

                                        For I = 0 To Int(Len(mech) / 60) - 1
                                            mchElement = Trim(Mid(mech, I * 60 + 1, 20))
                                            ResultDoc = tblDoc.Select("header = '" & ParameterTable("plant") & "R_PRO_MED' and filename like '" & mchElement & "*'")
                                            If ResultDoc.Length > 0 Then
                                                FillListView(ResultDoc, True, rowPrdList("BITRONPN").ToString)
                                            ElseIf ComboBoxSign.Text = "SIGNED" Then
                                                Dim str(tblDoc.Columns.Count - 1) As String
                                                str(1) = ParameterTable("plant") & "R_PRO_MED"
                                                str(2) = mchElement & " -- File required But Missing!"
                                                str(11) = FileNameDes(rowPrdList("BITRONPN").ToString)
                                                Dim ii As New ListViewItem(str)
                                                ListView1.Items.Add(ii)
                                                ListView1.Items(ListView1.Items.Count - 1).BackColor = Color.Yellow
                                            End If
                                        Next
                                    End If
                                Else
                                    ComunicationLog("5063") ' Product not found in product list

                                End If
                                LastRowList = ListView1.Items.Count
                            End If

                        Next

                    Else
                        ComunicationLog("0012")
                    End If
                End If

                RowSearch = tblBom.Select("bitron_pn like '" & IIf(TextBoxCompPn.Text = "", "*", TextBoxCompPn.Text) & "'", "bitron_pn")
                Application.DoEvents()
                oldBitronPn = ""
                For Each row In RowSearch
                    Application.DoEvents()
                    If oldBitronPn <> row("bitron_pn").ToString Then
                        If Mid(row("bitron_pn").ToString, 1, 2) <> "18" Then
                            Dim ssr(tblDoc.Columns.Count) As String
                            If row("acq_fab").ToString = "FAB" Then
                                ssr(0) = "FAB"
                            ElseIf Mid(row("doc").ToString, 1, 2) = "HC" Then
                                ssr(0) = "HC"
                            ElseIf Mid(row("doc").ToString, 1, 7) = "SRV_DOC" Then
                                ssr(0) = RevisionLast(row("doc").ToString)
                            Else
                                ssr(0) = "MISS"
                            End If

                            ssr(1) = row("acq_fab").ToString
                            ssr(2) = row("bitron_pn").ToString & " - " & row("des_pn").ToString
                            ssr(11) = "Documentation: " & row("doc").ToString
                            Dim kk As New ListViewItem(ssr)
                            ListView1.Items.Add(kk)
                            If row("doc").ToString = "NO" And row("acq_fab").ToString = "ACQ" Then ListView1.Items(ListView1.Items.Count - 1).BackColor = Color.Yellow

                        End If
                    End If
                    oldBitronPn = row("bitron_pn").ToString

                Next
            End If

            If CheckBoxSaveInfo.Checked Then
                SaveFileDialog1.FileName = System.IO.Path.GetTempPath & "SrvQueryLog.txt"
                SaveFileDialog1.ShowDialog()
                Try
                    If SaveFileDialog1.FileName <> "" Then FileCopy(System.IO.Path.GetTempPath & "SrvQueryLog.txt", SaveFileDialog1.FileName)

                Catch ex As Exception

                End Try
            End If
            ListBoxLog.Items.Add("Query Done!")
            ListBoxLog.SelectedIndex = ListBoxLog.Items.Count - 1
        Else
            If Not trdFinish And Not Autoupdate Then MsgBox("Wait one moment!")
        End If

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonBrowse.Click

        FolderBrowserDialog1.ShowDialog()
        If FolderBrowserDialog1.SelectedPath <> "" Then
            TextBoxFilePath.Text = FolderBrowserDialog1.SelectedPath
        End If
        FolderBrowserDialog1.Dispose()
    End Sub

    Private Sub RadioButtonGeneralSearch_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButtonGeneralSearch.CheckedChanged
        If RadioButtonProductSearch.Checked And Not RadioButtonGeneralSearch.Checked Then
        Else
            RadioButtonProductSearch.Checked = Not RadioButtonGeneralSearch.Checked
        End If
    End Sub

    Private Sub ButtonReset_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonReset.Click
        FillComboFirstType()
    End Sub
    Private Sub ButtonDelete_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonDelete.Click
        Dim strRes As String
        Dim strPathFtp As String
        Dim objFtp As ftp = New ftp()
        objFtp.UserName = strFtpServerUser
        objFtp.Password = strFtpServerPsw
        objFtp.Host = strFtpServerAdd

        Dim sql As String, cmd As MySqlCommand, rev As Integer
        If MsgBox("Are you sure to delete a document?" & vbCrLf & "Please consider that when you delete a file with revisione greater than 0, automatically you will validate the " _
                  & "file in the server with previous revision index!!! Please care about this.", MsgBoxStyle.YesNo, "SRVDOC - File delete") = vbYes Then
            If ListView1.CheckedItems.Count = 1 Then
                RevisionExtract(rev, ListView1.CheckedItems(0).SubItems(1).Text(), ListView1.CheckedItems(0).SubItems(2).Text(), ListView1.CheckedItems(0).SubItems(4).Text())
                If Trim(Str(rev)) = ListView1.CheckedItems(0).SubItems(3).Text() Then
                    If (controlRight(Mid(ListView1.CheckedItems(0).SubItems(1).Text, 3, 1)) >= 2 And (ListView1.CheckedItems(0).SubItems(6).Text = "" Or ListView1.CheckedItems(0).SubItems(6).Text Like "NoSignReq*")) Or controlRight(Mid(ListView1.CheckedItems(0).SubItems(1).Text, 3, 1)) >= 3 Then
                        Try
                            Try
                                strPathFtp = (Mid(ListView1.CheckedItems(0).SubItems(1).Text, 1, 3) & "/" & ListView1.CheckedItems(0).SubItems(1).Text)
                                strRes = objFtp.DeleteFile(strPathFtp & "/", ListView1.CheckedItems(0).SubItems(1).Text & "_" & ListView1.CheckedItems(0).SubItems(2).Text() & "_" & ListView1.CheckedItems(0).SubItems(3).Text() & "." & ListView1.CheckedItems(0).SubItems(4).Text())
                            Catch ex As Exception
                                ComunicationLog("0056") ' ftp operation error
                            End Try
                            strPathFtp = ("/" & Mid(ListView1.CheckedItems(0).SubItems(1).Text, 1, 3) & "/" & ListView1.CheckedItems(0).SubItems(1).Text)
                            strRes = objFtp.ListDirectory(strPathFtp & "/", ListView1.CheckedItems(0).SubItems(1).Text & "_" & ListView1.CheckedItems(0).SubItems(2).Text() & "_" & ListView1.CheckedItems(0).SubItems(3).Text() & "." & ListView1.CheckedItems(0).SubItems(4).Text())

                            sql = "DELETE FROM `" & DBName & "`.`doc` WHERE `doc`.`id` = " & ListView1.CheckedItems(0).SubItems(0).Text & ""
                            cmd = New MySqlCommand(sql, MySqlconnection)
                            cmd.ExecuteNonQuery()

                            ComunicationLog("5057") ' document deleted
                        Catch ex As Exception
                            ComunicationLog("0043") ' Mysql delete error 
                        End Try
                    Else
                        ComunicationLog("0043") 'no enough right
                    End If
                Else
                    ComunicationLog("5060")
                End If
            Else
                MsgBox("Only one file at a time can be deleted!")
            End If
        Else
            ComunicationLog("5058")
        End If

    End Sub

    Private Sub ButtonDownload_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonDownload.Click
        Dim objFtp As ftp = New ftp()
        Dim strPathFtp As String
        ListBoxLog.Items.Clear()
        Application.DoEvents()
        objFtp.UserName = strFtpServerUser
        objFtp.Password = strFtpServerPsw
        objFtp.Host = strFtpServerAdd
        Dim rgPattern = "[\\\/:\*\?""'<>|]"
        Dim objRegEx As New Regex(rgPattern)
        Dim fileDetail As IO.FileInfo
        Dim i As Integer
        Dim cmd As New MySqlCommand()
        If TextBoxFilePath.Text <> "" Then
            For i = 0 To ListView1.CheckedItems.Count - 1
                Application.DoEvents()
                strPathFtp = ("/" & Mid(ListView1.CheckedItems(i).SubItems(1).Text, 1, 3) & "/" & ListView1.CheckedItems(i).SubItems(1).Text & "/")
                If "GRU" = (ListView1.CheckedItems(i).SubItems(0).Text) Then
                    Application.DoEvents()
                    Try
                        ListBoxLog.Items.Add("Download Web....")
                        ListBoxLog.Items.Add(ListView1.CheckedItems(i).SubItems(1).Text & "" & ListView1.CheckedItems(i).SubItems(2).Text() & "  " & ListView1.CheckedItems(i).SubItems(3).Text() & ListView1.CheckedItems(i).SubItems(4).Text())

                        Application.DoEvents()
                        My.Computer.Network.DownloadFile("http://10.10.10.15/grugliasco/gestdoc/attach/" & ListView1.CheckedItems(i).SubItems(2).Text, TextBoxFilePath.Text & "\" & ListView1.CheckedItems(i).SubItems(2).Text, "", "", False, 10000, True)
                        ComunicationLog("5076") ' file download from web
                        ListBoxLog.Items.Add("")
                    Catch ex As Exception
                        MsgBox("Document not present in Bitron Intranet. Error in Intranet DB")
                    End Try
                ElseIf (ListView1.CheckedItems(i).SubItems(0).Text) = "HC" Then
                    ListBoxLog.Items.Add("Download HC....")
                    ListBoxLog.Items.Add(ListView1.CheckedItems(i).SubItems(1).Text & "_" & ListView1.CheckedItems(i).SubItems(2).Text() & "_" & ListView1.CheckedItems(i).SubItems(3).Text() & "." & ListView1.CheckedItems(i).SubItems(4).Text())
                    My.Computer.Network.DownloadFile("http://10.10.10.15/orcad/carica_file_pdf.php?cod_comp=" & Mid(ListView1.CheckedItems(i).SubItems(11).Text, 20), TextBoxFilePath.Text & "\" & objRegEx.Replace(ListView1.CheckedItems(i).SubItems(2).Text, "") & ".pdf", "", "", False, 10000, True)
                    ComunicationLog("5076")
                    ListBoxLog.Items.Add("")
                ElseIf IsNumeric((ListView1.CheckedItems(i).SubItems(0).Text)) Then
                    Dim repeatDownload As Integer = 0

                    While repeatDownload < 3

                        ListBoxLog.Items.Add("Download Local....")
                        ListBoxLog.Items.Add(ListView1.CheckedItems(i).SubItems(1).Text & "_" & ListView1.CheckedItems(i).SubItems(2).Text() & "_" & ListView1.CheckedItems(i).SubItems(3).Text() & "." & ListView1.CheckedItems(i).SubItems(4).Text())
                        objFtp.DownloadFile(strPathFtp, TextBoxFilePath.Text, ListView1.CheckedItems(i).SubItems(1).Text & "_" & ListView1.CheckedItems(i).SubItems(2).Text() & "_" & ListView1.CheckedItems(i).SubItems(3).Text() & "." & ListView1.CheckedItems(i).SubItems(4).Text())
                        fileDetail = My.Computer.FileSystem.GetFileInfo(TextBoxFilePath.Text & "\" & ListView1.CheckedItems(i).SubItems(1).Text & "_" & ListView1.CheckedItems(i).SubItems(2).Text() & "_" & ListView1.CheckedItems(i).SubItems(3).Text() & "." & ListView1.CheckedItems(i).SubItems(4).Text())
                        If fileDetail.Length > 0 Then
                            repeatDownload = 5
                        End If
                        ComunicationLog("5062")
                        ListBoxLog.Items.Add("")
                        repeatDownload = repeatDownload + 1
                        If repeatDownload = 3 Then
                            ListBoxLog.Items.Add("Error download file: " & ListView1.CheckedItems(i).SubItems(1).Text & "_" & ListView1.CheckedItems(i).SubItems(2).Text() & "_" & ListView1.CheckedItems(i).SubItems(3).Text() & "." & ListView1.CheckedItems(i).SubItems(4).Text())
                        End If
                    End While
                End If
            Next
        Else
            ComunicationLog("5061") ' fill path
        End If
        ListBoxLog.Items.Add("Download Finish!")
    End Sub

    Private Sub ButtonSel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonSel.Click
        Dim i As Integer
        For i = 0 To ListView1.Items.Count - 1
            ListView1.Items(i).Checked = True
        Next
    End Sub

    Private Sub ButtonEcr_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim i As Integer, sql As String, question As String
        Dim cmd As New MySqlCommand()
        For i = 0 To ListView1.Items.Count - 1
            If ListView1.Items(i).Checked And ListView1.Items(i).SubItems(9).Text <> "" Then
                If controlRight(Mid(ListView1.Items(i).SubItems(1).Text, 3, 1)) >= 2 Then
                    question = InputBox("Please write the same [*] number for invalidate the ECR Alarm" & vbCrLf & "EcrPending: " & ListView1.Items(i).SubItems(9).Text)
                    If Mid(question, 1) = "[" And Mid(question, Len(question), 1) = "]" And IsNumeric(Mid(question, 2, Len(question) - 2)) Then
                        Try
                            sql = "UPDATE `" & DBName & "`.`doc` SET `ecrnull` = '" & ListView1.Items(i).SubItems(9).Text & question & "' WHERE `doc`.`id` = '" & ListView1.Items(i).SubItems(0).Text & "' ;"
                            cmd = New MySqlCommand(sql, MySqlconnection)
                            cmd.ExecuteNonQuery()
                            ComunicationLog("5067") 'ecr nulled
                            ButtonQuery_Click(Me, e)
                        Catch ex As Exception
                            ComunicationLog("0052") 'db operation error
                        End Try
                    Else
                        ComunicationLog("0068") ' parser text ko
                    End If
                Else
                    ComunicationLog("0043") ' no right
                End If
            End If
        Next
    End Sub

    Private Sub ButtonSign_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonSign.Click
        ListBoxLog.Items.Clear()
        Dim i As Integer, sql As String
        Dim cmd As New MySqlCommand()
        If ComboBoxSign.Text = "UNSIGNED" Then
            For i = 0 To ListView1.CheckedItems.Count - 1
                If (ListView1.CheckedItems(i).SubItems(1).Text <> ParameterTable("plant") & "R_PRO_ECR" Or (ListView1.CheckedItems(i).SubItems(1).Text = ParameterTable("plant") & "R_PRO_ECR" And InStr(1, ListView1.CheckedItems(i).SubItems(2).Text, "Template", CompareMethod.Text) > 0)) _
                And (ListView1.CheckedItems(i).SubItems(1).Text <> ParameterTable("plant") & "R_PRO_TCR" Or (ListView1.CheckedItems(i).SubItems(1).Text = ParameterTable("plant") & "R_PRO_TCR" And InStr(1, ListView1.CheckedItems(i).SubItems(2).Text, "Template", CompareMethod.Text) > 0)) Then
                    If ListView1.CheckedItems(i).Checked Then
                        If controlRight(Mid(ListView1.CheckedItems(i).SubItems(1).Text, 3, 1)) >= 3 Then
                            Try
                                sql = "UPDATE `" & DBName & "`.`doc` SET `sign` = '" & CreAccount.strUserName & "[" & Date.Today.Day & "/" & Date.Today.Month & "/" & Date.Today.Year & "]" & _
                                "' WHERE `doc`.`id` = " & ListView1.CheckedItems(i).SubItems(0).Text & " ;"
                                cmd = New MySqlCommand(sql, MySqlconnection)
                                cmd.ExecuteNonQuery()
                                ComunicationLog("5056") 'doc signed

                            Catch ex As Exception
                                ComunicationLog("0052") 'db operation error
                            End Try
                        Else
                            ComunicationLog("0043") ' no right
                        End If
                    End If
                Else
                    ComunicationLog("5066") ' Special Procedure For sign this document
                End If
            Next

        Else
            ComunicationLog("5053") ' plase go in unsigned section
        End If

    End Sub

    Private Sub Timer1_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Application.DoEvents()
    End Sub

    Private Sub CheckGru_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        'ButtonQuery.Enabled = False
        'If loadDoc = False And CheckGru.Checked = True Then
        '    If loadDoc = False Then loadDoc = True
        '    ComunicationLog("5077")
        '    Application.DoEvents()
        '    Try
        '        OpenConnectionMySqlGru("10.10.10.15", "Gestdoc", "chinadoc", "china")
        '        Dim AdapterDocGruProdType As New MySqlDataAdapter("SELECT * FROM tipodoc", MySqlconnectionGru)
        '        AdapterDocGruProdType.Fill(DsDocGrutype, "tipodoc")
        '        tlbDocGrutype = DsDocGrutype.Tables("tipodoc")

        '        ComunicationLog("5080")
        '    Catch ex As Exception
        '        MsgBox("Database Account error, server Grugliasco open procedure")
        '        Me.Close()
        '    End Try
        'End If
        'ButtonQuery.Enabled = True
    End Sub
    Function RevisionExtract(ByRef rev As Integer, ByVal header As String, ByVal Filename As String, ByVal Extension As String) As String

        Dim returnValue As DataRow()
        Try
            RevisionExtract = ("5029") ' revision extract ok
            If controlType(header, "C") = 2 And Regex.Match(Filename, "^\w+ - ").ToString <> "" Then
                returnValue = tblDoc.Select("header='" & header & "' and FileName like '" & Regex.Match(Filename, "^\w+").ToString & " - *'", "rev DESC") 'and Extension='" & Extension & "' ", "rev DESC")
            Else
                returnValue = tblDoc.Select("header='" & header & "' and FileName='" & Filename & "'", "rev DESC") 'and Extension='" & Extension & "' ", "rev DESC")
            End If

            If returnValue.Length >= 1 Then
                rev = returnValue(0).Item("rev")
            ElseIf returnValue.Length = 0 Then ' no file in DB
                rev = -1 ' file not find
            End If
        Catch ex As Exception
            RevisionExtract = ("0013") ' "Error in revision extract
        End Try

    End Function
    Function fControl(ByVal bitronPn As String, ByRef pcbCode As String, ByRef piastracode As String) As String

        Dim rsResult As DataRow()
        rsResult = tblDocProd.Select("bitronpn='" & bitronPn & "'")
        If rsResult.Length = 1 Then
            fControl = rsResult(0).Item("docFlag").ToString
            pcbCode = rsResult(0).Item("pcbcode").ToString
            piastracode = rsResult(0).Item("piastracode").ToString
            'fControl = rsResult(0).Item("docflag").ToString

        ElseIf rsResult.Length > 1 Then
            ComunicationLog("0054") ' product duplicate
            fControl = ""
        Else
            fControl = ""
        End If
    End Function
    Function Presence(ByVal strFlag As String, ByVal strControl As String) As String
        Presence = ""
        If InStr(1, strControl, strFlag) > 0 Then Presence = Mid(strControl, InStr(1, strControl, strFlag) + 1, 1)
        If strFlag = "1" Then Presence = "1"
        If strFlag = "0" Then Presence = "0"
    End Function
    Function TranslateIntranetName(ByVal h As String, ByVal f As String, ByVal r As Integer, ByVal e As String) As String
        TranslateIntranetName = ""
        If h = ParameterTable("plant") & "R_PRO_GPN" Then TranslateIntranetName = f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_GFX" Then TranslateIntranetName = "fix_" & f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_NFB" Then TranslateIntranetName = f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_NFP" Then TranslateIntranetName = f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_PCB" Then TranslateIntranetName = "doc_" & f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_PST" Then TranslateIntranetName = f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_SPG" Then TranslateIntranetName = "ps_" & f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        If h = ParameterTable("plant") & "R_PRO_TDS" Then TranslateIntranetName = f & "_" & IIf(r > 10, r, "0" & r) & "." & e
        TranslateIntranetName = "http://10.10.10.15/grugliasco/gestdoc/attach/" & TranslateIntranetName
    End Function

    Function FileNameDes(ByVal filename As String, Optional ByVal header As String = "") As String

        Dim rsResult As DataRow(), groupResult As DataRow(), pos As Integer

        Dim AdapterGroupProd As New MySqlDataAdapter("SELECT * FROM Product where groupList like '%" & header & "[" & filename & "]" & "%'", MySqlconnection)
        Dim tblGroupProd As DataTable
        Dim DsGroupProd As New DataSet

        AdapterGroupProd.Fill(DsGroupProd, "Product")
        tblGroupProd = DsGroupProd.Tables("Product")

        FileNameDes = ""

        rsResult = tblDocProd.Select("bitronpn='" & filename & "'")
        If rsResult.Length > 0 Then
            FileNameDes = rsResult(0).Item("name").ToString

        Else
            rsResult = tblDocProd.Select("piastracode LIKE '*" & filename & "*'")
            If rsResult.Length > 0 Then
                FileNameDes = "Piastra of " & rsResult(0).Item("name").ToString

            Else
                rsResult = tblDocProd.Select("pcbcode LIKE '*" & filename & "*'")
                If rsResult.Length > 0 Then
                    FileNameDes = "Pcb of " & rsResult(0).Item("name").ToString

                Else
                    groupResult = tblGroupProd.Select("bitronpn LIKE '*'")
                    If groupResult.Length > 0 Then
                        For Each row In groupResult
                            FileNameDes = FileNameDes & row("name").ToString & ";"
                        Next

                    Else
                        rsResult = tblDocProd.Select("bitronpn LIKE '*'")
                        For Each row In rsResult
                            pos = InStr(filename, row("bitronpn").ToString, CompareMethod.Text)
                            '.Select("header='" & Mid(FILENAME, 11, 11) & "' and FileName='" & Mid(FILENAME, 23, InStrRev(FILENAME, "_") - 23) & "' and Extension='" & Mid(FILENAME, InStrRev(FILENAME, ".") + 1) & "' "
                            If pos > 0 Then
                                FileNameDes = row("name").ToString
                            End If
                        Next

                    End If
                End If
            End If
        End If
    End Function
    Sub FillListView(ByVal rowShow As DataRow(), Optional ByVal Notclear As Boolean = False, Optional ByVal prod As String = "")
        ListBoxLog.BackColor = Color.LightGreen

        If Not stopEvent Then

            If Notclear Then

            Else
                ListView1.Clear()
            End If
            Dim c As DataColumn, i As Integer

            Dim Widht(tblDoc.Columns.Count - 1) As Integer, j As Integer

            If CheckBox1.Checked Then Widht(0) = 70 Else Widht(0) = 0
            If CheckBox2.Checked Then Widht(1) = 140 Else Widht(1) = 0
            If CheckBox3.Checked Then Widht(2) = 300 Else Widht(2) = 0
            If CheckBox4.Checked Then Widht(3) = 70 Else Widht(3) = 0
            If CheckBox5.Checked Then Widht(4) = 70 Else Widht(4) = 0
            If CheckBox6.Checked Then Widht(5) = 100 Else Widht(5) = 0
            If CheckBox7.Checked Then Widht(6) = 100 Else Widht(6) = 0
            If CheckBox8.Checked Then Widht(7) = 100 Else Widht(7) = 0
            If CheckBox9.Checked Then Widht(8) = 100 Else Widht(8) = 0
            If CheckBox10.Checked Then Widht(9) = 70 Else Widht(9) = 0
            If CheckBox11.Checked Then Widht(10) = 70 Else Widht(10) = 0

            If ListView1.Columns.Count < 12 Then
                i = 0
                For Each c In tblDoc.Columns
                    'adding names of columns as Listview columns				
                    Dim h As New ColumnHeader
                    If c.ColumnName = "notification" Then
                        h.Text = "Description"
                    Else
                        h.Text = c.ColumnName
                    End If

                    h.Width = Widht(i)
                    ListView1.Columns.Add(h)
                    i = i + 1
                Next
                'ListView1.Columns.Add("Description")
                'ListView1.Columns.Item(11).Name = "Description"

                If CheckBox12.Checked Then ListView1.Columns.Item(11).Width = 400 Else ListView1.Columns.Item(11).Width = 0

            End If

            Dim str(tblDoc.Columns.Count) As String, rev As Integer
            'adding Datarows as listview Grids

            For i = 0 To rowShow.Length - 1
                For col As Integer = 0 To tblDoc.Columns.Count - 1
                    str(col) = rowShow(i).ItemArray(col).ToString()
                Next
                If prod <> "" Then
                    str(11) = FileNameDes(prod)
                Else
                    str(11) = FileNameDes(str(2), str(1))
                End If


                'Dim Widht(tblDoc.Columns.Count - 1) As Integer, j As Integer

                'If CheckBox1.Checked Then Widht(0) = 70 Else Widht(0) = 0
                'If CheckBox2.Checked Then Widht(1) = 140 Else Widht(1) = 0
                'If CheckBox3.Checked Then Widht(2) = 300 Else Widht(2) = 0
                'If CheckBox4.Checked Then Widht(3) = 70 Else Widht(3) = 0
                'If CheckBox5.Checked Then Widht(4) = 70 Else Widht(4) = 0
                'If CheckBox6.Checked Then Widht(5) = 100 Else Widht(5) = 0
                'If CheckBox7.Checked Then Widht(6) = 100 Else Widht(6) = 0
                'If CheckBox8.Checked Then Widht(7) = 100 Else Widht(7) = 0
                'If CheckBox9.Checked Then Widht(8) = 100 Else Widht(8) = 0
                'If CheckBox10.Checked Then Widht(9) = 70 Else Widht(9) = 0
                'If CheckBox11.Checked Then Widht(10) = 70 Else Widht(10) = 0

                'If ListView1.Columns.Count < 12 Then
                '    i = 0
                '    j = 0
                '    For Each c In tblDoc.Columns
                '        If Widht(i) <> 0 Then
                '            'adding names of columns as Listview columns				
                '            Dim h As New ColumnHeader
                '            'If c.ColumnName = "notification" Then
                '            '    h.Text = "Description"
                '            'Else
                '            h.Text = c.ColumnName
                '            'End If

                '            h.Width = Widht(i)
                '            ListView1.Columns.Add(h)
                '            j = j + 1

                '        End If
                '        i = i + 1
                '    Next
                '    'ListView1.Columns.Add("Description")
                '    'ListView1.Columns.Item(11).Name = "Description"

                '    If CheckBox12.Checked Then
                '        ListView1.Columns.Add("Description")
                '        ListView1.Columns.Item(j).Width = 400
                '    End If

                'End If

                'Dim str(tblDoc.Columns.Count) As String, rev As Integer
                ''adding Datarows as listview Grids

                'For i = 0 To rowShow.Length - 1
                '    j = 0
                '    For col As Integer = 0 To tblDoc.Columns.Count - 1
                '        If Widht(col) <> 0 Then
                '            str(j) = rowShow(i).ItemArray(col).ToString()
                '            j = j + 1
                '        End If
                '    Next
                '    If prod <> "" Then
                '        str(j) = FileNameDes(prod)
                '    Else
                '        str(j) = FileNameDes(str(2))
                '    End If

                Dim ii As New ListViewItem(str)

                RevisionExtract(rev, rowShow(i).ItemArray(1).ToString(), rowShow(i).ItemArray(2).ToString(), rowShow(i).ItemArray(4).ToString())
                Application.DoEvents()
                If stopEvent Then Exit Sub
                If ComboBoxRevision.Text = "ALL" Or rev = Int(Val(rowShow(i).ItemArray(3).ToString())) Then
                    If ComboBoxSign.Text = "SIGNED" And Trim(rowShow(i).ItemArray(6).ToString()) <> "" Then
                        If controlRight(Mid(rowShow(i).ItemArray(1).ToString(), 3, 1)) >= 1 Then
                            ListView1.Items.Add(ii)
                        End If
                    ElseIf ComboBoxSign.Text = "UNSIGNED" And rowShow(i).ItemArray(6).ToString() = "" Then
                        If controlRight(Mid(rowShow(i).ItemArray(1).ToString(), 3, 1)) >= 2 Then
                            ListView1.Items.Add(ii)
                        End If
                    End If
                End If

            Next

        Else
            ComunicationLog("5072")
        End If
    End Sub
    Sub ComunicationLog(ByVal ComCode As String)

        Dim rsResult As DataRow()
        rsResult = tblError.Select("code='" & ComCode & "'")
        If rsResult.Length = 0 Then
            ComCode = "0051"
            rsResult = tblError.Select("code='" & ComCode & "'")
        End If
        ListBoxLog.SelectedIndex = ListBoxLog.Items.Count - 1
        ListBoxLog.Items.Add(ComCode & " -> " & rsResult(0).Item("en").ToString)
        WriteFile(ComCode & " -> " & rsResult(0).Item("en").ToString & vbCrLf, True)

        If Val(ComCode) >= 5000 Then

            ListBoxLog.BackColor = Color.LightGreen

        ElseIf Val(ComCode) < 5000 Then

            ListBoxLog.BackColor = Color.OrangeRed

        End If
    End Sub

    Sub FillComboRevision()
        ComboBoxRevision.Items.Add("LAST")
        ComboBoxRevision.Items.Add("ALL")
        ComboBoxRevision.Sorted = True
        ComboBoxRevision.Text = ("LAST")
    End Sub
    Sub FillComboProd()
        ComboBoxProd.Items.Clear()
        ComboBoxProd.Items.Add("ALL - PRODUCT")
        Dim returnValue As DataRow()
        returnValue = tblDocProd.Select(IIf(CheckBoxObsolete.Checked = True, "( status like '*') and ", "( NOT status like 'OBSOLETE') and ") & IIf(ComboBoxStatus.Text = "ALL - STATUS", "( status like '*')", "( status like '" & ComboBoxStatus.Text & "')") & " and customer like '" & IIf(ComboBoxCustomer.Text = "ALL - CUSTOMER", "*", ComboBoxCustomer.Text) & "'", "bitronpn DESC")
        For Each row In returnValue
            ComboBoxProd.Items.Add(row("bitronpn").ToString & " - " & row("name").ToString)
        Next
    End Sub
    Sub FillComboCust()
        ComboBoxCustomer.Items.Clear()
        ComboBoxCustomer.Items.Add("ALL - CUSTOMER")
        Dim returnValue As DataRow()
        returnValue = tblDocCust.Select("name like '*'", "name ASC")
        For Each row In returnValue
            ComboBoxCustomer.Items.Add(row("name").ToString)
        Next
    End Sub
    Sub FillComboFirstType()
        ComboBoxFirstType.Items.Clear()
        Dim strOld As String = ""
        Dim strNew As String
        Dim returnValue As DataRow()
        returnValue = tblDocType.Select("FirstType LIKE '*' ", "FirstType DESC")
        For Each row In returnValue
            strNew = (row("FirstType").ToString)
            If StrComp(strOld, strNew) <> 0 Then
                strOld = strNew
                ComboBoxFirstType.Items.Add(strNew)
            End If
        Next
        ComboBoxFirstType.Items.Add("")
        ComboBoxFirstType.Sorted = True
        ComboBoxSecondType.Items.Clear()
        ComboBoxThirdType.Items.Clear()
        ComboBoxProd.Text = ""
        ComboBoxCustomer.Text = ""
        TextBoxfileName.Text = ""
        TextBoxCompPn.Text = ""
    End Sub
    Sub FillComboEcrNull()
        ComboBoxEcrNull.Items.Clear()
        ComboBoxEcrPending.Items.Clear()
        Dim strOld As String = ""
        Dim strNew As String
        Dim returnValue As DataRow()
        returnValue = tblDoc.Select(" ecrnull LIKE '*ecr*'", "ecrnull DESC")
        For Each row In returnValue
            strNew = (row("ecrnull").ToString)
            If StrComp(strOld, strNew) <> 0 Then
                strOld = strNew
                ComboBoxEcrNull.Items.Add(strNew)
            End If
        Next
        ComboBoxEcrNull.Sorted = True
    End Sub
    Sub FillComboEcrPending()
        ComboBoxEcrPending.Items.Clear()
        Dim strOld As String = ""
        Dim strNew As String
        Dim returnValue As DataRow()
        returnValue = tblDoc.Select(" ecrPending LIKE '*ecr*'", "ecrPending DESC")
        For Each row In returnValue
            strNew = (row("ecrPending").ToString)
            If StrComp(strOld, strNew) <> 0 Then
                strOld = strNew
                ComboBoxEcrPending.Items.Add(strNew)
            End If
        Next
        ComboBoxEcrPending.Sorted = True
    End Sub
    Sub FillComboSign()
        ComboBoxSign.Items.Add("SIGNED")
        If InStr(1, CreAccount.strSign, "2", CompareMethod.Text) > 0 Or InStr(1, CreAccount.strSign, "3", CompareMethod.Text) > 0 Then ComboBoxSign.Items.Add("UNSIGNED")
        ComboBoxSign.Sorted = True
        ComboBoxSign.Text = "SIGNED"
    End Sub
    Function tipodoc(ByVal idtipo As Integer) As String
        Dim rsResult As DataRow()
        rsResult = tlbDocGrutype.Select("coddoc='" & idtipo & "'")
        If rsResult.Length = 1 Then
            tipodoc = rsResult(0).Item("tipodoc").ToString
        End If
    End Function

    Private Sub ListView1_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListView1.MouseDoubleClick

        If ListView1.SelectedItems.Count = 1 Then
            If IsNumeric(ListView1.SelectedItems.Item(0).SubItems(0).Text) Then
                Dim fileOpen As String
                fileOpen = downloadFileWinPath(ListView1.SelectedItems.Item(0).SubItems(1).Text & "_" & _
                ListView1.SelectedItems.Item(0).SubItems(2).Text & "_" & _
                ListView1.SelectedItems.Item(0).SubItems(3).Text & "." & _
                ListView1.SelectedItems.Item(0).SubItems(4).Text)

                Application.DoEvents()
                Process.Start(fileOpen)
                Application.DoEvents()
            ElseIf Mid(ListView1.SelectedItems.Item(0).SubItems(0).Text, 1, 7) = "SRV_DOC" Then
                Dim fileOpen As String
                fileOpen = downloadFileWinPath(Mid(ListView1.SelectedItems.Item(0).SubItems(0).Text, 11))
                Application.DoEvents()
                Process.Start(fileOpen)
                Application.DoEvents()
            ElseIf ListView1.SelectedItems.Item(0).SubItems(0).Text = "GRU" Then

                Try
                    Application.DoEvents()
                    ListBoxLog.Items.Add("Download Web....")
                    Application.DoEvents()
                    My.Computer.Network.DownloadFile("http://10.10.10.15/grugliasco/gestdoc/attach/" & ListView1.SelectedItems(0).SubItems(2).Text, System.IO.Path.GetTempPath & ListView1.SelectedItems(0).SubItems(2).Text, "", "", False, 10000, True)
                    ListBoxLog.Items.Add("Download Web....Finish!")
                    Process.Start(System.IO.Path.GetTempPath & ListView1.SelectedItems(0).SubItems(2).Text)
                    Application.DoEvents()
                Catch ex As Exception
                    MsgBox("Document not present in Bitron Intranet. Error in Intranet DB")
                End Try
            ElseIf ListView1.SelectedItems.Item(0).SubItems(0).Text = "HC" Then
                Process.Start("IExplore.exe", "http://10.10.10.15/orcad/gest.php?cod_comp=" & Mid(ListView1.SelectedItems.Item(0).SubItems(11).Text, 20))
                Application.DoEvents()
                ' open DS
                'If vbYes = MsgBox("Do you want open all DS? ") Then
                Dim dsstr As String
                For i = 0 To 9
                    If i <> 1 Then
                        dsstr = ds(Mid(ListView1.SelectedItems.Item(0).SubItems(11).Text, 20), i)
                        Try
                            If dsstr <> "" Then Process.Start("IExplore.exe", dsstr)
                        Catch ex As Exception
                            MsgBox("Problem to open")
                        End Try
                        Application.DoEvents()
                    End If

                Next

                'End If

            End If
        End If

        ListView1.SelectedItems.Item(0).Checked = Not ListView1.SelectedItems.Item(0).Checked
    End Sub

    Function ds(ByVal comp As String, ByVal i As Integer) As String
        Dim RowHC As DataRow()

        If tblDocComp Is Nothing Or tblDocComp.Rows.Count = 0 Then
            Try
                CloseConnectionSqlOrcad()
                'OpenConnectionMySqlOrcad("10.10.10.36", "orcad1", "orcadw", "orcadw")
                OpenConnectionMySqlOrcad(OrcadDBAdr, OrcadDBName, OrcadDBUser, OrcadDBPwd)

                Dim AdapterDocComp As New SqlDataAdapter("SELECT * FROM orcadw.T_orcadcis where not valido = 'no_valido'", SQLconnectionOrcad)
                AdapterDocComp.Fill(DsDocComp, "orcadw.T_orcadcis")
                tblDocComp = DsDocComp.Tables("orcadw.T_orcadcis")
            Catch ex As Exception
                MsgBox("Error in opening Orcad Database! Now - " & ex.Message)
            End Try
        End If

        RowHC = tblDocComp.Select("cod_comp = " & comp & " and valido = 'valido'", "valido")
        If RowHC.Length = 1 Then
            ds = Replace(Replace(RowHC(0)("datasheet" & IIf(i > 0, i, "")).ToString, "http://webserver.industrie.bitron.net/", "10.10.10.15\"), "\", "/")
        End If

    End Function

    Function downloadFileWinPath(ByVal fileName As String) As String
        Dim strPathFtp As String
        Dim objFtp As ftp = New ftp()
        objFtp.UserName = strFtpServerUser
        objFtp.Password = strFtpServerPsw
        objFtp.Host = strFtpServerAdd
        downloadFileWinPath = ""


        Dim cmd As New MySqlCommand()
        If fileName <> "" Then
            Try
                strPathFtp = Mid(fileName, 1, 3) & "/" & Mid(fileName, 1, 11) & "/"

                ComunicationLog(objFtp.DownloadFile(strPathFtp, System.IO.Path.GetTempPath, fileName)) ' download successfull
                downloadFileWinPath = System.IO.Path.GetTempPath & fileName
            Catch ex As Exception
                ComunicationLog("0049") ' Error in ecr Download
            End Try
        Else
            ComunicationLog("5061") ' fill path
        End If

    End Function

    Function NameFile(ByVal id As Long) As String

        Dim RowSearchDoc As DataRow()
        AdapterDoc.SelectCommand = New MySqlCommand("SELECT * FROM DOC;", MySqlconnection)
        DsDoc.Clear()
        tblDoc.Clear()
        AdapterDoc.Fill(DsDoc, "doc")
        tblDoc = DsDoc.Tables("doc")
        RowSearchDoc = tblDoc.Select("id = " & id)
        NameFile = ""
        For Each ROW In RowSearchDoc
            NameFile = ROW("header").ToString & "_" & ROW("filename").ToString & "_" & ROW("rev").ToString & "." & ROW("extension").ToString
        Next
    End Function

    Private Sub ListView1_ColumnClick1(ByVal sender As Object, ByVal e As System.Windows.Forms.ColumnClickEventArgs) Handles ListView1.ColumnClick

        Me.ListView1.ListViewItemSorter = New ListViewItemComparer(e.Column)
        ' Call the sort method to manually sort.
        ListView1.Sort()


    End Sub

    Sub SalvaFile(ByVal NomeFile As String)

        Using sw As New System.IO.StreamWriter(NomeFile, False, System.Text.Encoding.GetEncoding(1252))
            Dim sb As New System.Text.StringBuilder
            For i As Integer = 0 To ListView1.Columns.Count - 1
                sb.Append(ListView1.Columns(i).Text)
                If i <> ListView1.Columns.Count - 1 Then
                    sb.Append(ControlChars.Tab)
                Else
                    sw.WriteLine(sb.ToString)
                End If
            Next
            For Each it As ListViewItem In ListView1.Items
                sb = New System.Text.StringBuilder
                sb.Append(it.Text)
                If ListView1.Columns.Count > 1 Then
                    For i As Integer = 1 To ListView1.Columns.Count - 1
                        sb.Append(ControlChars.Tab)
                        sb.Append(it.SubItems(i).Text)
                    Next
                End If
                sw.WriteLine(sb.ToString)
            Next
        End Using
    End Sub

    Private Sub ButtonSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            SalvaFile(SaveFileDialog1.FileName)
        End If
    End Sub

    Private Sub ButtonExport_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonExport.Click
        ExportListview2Excel(ListView1)
    End Sub

    ' find the last revision in the Server of the current file
    Function RevisionLast(ByVal FILENAME As String) As String
        RevisionLast = FILENAME
        Dim returnValue As DataRow()
        Try
            returnValue = tblDoc.Select("header='" & Mid(FILENAME, 11, 11) & "' and FileName='" & Mid(FILENAME, 23, InStrRev(FILENAME, "_") - 23) & "' and Extension='" & Mid(FILENAME, InStrRev(FILENAME, ".") + 1) & "' ", "rev DESC")
            If returnValue.Length >= 1 Then
                RevisionLast = "SRV_DOC - " & returnValue(0).Item("header") & "_" & returnValue(0).Item("filename") & "_" & returnValue(0).Item("rev") & "." & returnValue(0).Item("extension")
            Else

            End If
        Catch ex As Exception
            MsgBox("Revision Extract Error")
        End Try

    End Function

    ' check the control type of file
    ' if type not find give -1
    Function controlType(ByVal header As String, ByVal controlString As String) As Integer
        Dim intpos As Integer
        controlType = -1 ' type enot find
        Dim row As DataRow()
        row = tblDocType.Select("header = '" & header & "'")
        If row.Length > 0 Then
            intpos = InStr(1, row(0).Item("control").ToString, controlString, CompareMethod.Text)
            If intpos > 0 Then
                controlType = Val(Mid(row(0).Item("Control").ToString, intpos + 1, 1))
            Else
                controlType = 0
            End If
        End If
    End Function

    Private Sub GroupBox1_Enter(sender As Object, e As EventArgs)

    End Sub

    Private Sub TimerCompLoading_Tick(sender As Object, e As EventArgs) Handles TimerCompLoading.Tick
        Application.DoEvents()
        If trdFinish Then
            ButtonOrcad.BackColor = Color.Green

        Else
            ButtonOrcad.BackColor = Color.Red
        End If
    End Sub

    Private Sub RadioButtonProductSearch_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButtonProductSearch.CheckedChanged
        If RadioButtonGeneralSearch.Checked And Not RadioButtonProductSearch.Checked Then
        Else
            RadioButtonGeneralSearch.Checked = Not RadioButtonProductSearch.Checked
        End If

        If RadioButtonProductSearch.Checked = True Then
            ComboBoxFirstType.Enabled = True
            ComboBoxSecondType.Enabled = True
            ComboBoxThirdType.Enabled = True
            TextBoxfileName.Enabled = False
            ComboBoxCustomer.Enabled = True
            ComboBoxStatus.Enabled = True
            ComboBoxProd.Enabled = True
            CheckGru.Enabled = True
            CheckBoxObsolete.Enabled = True
            CheckComp.Enabled = True
            TextBoxCompPn.Enabled = True
            TextBoxCompPn.Text = ""
        ElseIf RadioButtonGeneralSearch.Checked = True Then
            ComboBoxFirstType.Enabled = True
            ComboBoxSecondType.Enabled = True
            ComboBoxThirdType.Enabled = True
            TextBoxfileName.Enabled = True
            ComboBoxCustomer.Enabled = False
            ComboBoxCustomer.Text = ""
            ComboBoxStatus.Enabled = False
            ComboBoxProd.Enabled = False
            ComboBoxProd.Text = ""
            CheckGru.Enabled = False
            CheckBoxObsolete.Enabled = False
            CheckComp.Enabled = False
            TextBoxCompPn.Enabled = False
            TextBoxCompPn.Text = ""
        End If

    End Sub

    Private Sub ComboBoxCustomer_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxCustomer.SelectedIndexChanged
        ComboBoxProd.Text = ""
        FillComboProd()
    End Sub

    Private Sub ComboBoxStatus_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxStatus.SelectedIndexChanged
        ComboBoxProd.Text = ""
        FillComboProd()
    End Sub

    Private Sub CheckBoxObsolete_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxObsolete.CheckedChanged
        ComboBoxCustomer.Text = ""
    End Sub

    Private Sub ButtonStop_Click_1(sender As Object, e As EventArgs) Handles ButtonStop.Click

        stopEvent = True

    End Sub
    Private Sub ComboBoxFirstType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxFirstType.SelectedIndexChanged

    End Sub

    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView1.SelectedIndexChanged

    End Sub

    Private Sub CheckBox9_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox9.CheckedChanged

    End Sub

    Private Sub ComboBoxThirdType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxThirdType.SelectedIndexChanged

    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged

    End Sub

    Private Sub OpenConnectionSqlOrcad(OrcadDBAds As String, OrcadDBName As String, OrcadDBUserName As String, OrcadDBPwd As String)
        Throw New NotImplementedException
    End Sub

    Private Sub CheckGru_CheckedChanged_1(sender As Object, e As EventArgs) Handles CheckGru.CheckedChanged

    End Sub

    Private Sub TextBoxCompPn_TextChanged(sender As Object, e As EventArgs) Handles TextBoxCompPn.TextChanged

    End Sub

    Private Sub CheckComp_CheckedChanged(sender As Object, e As EventArgs) Handles CheckComp.CheckedChanged

    End Sub

    Private Sub ComboBoxSign_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxSign.SelectedIndexChanged

    End Sub

    Private Sub GroupBox1_Enter_1(sender As Object, e As EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub ComboBoxProd_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxProd.SelectedIndexChanged

    End Sub

    Private Sub ListBoxLog_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxLog.SelectedIndexChanged

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged

    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged

    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged

    End Sub

    Private Sub CheckBox12_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox12.CheckedChanged

    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged

    End Sub

    Private Sub CheckBox6_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox6.CheckedChanged

    End Sub

    Private Sub CheckBox8_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox8.CheckedChanged

    End Sub

    Private Sub CheckBox7_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox7.CheckedChanged

    End Sub

    Private Sub CheckBox10_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox10.CheckedChanged

    End Sub

    Private Sub CheckBox11_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox11.CheckedChanged

    End Sub
End Class