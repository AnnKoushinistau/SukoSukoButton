@echo off
rmdir .\Confused /s /q
xcopy .\bin\Debug .\Confused /D /S /R /Y /I /K
del .\Confused\*.exe .\Confused\*.dll
copy .\bin\Debug\chromedriver.exe .\Confused\chromedriver.exe
.\obs\Confuser.CLI.exe -n .\obfuscate.crproj
pause
