To deploy the server:

1. Download this entire folder and place it on the machine at the event that the registers connect to.
2. Edit the `Caddyfile` and replace `desktop-tobi` with the hostname of the machine. The clients have to use exactly this
   name in their server address, otherwise the certificate does not match.
3. Log in to the docker hub, run `docker compose pull`, then `docker compose up -d`.
4. Open `https://<hostname>` in the browser. As long as no credentials are set, the app opens the credentials page.
   - Cloud API key: the address of the cloud, e.g. `https://innkeep2.example.com/`, and an API key generated on the cloud
     under "Api Config".
   - Discord: optional. Warnings and errors are posted to this Discord webhook. Leave it out to turn this off.
5. Restart the container once after the first setup with `docker compose restart`.
6. Create an API key for every client under "Api Config" and enter the receipt printer under "Printer Config".
   Use "Breitentest" there to find the width at which images are not cut off.
7. Give the root certificate in `exported-ca/innkeep2-server-root-ca.crt` to every client. For the docker deployment of the
   client it goes into its `certs/` folder. A browser on another machine has to trust it as well to open this site.

Notes:
- If the cloud cannot be reached at startup, the server starts anyway and logs a warning. Orders made while the cloud is
  down are queued and can be sent again under "Queue". A wrong API key can be corrected on the credentials page.
- Ports 80 and 443 are open to the whole network and the web interface has no login of its own. Run the server only on a
  network that is trusted.
- `db/` holds the data, the queue and `credentials.db` with the API key and webhook. The credentials are stored unencrypted,
  so the folder has to be protected like a password file. `logs/` holds the log files and `dataprotection-keys/` the keys that
  protect forms and sessions, so they survive a restart. On Linux the container user must be allowed to write to all three,
  e.g. `chmod 777 db logs dataprotection-keys`.
- `exported-ca/` is filled by the `export-ca` service with Caddy's root certificate once Caddy has created it.
  Caddy keeps its certificates in a docker volume, so the certificate stays the same unless that volume is deleted.
