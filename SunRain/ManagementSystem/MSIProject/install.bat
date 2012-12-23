netsh firewall delete allowedprogram #DIR#SystemService.exe >> #DIR#inun.log
#DIR#InstallUtil.exe -u #DIR#SystemService.exe >> #DIR#inun.log
#DIR#InstallUtil.exe #DIR#SystemService.exe >> #DIR#inun.log
netsh firewall add allowedprogram #DIR#SystemService.exe ENABLE >> #DIR#inun.log