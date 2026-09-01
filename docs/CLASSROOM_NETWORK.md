# Classroom network preview

MA-Teacher keeps the full teacher UI and operator API on loopback TCP `5201`. That port must never be exposed to another device.

The classroom relay is a separate, opt-in listener on TCP `5202`. It starts only when a teacher creates an invite and stops when the teacher revokes sharing or closes MA-Teacher.

## Current checkpoint boundary

The `0.1.0` installer now offers an administrator-only, unchecked `Allow supervised student links` task. When selected, it creates the exact `http://+:5202/` URL reservation and one inbound firewall rule confined to `MA-Teacher.exe`, TCP `5202`, and Domain/Private profiles. It never enables Public networks. Setup records ownership only after both changes succeed, rolls back a partial change, and uninstall removes the entries only when that ownership marker exists.

The ordinary per-user install remains non-administrative and creates no inbound rule. This is deliberate: a teacher who uses MA-Teacher on one computer does not need a LAN listener.

## Setup choices

1. School IT verifies the installer SHA-256.
2. Run Setup in administrator mode for the intended teacher installation.
3. Select `Allow supervised student links on this school's private network (TCP 5202)`.
4. Keep the Windows network classified as Domain or Private. Never change it to Public merely to make the link work.
5. After installation, verify the exact URL reservation and firewall rule using the commands in the school IT guide.

If the URL reservation is already owned and MA-Teacher has no ownership marker, Setup fails closed and leaves the existing reservation untouched. IT must identify the existing owner rather than deleting it blindly.

The final installer must offer an explicit elevated school-network task that reserves `http://+:5202/`, limits an inbound rule to the MA-Teacher executable on TCP `5202`, Domain/Private profiles and `LocalSubnet`, leaves TCP `5201` loopback-only, and removes its exact changes during uninstall.

## Implemented application boundary

- One-use invite codes are hashed, expire after 5-240 minutes and are rate-limited after failures.
- Learner sessions use random 256-bit tokens stored as hashes in memory and HttpOnly SameSite cookies.
- Only private/local source addresses are accepted.
- The learner API exposes join, assigned approved lesson, current checks, that learner's attempts, submission, print request and logout.
- Teacher planning, learner lists, curriculum authoring, raw database access, development tools and attachment download are absent from the relay.
- Profanity, explicit content, slurs, unsafe links, obfuscation and safety-bypass attempts are blocked and recorded as privacy-minimised teacher incidents.
- Learner print requests contain only learner, lesson and a server-owned document kind; a teacher must choose a locally detected printer and approve.

## Required final acceptance test

1. Install using the elevated classroom-network task on a test teacher laptop.
2. Prove TCP `5201` is unreachable from a second device.
3. Prove TCP `5202` is unreachable on a Public profile and reachable only from `LocalSubnet` on Domain/Private.
4. Join one synthetic learner with one current approved lesson.
5. Prove a wrong code, reused code, expired code and repeated failures refuse.
6. Prove teacher API paths and another learner's records remain unreachable.
7. Submit synthetic work, trigger one safety report and request one print.
8. Prove printing requires teacher approval and an actually detected printer.
9. Stop sharing and prove the invite/session immediately stops working.
10. Uninstall and prove the exact firewall rule and URL reservation are removed.

## Honest transport limit

The preview relay uses local HTTP. An isolated, managed WPA2/WPA3 classroom network or VLAN is part of its security boundary. HTTP cannot protect against an active attacker controlling that network. School PKI/TLS or a managed native learner application is required before claiming stronger transport confidentiality.
