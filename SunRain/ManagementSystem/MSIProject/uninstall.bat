netsh advfirewall firewall delete rule name="DTU System Service" program="#DIR#SystemService.exe" >> #DIR#inun.log
#DIR#InstallUtil.exe -u #DIR#SystemService.exe >> #DIR#inun.log
REM net stop WAS /y
REM net stop W3SVC /y