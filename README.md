# packmanMultiplayer

## Not implemented
- Causal order of messages
- Client process commands
- Server to ignore delayed commands

## Implemented
- whole funkcionality, intersections, application freez, delay and crash

## How to run
- Build solution as DEBUG with Visual Studio
- Set "Process creation service" and "Puppet Master" as startup project
- Set program arguments: 
	- Process creation service: url where to listen
	- Puppet Master: location of commands file
- Run project. (Process creation service hase location of server and client .exe files in their project location, Debug, Bin. Don't move them)

## Example of input file for PuppetMaster

StartServer myServer tcp://localhost:8085/PCSServicesName tcp://localhost:8086/ServerServicesName 200 2
Wait 1000
StartClient myClient1 tcp://localhost:8085/PCSServicesName tcp://localhost:8087/ClientServicesName 200 2
Wait 1000
StartClient myClient2 tcp://localhost:8085/PCSServicesName tcp://localhost:8088/ClientServicesName 200 2
Wait 5000
Crash myServer
Wait 5000
Crash myClient1
GlobalStatus
InjectDelay myServer myClient1
Freeze myClient1


### Example how to start client

Client1 ftp://localhost:8087/client Server ftp://localhost:8085/server 1000
