SunnyPi
=======

Solar monitor for the Raspberry Pi, made a link between Mastervolt Soladin and PVoutput.org (or CSV files)

NHL students made this solar monitor for raspberry pi,
and is made for Mastervolt Soladin
files will be logged into a CSV file, and uploaded to PVoutput.org


-1. Iso for Raspberry 8GB SD-card here: http://jonajona.nl/bin/Final SunnyPi.rar
-2. Deploy unpacked 'Final Sunnypi.rar' with 'RPI-images\win32diskimager.zip'
-3. Connecting details are to be found in the 'Hardware' directory
-4. (Change and) Upload and Use software inside 'Software\RPI -applicatie\PVoutput_sync - Update' folder  
  4a. Via FTP : ftp://sunnypi:21 --> user: pi - password: sunny
	4b. Via TightVNC by opening 'Software\debug\sunnypi-5901.vnc' with tightvncviewer
	4c. Manually via TightVNCvieuwer connecting to --> host:sunnypi - password:sunnypi
	4d. for the tough users via SSH (by putty for instance) --> host:sunnypi - username:pi - password:sunny	

for help, first look inside 'Software\debug\Rpi Commands.txt' folder, for commands (BT inside 'Software\debug\Rpi Commands.txt' file)
to make a completly new app for RPI install monodevelop and GTK# by instructions/links/installers inside 'debug\Mono Install' directory

Good luck,
if there is anything unclear or i could help you with:
info@jonajona.nl
