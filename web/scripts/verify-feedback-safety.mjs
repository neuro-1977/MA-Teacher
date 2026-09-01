import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..');
const panel = readFileSync(resolve(root, 'src/FeedbackHubPanel.tsx'), 'utf8');
const surfaces = readFileSync(resolve(root, 'src/workspace-surfaces.ts'), 'utf8');
const bugForm = readFileSync(resolve(root, '../.github/ISSUE_TEMPLATE/bug_report.yml'), 'utf8');
const featureForm = readFileSync(resolve(root, '../.github/ISSUE_TEMPLATE/feature_request.yml'), 'utf8');

const requiredPanelMarkers = [
  'Nothing is sent or saved automatically.',
  'inspectFeedbackDraft',
  'Keep private things private.',
  'A responsible adult checks this next.',
  'I am a responsible adult and I reviewed this draft.',
  'Public feedback stays locked until an adult checks the box.',
  'navigator.clipboard.writeText(reviewedDraft)',
  'writes no database or browser storage',
  'invokes no model',
];

for (const marker of requiredPanelMarkers) {
  if (!panel.includes(marker)) throw new Error(`Feedback safety marker missing: ${marker}`);
}

for (const forbidden of ['localStorage', 'sessionStorage', 'fetch(', 'axios', 'window.open(']) {
  if (panel.includes(forbidden)) throw new Error(`Feedback panel must remain local and user-driven; found ${forbidden}`);
}

if (!surfaces.includes("effect: 'clipboard-optional'")) throw new Error('Feedback workspace must declare its optional clipboard effect.');

for (const [name, form] of [['bug', bugForm], ['feature', featureForm]]) {
  if (!form.includes('id: privacy')) throw new Error(`${name} issue form has no privacy gate.`);
  const requiredChecks = (form.match(/required: true/g) ?? []).length;
  if (requiredChecks < 4) throw new Error(`${name} issue form does not require adult/privacy confirmation and core evidence.`);
  if (!form.includes('responsible adult reviewed every word')) throw new Error(`${name} issue form has no responsible-adult confirmation.`);
}

console.log('Feedback safety contract verified: local draft, bounded checks, adult review, and public-form privacy gates.');
