SET getWorkDir=%~dp0

sc create Platform.Application binPath= "%getWorkDir%\Platform.Application.exe" DisplayName= "Platform.Application"
pause