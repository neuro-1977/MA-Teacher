# Printing and privacy

MA-Teacher detects printers already installed in Windows. It does not discover printers across the network, install drivers or accept a printer address from a learner.

## Learner requests

A joined learner may ask for one of two generated documents:

- the currently assigned approved lesson;
- their current practice responses and teacher feedback.

The request stores learner ID, lesson ID and document kind. It does not store printer commands, HTML, scripts or a learner-supplied document body. Repeated pending requests are coalesced.

## Teacher approval

The teacher chooses a printer from the current Windows list and approves or declines each request. At approval time MA-Teacher regenerates plain text from canonical database records and submits it to the selected Windows print queue. Uploaded learner files are never passed to the print system.

Safety reports are teacher-only and require the same explicit printer choice.

## School IT and privacy

- Install and manage printer drivers through the school's normal trusted process.
- Use a printer on the managed school network, not a personal or public cloud-print queue.
- Restrict printer ACLs to authorised staff and classroom devices.
- Remember that Windows print queues, print servers and device storage may retain document data.
- Collect pages promptly, do not leave learner work on the output tray, and shred/dispose of it under school policy.
- Test with synthetic learner records before approving real printing.

Detecting a printer does not prove that it is secure, online, stocked or physically in the intended room. The teacher remains responsible for checking the destination before approval.
