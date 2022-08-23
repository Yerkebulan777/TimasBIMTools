
Namespace RevitTimasBIMTools.Temp
    Friend Class Converter
        Public Function get_fitrable_categories() As List(Of BuiltInCategory)
            Try
                Dim bi As Autodesk.Revit.DB.BuiltInCategory
                Dim bis As New List(Of BuiltInCategory)
                '  
                '  
                For Each elemID In Autodesk.Revit.DB.ParameterFilterUtilities.GetAllFilterableCategories
                    System.Windows.Forms.Application.DoEvents()
                    Try
                        bi = New Autodesk.Revit.DB.BuiltInCategory
                        bi = [Enum].Parse(bi.GetType, elemID.IntegerValue)
                        bis.Add(bi)
                    Catch ex As Exception
                    End Try
                Next

                '**********  
                Return bis
            Catch ex As Exception
                api.print_out_error(ex)
                Return New List(Of BuiltInCategory)
            End Try
        End Function
    End Class
End Namespace
