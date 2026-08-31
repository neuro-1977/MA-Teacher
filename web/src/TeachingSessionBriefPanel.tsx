import { useMemo, useState } from 'react';
import { stageLenses } from './stage-lenses';
import { subjectLenses } from './subject-lenses';
import './teaching-session-brief.css';

type CopyState = 'idle' | 'copied' | 'failed';

export function TeachingSessionBriefPanel() {
  const [subjectId, setSubjectId] = useState<string>(subjectLenses[0].id);
  const [stageId, setStageId] = useState<string>(stageLenses[0].id);
  const [topic, setTopic] = useState('');
  const [goal, setGoal] = useState('');
  const [evidence, setEvidence] = useState('');
  const [accessPlan, setAccessPlan] = useState('');
  const [copyState, setCopyState] = useState<CopyState>('idle');

  const subject = subjectLenses.find((item) => item.id === subjectId) ?? subjectLenses[0];
  const stage = stageLenses.find((item) => item.id === stageId) ?? stageLenses[0];
  const missing = [
    !topic.trim() && 'topic or context',
    !goal.trim() && 'narrow learning goal',
    !evidence.trim() && 'observable evidence',
  ].filter((item): item is string => Boolean(item));

  const briefText = useMemo(() => [
    'MA-TEACHER / NON-CANONICAL TEACHING-SESSION BRIEF',
    `Subject lens: ${subject.label}`,
    `Stage lens: ${stage.label} (${stage.support})`,
    `Topic or context: ${topic.trim() || '[not supplied]'}`,
    `Narrow learning goal: ${goal.trim() || '[not supplied]'}`,
    `Observable evidence: ${evidence.trim() || '[not supplied]'}`,
    `Access and participation plan: ${accessPlan.trim() || '[not supplied]'}`,
    '',
    `Subject purpose: ${subject.promise}`,
    `Curriculum boundary: ${stage.curriculumBoundary}`,
    '',
    'Disciplinary habits:',
    ...subject.disciplinaryHabits.map((item) => `- ${item}`),
    '',
    'Subject planning questions:',
    ...subject.planningQuestions.map((item) => `- ${item}`),
    '',
    'Stage planning focus:',
    ...stage.planningFocus.map((item) => `- ${item}`),
    '',
    'Response options:',
    ...stage.responseOptions.map((item) => `- ${item}`),
    '',
    'Disciplinary evidence forms:',
    ...subject.evidenceForms.map((item) => `- ${item}`),
    '',
    'Dignity and interpretation cautions:',
    ...stage.dignityRules.map((item) => `- ${item}`),
    ...subject.cautions.map((item) => `- ${item}`),
    '',
    'Boundary: browser-memory planning aid only. This is not a canonical lesson, curriculum approval, learner record, teaching-session receipt, or evidence of effectiveness.',
  ].join('\n'), [accessPlan, evidence, goal, stage, subject, topic]);

  async function copyBrief() {
    if (missing.length > 0 || !navigator.clipboard) return;
    try {
      await navigator.clipboard.writeText(briefText);
      setCopyState('copied');
    } catch {
      setCopyState('failed');
    }
  }

  function resetBrief() {
    setTopic('');
    setGoal('');
    setEvidence('');
    setAccessPlan('');
    setCopyState('idle');
  }

  return (
    <section className="session-brief" id="workspace-session-brief" aria-labelledby="session-brief-title">
      <header>
        <div><p>Plan before records</p><h2 id="session-brief-title">Teaching-session brief</h2><span>Combine one subject lens and one stage lens before opening canonical authoring surfaces.</span></div>
        <strong>{missing.length === 0 ? 'READY TO COPY' : `${missing.length} REQUIRED FIELD${missing.length === 1 ? '' : 'S'} MISSING`}</strong>
      </header>
      <aside className="session-brief__boundary" role="note"><b>Browser memory only.</b> Do not enter names, health, safeguarding, family, immigration, financial, account, or other identifying information. Copying does not save or approve a lesson.</aside>
      <div className="session-brief__form">
        <label>Subject lens<select value={subjectId} onChange={(event) => { setSubjectId(event.target.value); setCopyState('idle'); }}>{subjectLenses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Stage lens<select value={stageId} onChange={(event) => { setStageId(event.target.value); setCopyState('idle'); }}>{stageLenses.map((item) => <option key={item.id} value={item.id}>{item.label} / {item.support}</option>)}</select></label>
        <label>Topic or context<input maxLength={160} value={topic} onChange={(event) => { setTopic(event.target.value); setCopyState('idle'); }} placeholder="For example: why shadows change" /></label>
        <label>Narrow learning goal<textarea maxLength={420} rows={3} value={goal} onChange={(event) => { setGoal(event.target.value); setCopyState('idle'); }} placeholder="One observable learning intention, without a score or learner label." /></label>
        <label>Observable evidence<textarea maxLength={420} rows={3} value={evidence} onChange={(event) => { setEvidence(event.target.value); setCopyState('idle'); }} placeholder="What could the learner say, make, show, compare, calculate, explain, or revise?" /></label>
        <label>Access and participation plan / optional<textarea maxLength={560} rows={3} value={accessPlan} onChange={(event) => { setAccessPlan(event.target.value); setCopyState('idle'); }} placeholder="Representations, response choices, pacing, language access, or environmental adjustments. No personal data." /></label>
      </div>
      <div className="session-brief__preview" aria-live="polite">
        <article><p>{subject.label} / disciplinary purpose</p><h3>{subject.promise}</h3><h4>Disciplinary habits</h4><ul>{subject.disciplinaryHabits.map((item) => <li key={item}>{item}</li>)}</ul><h4>Planning questions</h4><ul>{subject.planningQuestions.map((item) => <li key={item}>{item}</li>)}</ul></article>
        <article><p>{stage.label} / {stage.support}</p><h3>{stage.curriculumBoundary}</h3><ul>{stage.planningFocus.map((item) => <li key={item}>{item}</li>)}</ul></article>
        <article className="session-brief__caution"><p>Keep interpretation bounded</p><h3>Response choice and dignity</h3><ul>{stage.responseOptions.map((item) => <li key={item}>{item}</li>)}{stage.dignityRules.map((item) => <li key={item}>{item}</li>)}</ul></article>
      </div>
      {missing.length > 0 && <p className="session-brief__missing">Add {missing.join(', ')} before copying. The app does not invent missing teaching intent.</p>}
      <footer><button type="button" onClick={copyBrief} disabled={missing.length > 0}>Copy non-canonical brief</button><button type="button" className="session-brief__reset" onClick={resetBrief}>Clear local fields</button><span aria-live="polite">{copyState === 'copied' ? 'Copied. Paste deliberately into the next human workflow.' : copyState === 'failed' ? 'Clipboard refused. Nothing was saved.' : 'No database write, model call, fetch, score, or approval.'}</span></footer>
    </section>
  );
}
