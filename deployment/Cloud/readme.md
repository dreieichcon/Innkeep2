To deploy the cloud:

1. Download this entire folder and place it on the machine that runs the cloud.
2. Copy `credentials/credentials.cloud.example.json` to `credentials/credentials.json` and fill it in. The cloud reads exactly
   this file name.
   - `pretix.keys`: one entry per Pretix account. The first entry with `"active": true` is used. `baseUrl` is the API address
     of the Pretix instance and has to end with `/api/v1/`.
   - `fiskaly.keys`: the same for Fiskaly, with `apiKey` and `apiSecret`. Set `isTest` for test accounts.
   - `keycloak`: the realm address as `authority`, plus `clientid` and `clientsecret` of the Keycloak client.
   - `discord.WebhookUrl`: optional. Warnings and errors are posted to this Discord webhook. Leave it empty to turn this off.
3. In Keycloak, give the client a role named `innkeep2-admin` and assign it to everyone who may configure the cloud. Only
   they see the configuration menu. Add the address of the cloud as a valid redirect URI of the client.
4. Put a reverse proxy with HTTPS in front of the container, forwarding to port 8087. It has to pass on the
   `X-Forwarded-For` and `X-Forwarded-Proto` headers, as the login depends on the real address and scheme.
5. Log in to the docker hub, run `docker compose pull`, then `docker compose up -d`.
6. Log in to the cloud. Choose the Pretix event, the Fiskaly TSS and client and the order database under Config, and create the
   API keys for the servers under "Api Config".

Notes:
- The container only speaks plain HTTP on port 8087. Do not expose that port directly to the internet.
- The cloud loads the saved configuration on startup, so a redeploy does not need a login to get going again.
- `db/` holds the settings and one order database per day. These contain the transactions, so back the folder up.
  `logs/` holds the log files and `dataprotection-keys/` the keys that protect logins and forms, so nobody has to log in
  again after a redeploy. On Linux the container user must be allowed to write to all three,
  e.g. `chmod 777 db logs dataprotection-keys`.
