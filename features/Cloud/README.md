# Cloud

Central app. Single source of truth for transactions. Admin web UI (login via Keycloak) and API for Servers (API key required).

## Active configuration
One active setup at a time, stored in Cloud:
- Pretix organizer and event, test mode on/off.
- Fiskaly TSS and fiscal client.
- Transaction database (file).
- Incomplete setup → orders, refunds and transfers are rejected.
- Shown in status bar: organizer, event, TSS.

## Data (API)
- Serves the active event and its sales items (cached from Pretix, stock refreshed after sales and refunds).

## Order
- Stores the transaction first, then creates the Pretix order and the Fiskaly transaction.
- Pretix or Fiskaly failing does not fail the order: the step is marked Failed, the receipt shows "PRETIX OFFLINE" / "TSS OFFLINE".
- Returns the receipt.
- Failed steps are not retried or repaired.

## Refund
- Of a sale, once. Already refunded → conflict.
- Needs a completed Pretix order (Pretix statistics would be wrong otherwise). Failed Pretix step on the sale → refund rejected.
- Stored as own transaction (negative amount) linked to the sale; Pretix refund, Fiskaly negative transaction.

## Transfer
- Cash in or out of the register. Fiskaly only, no Pretix.
- Needs TSS, client and event configured.

## Idempotency
- Retry of a known request id returns the stored receipt, nothing is created twice.

## Transactions (UI + API)
- Paged overview: id, Pretix order code, type, time, amount, Pretix and Fiskaly status, link to original for refunds.
- Detail view as receipt, incl. whether it was refunded.
- Dashboard: order count, total income, cash income, current register balance.

## Config (UI)
- Pretix: connection, organizer, event.
- Fiskaly: credentials, TSS lifecycle (deploy, set admin PIN, initialize, deactivate), fiscal client (create, deactivate).
- Database: select or create.
- API keys: create (named) and revoke.
- Sales items overview.
