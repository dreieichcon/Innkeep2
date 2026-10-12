# Future TODOs

## Planned
- Client: NonCash (card) payment.
- Client: digital receipt link as QR code on the customer display.
- Client: remove keypad; move buttons to the right for different functions (see force pull).
- Stack: multiple POS clients; transactions carry a client id (format to be defined).
- Force pull of sales items: Client or Server can trigger; invalidates all caches up to Cloud and reloads all items.
- Sales items: order by category, then by position within the category.

## Bugs
- Cloud: Pretix request culture code must come from Pretix and only allow cultures enabled in the event.
- Cloud/Server: UI only works at 1440p. Make it percentage-based so it scales with screen size.
- Server/Client: hide items with stock 0 and items not currently active (e.g. time-limited).

## To decide
- Cloud: how to fix transactions where the Pretix order or Fiskaly transaction failed (currently no way).

## Technical
- Quality refactoring: analyze the code and make it more compact.