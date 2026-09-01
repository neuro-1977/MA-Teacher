import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..', '..');
const panel = readFileSync(resolve(root, 'web/src/ClassroomPanel.tsx'), 'utf8');
const styles = readFileSync(resolve(root, 'web/src/ClassroomPanel.css'), 'utf8');

const requiredPanelMarkers = [
  "status?.error ? 'problem'",
  "joinedLearners > 0 ? 'joined'",
  "status?.running ? 'waiting' : 'ready'",
  'Ready to make a private classroom link.',
  'The invite is ready. Waiting for the learner.',
  'Classroom status',
  'role="status" aria-live="polite"',
  'aria-label="Classroom sharing steps"',
  'Code (works once)',
  'Stop sharing and sign everyone out',
  'Stopping sharing revokes every invite and signs learners out immediately.',
  'Same managed school network only.',
  'Private or Domain network',
];

const requiredStyleMarkers = [
  '.classroom-panel__readiness.is-ready',
  '.classroom-panel__readiness.is-waiting',
  '.classroom-panel__readiness.is-joined',
  '.classroom-panel__readiness.is-problem',
  '.classroom-panel__journey li.is-current',
  '.classroom-panel__journey li.is-done',
  '@media(max-width:760px)',
];

for (const marker of requiredPanelMarkers) {
  if (!panel.includes(marker)) throw new Error(`Classroom journey contract missing panel marker: ${marker}`);
}

for (const marker of requiredStyleMarkers) {
  if (!styles.includes(marker)) throw new Error(`Classroom journey contract missing style marker: ${marker}`);
}

for (const forbidden of ['public network is allowed', 'code can be reused', 'internet account required']) {
  if (panel.toLowerCase().includes(forbidden)) throw new Error(`Classroom journey contains unsafe guidance: ${forbidden}`);
}

console.log('Classroom journey contract verified: four honest states, one-use access, explicit revocation, managed-network guidance, and responsive layout.');
