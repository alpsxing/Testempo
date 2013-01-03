netsh advfirewall firewall delete rule name="DTU System Service" program="#DIR#SystemService.exe" >> #DIR#inun.log
#DIR#InstallUtil.exe -u #DIR#SystemService.exe >> #DIR#inun.log
net stop WAS /y