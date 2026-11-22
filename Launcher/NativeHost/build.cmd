@echo off
setlocal
set "output=%~f1"
set "sdk=%~f2"
set "gate=%~f3"
for /f "usebackq tokens=*" %%V in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "vs=%%V"
if not defined vs exit /b 1
call "%vs%\VC\Auxiliary\Build\vcvars64.bat" >nul || exit /b 1
set "includes=%~4\Include\10.0.26100.0"
set "INCLUDE=%includes%\ucrt;%includes%\um;%includes%\shared;%INCLUDE%"
set "LIB=%~5\um\x64;%~5\ucrt\x64;%LIB%"
set "PATH=%~4\bin\10.0.26100.0\x64;%PATH%"
if not exist "%output%" mkdir "%output%" || exit /b 1
pushd "%output%" || exit /b 1
>host.rc echo 101 RCDATA "%gate:\=/%"
>>host.rc echo 1 ICON "%~dp0../MIR2.ICO"
rc /nologo host.rc || exit /b 1
cl /nologo /EHsc /std:c++17 /O2 /MT /DUNICODE /D_UNICODE /DNETHOST_USE_AS_STATIC /I"%sdk%" "%~dp0host.cpp" host.res "%sdk%\libnethost.lib" user32.lib ole32.lib advapi32.lib shell32.lib /link /subsystem:windows /out:Launcher.exe
exit /b %errorlevel%
