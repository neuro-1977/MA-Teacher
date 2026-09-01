import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const data = readFileSync(join(root, 'src', 'subject-explorer-data.ts'), 'utf8');
const panel = readFileSync(join(root, 'src', 'SubjectLensesPanel.tsx'), 'utf8');
const shell = readFileSync(join(root, 'src', 'TeacherShell.tsx'), 'utf8');
const stages = readFileSync(join(root, 'src', 'stage-lenses.ts'), 'utf8');
const failures = [];
const ids = [...data.matchAll(/\{ id: '([^']+)'/g)].map((match) => match[1]);

if (ids.length !== 14) failures.push(`expected 14 learner subject prompts, found ${ids.length}`);
if (new Set(ids).size !== ids.length) failures.push('learner subject prompt ids must be unique');
for (const marker of ['Pick one subject', 'TRY THIS', 'SHOW YOUR LEARNING', 'Open teacher planning and curriculum notes', 'This is a practice idea, not a curriculum claim.']) {
  if (!panel.includes(marker)) failures.push(`missing subject explorer marker: ${marker}`);
}
if (!panel.includes("useState<SubjectLensId>('science')")) failures.push('science must remain the calm default learner subject');
if (!panel.includes("useState<StageLensId>('ks2')")) failures.push('KS2 must remain the default stage for the main learner experience');
if (!panel.includes('showTeacherDetails = false') || !panel.includes('Teacher planning stays in Teacher view.')) failures.push('dense teacher subject notes must stay out of Simple view');
if (!shell.includes("'workspace-subjects': (view) => <SubjectLensesPanel showTeacherDetails={view === 'teacher'} />")) failures.push('subject guidance must receive the current learner or teacher presentation state');
if ((stages.match(/learnerCue:/g) ?? []).length !== 6) failures.push('every learning stage must provide one short learner-facing approach cue');
if (!shell.includes("'workspace-safe-code-lab': () => <SafeCodeLabPanel />")) failures.push('Safe Code Lab must retain its own workspace');

if (failures.length) {
  console.error(JSON.stringify({ contract: 'learner-subject-explorer', status: 'failed', failures }, null, 2));
  process.exit(1);
}

console.log(JSON.stringify({ contract: 'learner-subject-explorer', status: 'passed', subjectCount: ids.length, defaultSubject: 'science', defaultStage: 'ks2' }));
