﻿Partial Public Class Engine
	Private ReadOnly Functions As New Dictionary(Of String, String)
	Private Lexer As New Lexer
	Private InFunc As Boolean

	Private Register As Object
	Private Counter%
	Private LoopEnd%
	Private FuncArgs As New List(Of Object)
	Private Variable As New Dictionary(Of String, Object)
	Private ProjectionOutput As New List(Of Object)
	Private ReadOnly Stack As New Stack(Of Object)
	Private ReadOnly Types As New List(Of Type)

#Disable Warning IDE1006 ' Naming Styles
	Private Function Defined(ParamArray Data() As Object) As Object
		If Data.Length = 1 AndAlso TypeOf Data(0) IsNot String AndAlso TypeOf Data(0) Is IEnumerable(Of Object) Then Data = New List(Of Object)(CType(Data(0), IEnumerable(Of Object))).ToArray()
		If Data.Length = 1 AndAlso TypeOf Data(0) Is String Then
			Return Variable.ContainsKey(Data(0).ToLower())
		Else
			Dim retval As New List(Of Object)
			For Each item In Data
				retval.Add(Defined(item))
			Next
			Return retval
		End If
	End Function

	Private Function _Concat() As Object
		If TypeOf Stack.Peek() Is String Then
			Return If(Stack.Pop(), String.Empty).ToString() & If(Register, String.Empty)
		Else
			If Stack.Peek() Is Nothing Then
				Stack.Pop()
				If Register Is Nothing Then Return Nothing
				Return {Register}.ToList()
			Else
				If Register Is Nothing Then Return New List(Of Object)(CType(If(TypeOf Stack.Peek() Is IEnumerable(Of Object) AndAlso TypeOf Stack.Peek() IsNot String, Stack.Pop(), {Stack.Pop()}), IEnumerable(Of Object))).ToList()
				Return New List(Of Object)(CType(If(TypeOf Stack.Peek() Is IEnumerable(Of Object) AndAlso TypeOf Stack.Peek() IsNot String, Stack.Pop(), {Stack.Pop()}), IEnumerable(Of Object))).Concat({Register}).ToList()
			End If
		End If
	End Function

	Private Function _Equality(Item1 As Object, Item2 As Object) As Boolean
		Dim a As Object() = If(TypeOf Item1 Is IEnumerable(Of Object), If(TypeOf Item1 Is Object(), Item1, Item1.ToArray()), {Item1})
		Dim b As Object() = If(TypeOf Item2 Is IEnumerable(Of Object), If(TypeOf Item2 Is Object(), Item2, Item2.ToArray()), {Item2})
		If a.Length <> b.Length Then Return False
		If a.SequenceEqual(b) Then Return True
		For i% = 0 To a.Length - 1
			If (IsNumeric(a(i)) AndAlso IsNumeric(b(i))) OrElse (TypeOf a(i) Is String OrElse TypeOf a(i) Is Char) AndAlso (TypeOf b(i) Is String OrElse TypeOf b(i) Is Char) Then
				If CStr(a(i)) <> CStr(b(i)) Then Return False
			ElseIf TypeOf a(i) Is IEnumerable(Of Object) AndAlso TypeOf b(i) Is IEnumerable(Of Object) Then
				If Not _Equality(a(i), b(i)) Then Return False
			End If
		Next
		Return True
	End Function
