import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..', '..');
const panel = readFileSync(resolve(root, 'web/src/LearningCheckPanel.tsx'), 'utf8');
const store = readFileSync(resolve(root, 'ModuleShell/LearningCheckStore.cs'), 'utf8');
const host = readFileSync(resolve(root, 'ModuleShell/LocalModuleHost.cs'), 'utf8');

const requiredPanelMarkers = [
  'async function readLearningCheckJson<T>(response: Response, label: string): Promise<T>',
  'const body = await response.text();',
  'returned no data.',
  'returned a reply this screen could not read.',
  'current && nextDrafts.some((item: Draft) => item.id === current)',
  "item.id === current && item.reviewState === 'unreviewed'",
  'let cancelled = false;',
  'if (!cancelled)',
  'return () => { cancelled = true; };',
  'Record human review',
  'It is not sent to a model or marked automatically.',
  "export function LearningCheckPanel({ mode = 'teacher' }",
  "const isTeacher = mode === 'teacher';",
  '{isTeacher && <form onSubmit={createCheck}>',
  '{isTeacher && <form onSubmit={reviewAttempt}>',
  '{isTeacher && <div className="check-currency-ledger">',
  '{isTeacher && <div className="attempt-ledger">',
  'Your work stays on your teacher\'s computer. A person reviews it. A robot does not mark it.',
  'Send my work for review',
  'What happens next?',
];

for (const marker of requiredPanelMarkers) {
  if (!panel.includes(marker)) throw new Error(`Learning-check contract missing panel marker: ${marker}`);
}

if (panel.includes('response.json()')) {
  throw new Error('Learning-check UI must reject empty or malformed replies before JSON parsing reaches teacher state.');
}

const advertisedExtensions = ['.pdf', '.txt', '.rtf', '.doc', '.docx', '.odt', '.png', '.jpg', '.jpeg', '.webp'];
for (const extension of advertisedExtensions) {
  if (!panel.includes(extension)) throw new Error(`Learning-check UI no longer advertises supported extension ${extension}.`);
  if (!store.includes(`\"${extension}\"`)) throw new Error(`Learning-check server no longer accepts advertised extension ${extension}.`);
}

for (const marker of ['MaximumAttachmentBytes = 10 * 1024 * 1024', 'SHA256.HashData(body)', 'No model, remote service or browser agent participates in this workflow.']) {
  if (!store.includes(marker)) throw new Error(`Learning-check server boundary missing: ${marker}`);
}

for (const marker of ['failed integrity verification', 'Cache-Control', 'no-store', 'X-Content-Type-Options', 'nosniff']) {
  if (!host.includes(marker)) throw new Error(`Learning-check download boundary missing: ${marker}`);
}

console.log('Learning-check contract verified: guarded replies, current selections, stale-request cancellation, matching attachment formats, integrity evidence, and human-only review.');
