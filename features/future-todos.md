# Future TODOs

## Planned
- Cloud: overview page for card payment requests (status, amounts, SumUp ids, order assigned or not).
- Cloud: re-trigger the order flow for a payment that SumUp confirmed (status Paid) but that has no order, for the case that order creation never ran.
- Cloud: re-check payment requests left hanging as Pending against SumUp, from the overview page.
- Client: digital receipt link as QR code on the customer display.
- Stack: multiple POS clients; transactions carry a client id (format to be defined).
- Card payments: refunds of card payments only change Pretix, they do not refund on the card (SumUp refund not implemented).
- Card payments: several readers, one per POS client (see multiple POS clients).

## Bugs

## To decide
- Cloud: how to fix transactions where the Pretix order or Fiskaly transaction failed (currently no way).
- Client: items sometimes added twice on fast taps with touch emulation in the browser; not seen with a mouse. Check again on the real touch screen.

## Technical
- Quality refactoring: analyze the code and make it more compact.
