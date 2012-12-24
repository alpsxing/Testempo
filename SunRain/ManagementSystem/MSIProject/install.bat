netsh advfirewall firewall delete rule name="DTU System Service" program="#DIR#SystemService.exe" >> #DIR#inun.log
#DIR#InstallUtil.exe -u #DIR#SystemService.exe >> #DIR#inun.log
#DIR#InstallUtil.exe #DIR#SystemService.exe >> #DIR#inun.log
netsh advfirewall firewall add rule name="DTU System Service" dir=in action=allow program="#DIR#SystemService.exe" enable=yes >> #DIR#inun.log