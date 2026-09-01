import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..', '..');
const panel = readFileSync(resolve(root, 'web/src/ClassroomPanel.tsx'), 'utf8');
const styles = readFileSync(resolve(root, 'web/src/ClassroomPanel.css'), 'utf8');
const student = readFileSync(resolve(root, 'web/src/ClassroomStudentShell.tsx'), 'utf8');

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

const requiredStudentMarkers = [
  'async function readClassroomJson<T>(response: Response): Promise<T>',
  'const body = await response.text()',
  "if (!body.trim()) throw new Error('The classroom sent an empty reply.')",
  'return JSON.parse(body) as T',
  'The teacher classroom is not ready. Ask your teacher to check it.',
  'The classroom could not be reached. Ask your teacher for help.',
  'Your work could not be sent. It has not been marked as saved.',
  'The print request could not be sent.',
];

for (const marker of requiredPanelMarkers) {
  if (!panel.includes(marker)) throw new Error(`Classroom journey contract missing panel marker: ${marker}`);
}

for (const marker of requiredStyleMarkers) {
  if (!styles.includes(marker)) throw new Error(`Classroom journey contract missing style marker: ${marker}`);
}

for (const marker of requiredStudentMarkers) {
  if (!student.includes(marker)) throw new Error(`Classroom journey contract missing learner recovery marker: ${marker}`);
}

if (student.includes('response.json()')) {
  throw new Error('Learner classroom must reject empty or malformed responses before JSON parsing reaches UI state.');
}

const classroomJsonUses = student.match(/readClassroomJson(?:<[^>]+>)?\(/g) ?? [];
if (classroomJsonUses.length !== 5) {
  throw new Error(`Learner classroom response boundary must cover its helper plus four JSON endpoints; found ${classroomJsonUses.length}.`);
}

for (const forbidden of ['public network is allowed', 'code can be reused', 'internet account required']) {
  if (panel.toLowerCase().includes(forbidden)) throw new Error(`Classroom journey contains unsafe guidance: ${forbidden}`);
}

console.log('Classroom journey contract verified: honest sharing states, one-use access, explicit revocation, managed-network guidance, responsive layout, and child-friendly malformed-response recovery.');
