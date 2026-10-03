To deploy the server:

1. Download this entire folder and place it on the machine at the event that the registers connect to.
2. Copy `credentials/credentials.server.example.json` to `credentials/credentials.server.json` and fill it in:
   - `CloudUrl`: the address of the cloud, e.g. `https://innkeep2.example.com/`.
   - `ApiKey`: generate one on the cloud under "Api Config".
   - `discord.WebhookUrl`: optional. Warnings and errors are posted to this Discord webhook. Leave it empty to turn this off.
3. Edit the `Caddyfile` and replace `desktop-tobi` with the hostname of your machine. The clients have to use exactly this
   name in their `ServerUrl`, otherwise the certificate does not match.
4. Log in to the docker hub, run `docker compose pull`, then `docker compose up -d`.
5. Open `https://<hostname>` in the browser. Create an API key for every client under "Api Config" and enter the receipt
   printer under "Printer Config". Use "Breitentest" there to find the width at which images are not cut off.
6. Give the root certificate in `exported-ca/innkeep2-server-root-ca.crt` to every client. For the docker deployment of the
   client it goes into its `certs/` folder. A browser on another machine has to trust it as well to open this site.

Notes:
- The server needs the cloud at startup. If the cloud cannot be reached, the server exits, and `restart: always` keeps
  trying until it can. Once running, orders made while the cloud is down are queued and can be sent again under "Queue".
- Ports 80 and 443 are open to the whole network and the web interface has no login of its own. Run the server only on a
  network you trust.
- `db/` holds the data and the queue, `logs/` the log files and `dataprotection-keys/` the keys that protect forms and
  sessions, so they survive a restart. On Linux the container user must be allowed to write to all three,
  e.g. `chmod 777 db logs dataprotection-keys`.
- `exported-ca/` is filled by the `export-ca` service with Caddy's root certificate once Caddy has created it.
  Caddy keeps its certificates in a docker volume, so the certificate stays the same unless you delete that volume.
