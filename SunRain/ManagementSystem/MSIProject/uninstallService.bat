netsh advfirewall firewall delete rule name="DTU System Service" program="SystemService.exe" >> serviceinun.log
InstallUtil.exe -u SystemService.exe >> serviceinun.log