To deploy the client:

1. Download this entire folder and place it on the machine at the register.
2. Copy `credentials/credentials.client.example.json` to `credentials/credentials.client.json` and fill it in:
   - `ServerUrl`: the address of the server including `/api/v1`, e.g. `https://innkeep-server/api/v1`.
     The hostname has to be one the server's Caddyfile serves, otherwise the certificate does not match.
   - `ApiKey`: generate one on the server under "Api Config".
   - `discord.WebhookUrl`: optional. Warnings and errors are posted to this Discord webhook. Leave it empty to turn this off.
3. Copy the server's root certificate (`deployment/server/exported-ca/innkeep2-server-root-ca.crt`) into the `certs/` folder
   of this deployment. Keep the file name, as `compose.yml` points to it.
4. Log in to the docker hub, run `docker compose pull`, then `docker compose up -d`.
5. Open http://localhost:5265 in the browser. The customer display is under Config > "Kundendisplay öffnen".

Notes:
- The client is only bound to `127.0.0.1`, as it has no login of its own. Change the port mapping in `compose.yml` only if the
  register has to be used from another machine, and then protect it some other way.
- `db/` holds the printer settings, `logs/` the log files and `dataprotection-keys/` the keys that protect forms and sessions,
  so they survive a restart. On Linux the container user must be allowed to write to all three,
  e.g. `chmod 777 db logs dataprotection-keys`.
- The receipt printer is reached over the network from inside the container. Enter its IP address under Config in the client.
- `SSL_CERT_FILE` replaces the system certificate store for the container, so the client trusts only the server's certificate.
  That is intended, as the client does not talk to anything else.
