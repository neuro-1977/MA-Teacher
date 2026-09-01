# Simple and Teacher loading boundary

MA-Teacher serves two audiences without building two products:

- **Simple** presents six learner-facing choices in plain language.
- **Teacher** preserves the complete planning, curriculum, safety, evidence and diagnostic toolkit.

The view switch is a presentation choice, not authentication. School access controls and teacher supervision remain separate responsibilities.

## Why surfaces load on demand

The first public `0.1.0` shell imported every workspace before showing the learner home. That made a learner download code for backups, curriculum review and development diagnostics they had not opened.

Workspace panels now use React lazy boundaries. The common shell, project identity, navigation, Simple home, effect labels and error boundary load first. A learner or teacher downloads a panel only after choosing it.

While a panel arrives, MA-Teacher shows a short status card:

> Getting this page ready. Your saved work is not changing.

The card is announced through `role=status`, `aria-live=polite` and `aria-busy=true`. It contains no timer, fake percentage or claim that a database action occurred.

## Evidence for the 0.1.0 refresh

- TypeScript contract: passed.
- Vite production build: passed.
- Initial JavaScript changed from approximately 665 kB to 251 kB.
- 57 JavaScript files were emitted, with workspace panels split into independent chunks.
- Browser proof opened the Simple home, lazy Guided setup, Teacher navigation and lazy Advanced workspace index.
- Returning from Advanced to Simple removed the advanced route and restored the learner home.
- The standalone Vite preview returned expected API 404 responses because it has no desktop database host; each mounted panel showed its bounded unavailable state rather than a blank screen.

The production build runs `web/scripts/verify-simple-shell.mjs`. It refuses eager panel imports, a missing accessible loading boundary, fewer than 40 JavaScript chunks, or an initial entry above 350,000 bytes.

## Maintenance rule

New teacher surfaces must remain lazy. Shared code belongs in the initial shell only when both Simple and Teacher need it before choosing a workspace. A smaller bundle is not permission to hide teacher capability or weaken safety and evidence wording.
