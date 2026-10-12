To deploy the client:

1. Download this entire folder and place it on the machine at the register.
2. Copy the server's root certificate (`deployment/server/exported-ca/innkeep2-server-root-ca.crt`) into the `certs/` folder
   of this deployment. Keep the file name, as `compose.yml` points to it.
3. Log in to the docker hub, run `docker compose pull`, then `docker compose up -d`.
4. Open http://localhost:5265 in the browser. As long as no credentials are set, the app opens the credentials page.
   - Server API key: the address of the server including `/api/v1`, e.g. `https://innkeep-server/api/v1`, and an API key
     generated on the server under "Api Config".
     The hostname has to be one the server's Caddyfile serves, otherwise the certificate does not match.
   - Discord: optional. Warnings and errors are posted to this Discord webhook. Leave it out to turn this off.
5. Restart the container once after the first setup with `docker compose restart`.
6. The customer display is under Config > "Kundendisplay öffnen".

Notes:
- The client is only bound to `127.0.0.1`, as it has no login of its own. Change the port mapping in `compose.yml` only if the
  register has to be used from another machine, and then protect it some other way.
- `db/` holds the printer settings and `credentials.db` with the API key and webhook. The credentials are stored unencrypted,
  so the folder has to be protected like a password file. `logs/` holds the log files and `dataprotection-keys/` the keys that
  protect forms and sessions, so they survive a restart. On Linux the container user must be allowed to write to all three,
  e.g. `chmod 777 db logs dataprotection-keys`.
- The receipt printer is reached over the network from inside the container. Enter its IP address under Config in the client.
- `SSL_CERT_FILE` replaces the system certificate store for the container, so the client trusts only the server's certificate.
  That is intended, as the client does not talk to anything else.
- Without a working server connection, the client starts anyway and logs warnings. The API key can be corrected on the
  credentials page.
