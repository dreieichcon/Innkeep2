# Innkeep2 Features

Point-of-sale for events. Sells Pretix items, signs sales fiscally (Fiskaly TSS).

```
Client (POS client, per register) → Server (local, per venue) → Cloud (central) → Pretix, Fiskaly
```

| App    | Role                                                        | Details |
|--------|-------------------------------------------------------------|---------|
| Client | POS client UI: cart, payment, customer display              | [Client/](Client/README.md) |
| Server | Local hub: caches data, queues requests offline, prints     | [Server/](Server/README.md) |
| Cloud  | Source of truth: config, Pretix orders, Fiskaly, transactions | [Cloud/](Cloud/README.md) |

## Core
- Transaction types: Sale, Refund, Transfer.
- Payment types: Cash, NonCash.
- Every request has a unique id (idempotent).
- Server keeps working when Cloud is unreachable; queued requests retry.
- Each hop authenticates with an API key.
