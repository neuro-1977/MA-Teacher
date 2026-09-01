import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..', '..');
const panel = readFileSync(resolve(root, 'web/src/LearningProgressPanel.tsx'), 'utf8');
const shell = readFileSync(resolve(root, 'web/src/TeacherShell.tsx'), 'utf8');

const requiredPanelMarkers = [
  'showTeacherDetails = false',
  'value={value}>{value === \'all\' ? \'All subjects\'',
  'Full work stays in Teacher view.',
  'Trail markers celebrate saved activity only.',
  'They are not grades.',
  'We never use these counts to guess your ability, rank, or final result.',
];

const requiredShellMarkers = [
  "'workspace-progress': (view) => <LearningProgressPanel showTeacherDetails={view === 'teacher'} />",
  'activeRenderer(view)',
];

for (const marker of requiredPanelMarkers) {
  if (!panel.includes(marker)) throw new Error(`Learning progress contract missing panel marker: ${marker}`);
}

for (const marker of requiredShellMarkers) {
  if (!shell.includes(marker)) throw new Error(`Learning progress contract missing shell marker: ${marker}`);
}

for (const forbidden of ['leaderboard', 'daily streak', 'ability score', 'automatic grade']) {
  if (panel.toLowerCase().includes(forbidden)) throw new Error(`Learning progress contract contains forbidden pressure/scoring language: ${forbidden}`);
}

console.log('Learning progress contract verified: honest activity, gentle markers, stable filters, and teacher-only full evidence.');