#Enable Warning IDE1006 ' Naming Styles

	''' <summary>
	''' Instantiates a new interpretive engine.
	''' </summary>
	Public Sub New()
		AddLib("Runtime")
	End Sub

	''' <summary>
	''' Instatiates a new interpretive engine.
	''' </summary>
	''' <param name="[Imports]">An array of <see cref="Type"/> to import during engine setup.</param>
	Public Sub New([Imports]() As Type)
		Me.New()
		Types.AddRange([Imports])
	End Sub

	''' <summary>
	''' Loads, executes, and (optionally) returns the value of a W block or expression.
	''' </summary>
	''' <param name="Code">The code to be executed.</param>
	''' <param name="ForceReturn">If true, returns the most recent calculation from code blocks.</param>
	''' <exception cref="ReturnException">Returned outside a function.</exception>
	''' <exception cref="Exception">Interpretation error.</exception>
	''' <returns>The result of the most recent calculation or <c>null</c>.</returns>
	Public Function Eval(Code$, Optional ForceReturn As Boolean = False) As String
		Try
			Try
				Lexer.Reset(Code)
				BooleanExpr(recursionLimit:=10)
			Catch Ex As StackOverflowException
				Lexer.Reset(Code)
				Block()
				If Not ForceReturn Then Register = Nothing
			End Try

			Return FormatOutput({If(Register, String.Empty)})
		Catch ex As ReturnException
			Throw New Exception($"Don't use return outside of a function call! Return value: {ex.ReturnValue}")
		Catch ex As Exception
			Dim exMessage$() = ex.Message.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
			If TypeOf ex Is InvalidCastException AndAlso exMessage(3) = "not" Then
				Throw New Exception("Impossible operation on data")
			ElseIf TypeOf ex Is IndexOutOfRangeException Then
				Throw New Exception("Index out of range")
			ElseIf TypeOf ex Is InvalidCastException AndAlso exMessage(3) <> "not" Then
				Throw New Exception("Impossible operation on data " & exMessage(3))
			Else : Throw
			End If
		End Try
	End Function

	Public Function FormatOutput(Data() As Object) As String
		Dim Sterilise = Function(inp$)
							Return inp.Replace("\\", "ʍbɐcĸßℓɐßɥ").Replace("\r\n", vbCrLf).Replace("\r", vbCr).Replace("\n", Environment.NewLine).Replace("\b", vbBack).Replace("\t", vbTab).Replace("ʍbɐcĸßℓɐßɥ", "\")
						End Function

		If Data.Length = 1 AndAlso (TypeOf Data(0) Is String OrElse TypeOf Data(0) IsNot IEnumerable(Of Object)) _
			AndAlso TypeOf Data(0) IsNot Dynamic.ExpandoObject _
			AndAlso Data(0) IsNot Nothing Then Return Sterilise(Data(0))

		Dim retval As New List(Of String)
		For Each di In Data
			Dim item = If(If(TypeOf di Is Dynamic.ExpandoObject, DynamicFormat(di), di), "<UNINITIALIZED>")

			If (TypeOf item IsNot String) AndAlso TypeOf item Is IEnumerable Then
				retval.Add($"({FormatOutput(CType(item, IEnumerable(Of Object)).ToArray())})")
			Else
				retval.Add(Sterilise(item))
			End If
		Next
		If retval.Count = 0 Then Return String.Empty
		If retval.Count = 1 Then Return retval(0)
		Return Runtime.Thread({", "}.Concat(retval).ToArray())
	End Function

	''' <summary>
	''' Gets a complete code block from the console.
	''' </summary>
	Public Sub GetBlock()
		Dim depth% = 0
		Dim input$
		Do
			Console.Write("... ")
			input = Console.ReadLine().TrimStart("...")

			If input.Contains("[") Then depth += 1
			If input.Contains("]") Then depth -= 1

			Lexer.Feed(input)
		Loop Until depth = 0 AndAlso String.IsNullOrWhiteSpace(input)
	End Sub

	Private Function DynamicFormat(di As Dynamic.ExpandoObject) As String
		If Not di.Any() Then Return "Dynamic {}"

		Dim retval$ = String.Empty
		For Each prop As KeyValuePair(Of String, Object) In di
			retval &= $"{vbTab}{prop.Key}:{vbTab}{FormatOutput({prop.Value}).Split(Environment.NewLine).Select(Function(s$) vbTab & vbTab & s).Aggregate(Function(s, i) s & Environment.NewLine & i).Trim()}{Environment.NewLine}"
		Next
		Return ("Dynamic {" & Environment.NewLine & retval).TrimEnd() & Environment.NewLine & "}"
	End Function
End Class