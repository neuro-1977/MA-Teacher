import { useMemo, useState } from 'react';
import { feedbackPlanningEntries } from './feedback-planning-data';
import { questioningPlanningEntries } from './questioning-planning-data';
import { stageLenses } from './stage-lenses';
import { subjectLenses } from './subject-lenses';
import { teachingBankAuthoringRequirements, type TeachingBankAuthoringRequirement, type TeachingBankId } from './teaching-bank-authoring-requirements';
import { vocabularyEntries } from './vocabulary-planning-data';
import { workedExamples } from './worked-examples';
import './teaching-data-authoring-queue.css';

type MissingBankTask = {
  id: string;
  subject: string;
  subjectId: string;
  stage: string;
  stageId: string;
  stageSupport: string;
  requirement: TeachingBankAuthoringRequirement;
};

function hasEntry(bank: TeachingBankId, subject: string, stage: string) {
  if (bank === 'vocabulary') return vocabularyEntries.some((item) => item.subject === subject && item.stages.some((value) => value === stage));
  if (bank === 'questioning') return questioningPlanningEntries.some((item) => item.subject === subject && item.stages.some((value) => value === stage));
  if (bank === 'feedback') return feedbackPlanningEntries.some((item) => item.subject === subject && item.stages.some((value) => value === stage));
  return workedExamples.some((item) => item.subject === subject && item.stage === stage);
}

export function TeachingDataAuthoringQueuePanel() {
  const [bank, setBank] = useState<'All banks' | TeachingBankId>('All banks');
  const [subject, setSubject] = useState('All subjects');
  const [stage, setStage] = useState('All stages');
  const [copyState, setCopyState] = useState('');
  const tasks = useMemo<MissingBankTask[]>(() => subjectLenses.flatMap((subjectLens) => stageLenses.flatMap((stageLens) => teachingBankAuthoringRequirements
    .filter((requirement) => !hasEntry(requirement.id, subjectLens.label, stageLens.id))
    .map((requirement) => ({
      id: `${requirement.id}-${subjectLens.id}-${stageLens.id}`,
      subject: subjectLens.label,
      subjectId: subjectLens.id,
      stage: stageLens.label,
      stageId: stageLens.id,
      stageSupport: stageLens.support,
      requirement,
    })))), []);
  const visible = tasks.filter((task) => (bank === 'All banks' || task.requirement.id === bank)
    && (subject === 'All subjects' || task.subjectId === subject)
    && (stage === 'All stages' || task.stageId === stage));

  async function copyTemplate(task: MissingBankTask) {
    if (!navigator.clipboard) return setCopyState(`${task.id}:failed`);
    const template = [
      'MA-TEACHER / SOURCE-ONLY TEACHING-DATA AUTHORING TEMPLATE',
      `Missing bank: ${task.requirement.label}`,
      `Subject: ${task.subject}`,
      `Stage lens: ${task.stage} (${task.stageSupport})`,
      `Target source: ${task.requirement.sourcePath}`,
      `Boundary: ${task.requirement.boundaryPath}`,
      '',
      'Required fields:',
      ...task.requirement.requiredFields.map((field) => `- ${field}: [human-authored value]`),
      '',
      `Authority boundary: ${task.requirement.authoringBoundary}`,
      '',
      'Required review evidence:',
      ...task.requirement.reviewEvidence.map((item) => `- ${item}: [not reviewed]`),
      '',
      'Receipt truth:',
      '- Actor: [record the actual actor]',
      '- Crew activity: [record actual activity or none]',
      '- Crew response: [record actual response or none]',
      '- external assistant used: [true/false]',
      '- external automation used: [true/false]',
      '- Build/runtime evidence: [do not infer]',
      '',
      'This template does not authorize a source edit, reconcile counts, approve content, or establish curriculum/learner authority.',
    ].join('\n');
    try { await navigator.clipboard.writeText(template); setCopyState(`${task.id}:copied`); }
    catch { setCopyState(`${task.id}:failed`); }
  }

  return (
    <section className="authoring-queue" id="workspace-authoring-queue" aria-labelledby="authoring-queue-title">
      <header><div><p>Missing-source workbench</p><h2 id="authoring-queue-title">Teaching-data authoring queue</h2><span>Turn exact source gaps into bounded contribution templates without creating or prioritizing content automatically.</span></div><strong>{visible.length} OF {tasks.length} GAPS</strong></header>
      <aside className="authoring-queue__boundary" role="note"><b>No autonomous authoring.</b> A queue item means only that one current source array has no exact subject-stage match. It does not mean the material is educationally urgent, required by a curriculum, or safe to add without the named reviews.</aside>
      <div className="authoring-queue__filters">
        <label>Bank<select value={bank} onChange={(event) => setBank(event.target.value as 'All banks' | TeachingBankId)}><option>All banks</option>{teachingBankAuthoringRequirements.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Subject<select value={subject} onChange={(event) => setSubject(event.target.value)}><option>All subjects</option>{subjectLenses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Stage<select value={stage} onChange={(event) => setStage(event.target.value)}><option>All stages</option>{stageLenses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
      </div>
      {visible.length === 0 ? <p className="authoring-queue__empty">No missing bank matches these filters. This is not proof that the filtered material is sufficient or accepted.</p> : <div className="authoring-queue__grid">{visible.map((task) => <article key={task.id}>
        <div className="authoring-queue__heading"><div><p>{task.requirement.label}</p><h3>{task.subject} / {task.stage}</h3></div><span>{task.stageSupport}</span></div>
        <dl><dt>Target source</dt><dd><code>{task.requirement.sourcePath}</code></dd><dt>Required fields</dt><dd><ul>{task.requirement.requiredFields.map((field) => <li key={field}>{field}</li>)}</ul></dd><dt>Review before contribution</dt><dd><ul>{task.requirement.reviewEvidence.map((item) => <li key={item}>{item}</li>)}</ul></dd><dt>Authority boundary</dt><dd>{task.requirement.authoringBoundary}</dd></dl>
        <footer><button type="button" onClick={() => copyTemplate(task)}>Copy unfilled template</button><span aria-live="polite">{copyState === `${task.id}:copied` ? 'Copied; all review fields remain unfilled.' : copyState === `${task.id}:failed` ? 'Clipboard refused; nothing was saved.' : task.id}</span></footer>
      </article>)}</div>}
    </section>
  );
}
