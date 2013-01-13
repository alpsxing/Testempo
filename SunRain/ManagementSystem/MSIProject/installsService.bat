netsh advfirewall firewall delete rule name="DTU System Service" program="SystemService.exe" >> serviceinun.log
InstallUtil.exe -u SystemService.exe >> serviceinun.log
InstallUtil.exe SystemService.exe >> serviceinun.log
netsh advfirewall firewall add rule name="DTU System Service" dir=in action=allow program="SystemService.exe" enable=yes >> serviceinun.log
