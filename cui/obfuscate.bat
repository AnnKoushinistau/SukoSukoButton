@echo off
rmdir .\Confused /s /q
xcopy .\bin\Debug .\Confused /D /S /R /Y /I /K
del .\Confused\*.exe .\Confused\*.dll .\Confused\*.pdb .\Confused\*.xml
copy .\bin\Debug\chromedriver.exe .\Confused\chromedriver.exe
copy .\bin\Debug\Newtonsoft.Json.dll .\Confused\Newtonsoft.Json.dll
.\obs\Confuser.CLI.exe -n .\obfuscate.crproj
move .\Confused\SukoSukoCuiMerged.exe .\Confused\SukoSukoCui.exe
pause
