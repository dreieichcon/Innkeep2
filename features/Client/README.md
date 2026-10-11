# Client

POS client used by cashiers. Talks only to the Server.

## Scope
- Sells tickets only. No refunds or transfers (Server only).

## POS (`/`)
- Lists sales items from Server; refreshes every 5 s, keeps last list if Server is unreachable.
- Tap item → adds to cart. Quantity comes from keypad (default 1) and applies to the next tap.
- Same item + variation merges into one cart line.
- Cart shows name, quantity, price; "Leeren" clears it.
- Total shown live.
- BEZAHLEN → Pay. Disabled with empty cart.
- Bon Drucken: prints the last receipt on request (legally required on customer request), marked as copy. Disabled until a sale happened.

## Pay (`/Pay`)
- Cash only.
- Amount given: quick buttons (1, 2, 5, 10, 20, 50, 100) add up; keypad sets exact amount; "Zurücksetzen" resets.
- Shows change.
- BESTELLUNG ABSCHICKEN: enabled when cart not empty and amount given ≥ total.
- Zurück returns to POS, cart kept.

## Submit
- Sends order with new request id, items, amount given, EUR.
- Success:
  - Print vouchers automatically if the order has any (needed to redeem physical products).
  - Open cash drawer.
  - Show Payment Complete dialog.
  - Refresh sales items (stock).
- Timeout: warn that the order may have gone through; check status before resubmitting.
- Other error: show message, cart and amount kept.

## Payment Complete Dialog
- Opens when the order is confirmed, either by Cloud (online, returned through the Server) or by the Server (offline, Cloud unreachable).
- Shows time, order id, total, given, change, Pretix status and Fiskaly status ("Ok" or "OFFLINE").
- Bon Drucken: prints the receipt (not marked as copy).
- Schließen: clears the cart and returns to POS.
- Customer display shows the summary while open.

## Customer Display (`/customer-display`)
- Separate window, opened from Config.
- Shows cart and total; shows payment summary while the Payment Complete dialog is open.

## Config (`/Config`)
- Network printer; usually the same as the Server's, but may differ: IP, port, save, test print, open drawer.
- Open customer display.
