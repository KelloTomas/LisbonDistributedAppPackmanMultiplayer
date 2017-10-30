# packmanMultiplayer

# Example of input file for PuppetMaster

StartServer Server No_PCS tcp://localhost:8085/server 1000 2
Wait 1000
StartClient Client1 NO_PCS tcp://localhost:8087/client 1000 2
StartClient Client2 NO_PCS tcp://localhost:8088/client 1000 2

# Example how to start client

Client1 ftp://localhost:8087/client Server ftp://localhost:8085/server 1000
