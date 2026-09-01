import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const data = readFileSync(join(root, 'src', 'subject-explorer-data.ts'), 'utf8');
const panel = readFileSync(join(root, 'src', 'SubjectLensesPanel.tsx'), 'utf8');
const shell = readFileSync(join(root, 'src', 'TeacherShell.tsx'), 'utf8');
const failures = [];
const ids = [...data.matchAll(/\{ id: '([^']+)'/g)].map((match) => match[1]);

if (ids.length !== 14) failures.push(`expected 14 learner subject prompts, found ${ids.length}`);
if (new Set(ids).size !== ids.length) failures.push('learner subject prompt ids must be unique');
for (const marker of ['Pick one subject', 'TRY THIS', 'SHOW YOUR LEARNING', 'Open teacher planning and curriculum notes', 'This is a practice idea, not a curriculum claim.']) {
  if (!panel.includes(marker)) failures.push(`missing subject explorer marker: ${marker}`);
}
if (!panel.includes("useState<SubjectLensId>('science')")) failures.push('science must remain the calm default learner subject');
if (!panel.includes("useState<StageLensId>('ks2')")) failures.push('KS2 must remain the default stage for the main learner experience');
if (!shell.includes("'workspace-subjects': () => <SubjectLensesPanel />")) failures.push('subject guidance must render only the subject explorer');
if (!shell.includes("'workspace-safe-code-lab': () => <SafeCodeLabPanel />")) failures.push('Safe Code Lab must retain its own workspace');

if (failures.length) {
  console.error(JSON.stringify({ contract: 'learner-subject-explorer', status: 'failed', failures }, null, 2));
  process.exit(1);
}

console.log(JSON.stringify({ contract: 'learner-subject-explorer', status: 'passed', subjectCount: ids.length, defaultSubject: 'science', defaultStage: 'ks2' }));
