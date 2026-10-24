# Future TODOs

## Planned
- Client: digital receipt link as QR code on the customer display.
- Stack: multiple POS clients; transactions carry a client id (format to be defined).
- Card payments: several readers, one per POS client (see multiple POS clients).

## Bugs

## To decide
- Cloud: how to fix transactions where the Pretix order or Fiskaly transaction failed (currently no way).
- Client: items sometimes added twice on fast taps with touch emulation in the browser; not seen with a mouse. Check again on the real touch screen.

## Technical
- Quality refactoring: analyze the code and make it more compact.
