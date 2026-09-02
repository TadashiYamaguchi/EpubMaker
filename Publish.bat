@echo off
chcp 65001 > nul
setlocal

set PROJECT=%~dp0EpubMaker.csproj

echo シングルファイル発行を実行します...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% NEQ 0 (
	echo 発行に失敗しました。
	pause
	exit /b %ERRORLEVEL%
)

echo.
echo 完了しました。出力先: %~dp0bin\Release\net8.0-windows\win-x64\publish\EpubMaker.exe
pause