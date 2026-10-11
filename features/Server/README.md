# Server

Local hub between POS clients and Cloud. Also the admin tool for refunds, transfers and reprints.

## Startup
- Authenticates against Cloud; fails to start if that is rejected.
- Loads event and sales items.

## Data (API for Clients)
- Serves event and sales items from its cache.
- Sales items refresh after every sale and refund.
- Cached data is kept when Cloud is unreachable.

## Orders
- Accepts orders from Clients (API key required) and forwards them to Cloud.
- Cloud confirms → returns Cloud receipt.
- Cloud unreachable → queues the request, returns an offline receipt (Pretix/Fiskaly "OFFLINE").
- Same request id is never processed twice.

## Refund
- Transaction page, Storno button. Sales only, once per sale. Needs confirmation.
- Same offline behavior as orders: queued, offline refund receipt.
- Cloud says already refunded → shown as error, removed from queue.

## Transfer ("Geld Buchen")
- Deposit or withdrawal of cash in the register, amount > 0.
- Prints a receipt. Offline behavior as above.

## Transactions
- Loaded from Cloud. Overview list; detail page with type, time, total, payment type, Pretix and Fiskaly info.
- Queued transactions are flagged with a link to the queue.
- Reprint receipt.

## Queue
- Lists pending requests: type, id, queued at, attempts, last attempt, last error.
- Retry one or all, manually only: an outage can last long, so no automatic retry.
- Success removes the entry.

## Printing
- A print failure never blocks a refund, transfer or order; it only shows an error.
- Network printer configured on the Server; usually the same as the Client's, but may differ.
- Custom print: free text or image from a URL (optional rotate landscape).
- Printer config: IP, port, image width, test print, width test.

## API Keys
- Create (named) and revoke keys for Clients.
