@echo off

set DIR=%~dp0

set TARGET_FILE=RevitTimasBIMTools.dll

set TARGET_DIR=C:\ProgramData\Autodesk\Revit\Addins\2019\RevitTimasBIMTools

IF NOT EXIST "%TARGET_DIR%\" mkdir "%TARGET_DIR%"

IF EXIST "%TARGET_FILE%" (copy %TARGET_FILE% %TARGET_DIR%)

pause