To deploy: 
Download this entire folder and place it on the target machine.
Edit the Caddyfile and replace innkeep-server with the hostname of your machine, or set the hostname of your machine to innkeep-server. 
Log in to the docker hub, run docker compose pull, then docker compose up
The /exported-ca folder will hold a root certificate you will need to move to the machine running the client and trust