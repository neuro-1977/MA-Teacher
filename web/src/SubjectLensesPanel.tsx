import { useMemo, useState } from 'react';
import { subjectExplorerPrompts } from './subject-explorer-data';
import { stageLenses, type StageLensId } from './stage-lenses';
import { subjectLenses, type SubjectLensId } from './subject-lenses';
import { InfoTip } from './InfoTip';
import './subject-lenses.css';

export function SubjectLensesPanel({ showTeacherDetails = false }: { showTeacherDetails?: boolean }) {
  const [subjectId, setSubjectId] = useState<SubjectLensId>('science');
  const [stageId, setStageId] = useState<StageLensId>('ks2');
  const subject = useMemo(() => subjectLenses.find((item) => item.id === subjectId) ?? subjectLenses[0], [subjectId]);
  const prompt = useMemo(() => subjectExplorerPrompts.find((item) => item.id === subjectId) ?? subjectExplorerPrompts[0], [subjectId]);
  const stage = useMemo(() => stageLenses.find((item) => item.id === stageId) ?? stageLenses[2], [stageId]);

  return <section id="workspace-subjects" className="subject-lenses-shell" aria-labelledby="subject-lenses-title">
    <header>
      <div><p>EXPLORE SUBJECTS</p><h2 id="subject-lenses-title">Pick one subject. Try one small idea.</h2><span>You do not need to finish a list. Choose something that makes you curious.</span></div>
      <InfoTip label="What is a subject guide?">This is practice and planning help, not a full curriculum. A teacher checks the right official source for the learner's place, stage and lesson.</InfoTip>
    </header>

    <nav className="subject-picker" aria-label="Pick one subject">
      {subjectExplorerPrompts.map((item, index) => <button key={item.id} type="button" className={item.id === subjectId ? 'is-active' : ''} aria-pressed={item.id === subjectId} onClick={() => setSubjectId(item.id)}><span>{String(index + 1).padStart(2, '0')}</span>{item.shortLabel}</button>)}
    </nav>

    <article className="subject-focus" aria-live="polite">
      <header><div><p>YOU PICKED</p><h3>{subject.label}</h3><strong>{prompt.invitation}</strong></div><label>Learning stage<select value={stageId} onChange={(event) => setStageId(event.target.value as StageLensId)}>{stageLenses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label></header>
      <div className="subject-practice-boundary"><strong>{stage.label}</strong><div><span>{stage.learnerCue}</span><small>This is a practice idea, not a curriculum claim. Ask a teacher to check that it suits you.</small></div></div>
      <div className="subject-learner-cards">
        <section><p>TRY THIS</p><h4>One small activity</h4><span>{prompt.tryThis}</span></section>
        <section><p>SHOW YOUR LEARNING</p><h4>Make your thinking visible</h4><span>{prompt.showIt}</span></section>
        <section><p>YOU CAN RESPOND BY</p><h4>Choose a comfortable way</h4><span>{stage.responseOptions[0]}</span></section>
      </div>

      {showTeacherDetails ? <details className="subject-teacher-notes"><summary>Open teacher planning and curriculum notes</summary><div className="subject-curriculum-boundary"><strong>{stage.support.replace('-', ' ')}</strong><span>{stage.curriculumBoundary}</span></div><p className="subject-promise">{subject.promise}</p><div className="subject-lens-columns"><section><h4>Ways to think</h4><ul>{subject.disciplinaryHabits.map((item) => <li key={item}>{item}</li>)}</ul></section><section><h4>Ways to show learning</h4><ul>{subject.evidenceForms.map((item) => <li key={item}>{item}</li>)}</ul></section><section><h4>Good planning questions</h4><ul>{subject.planningQuestions.map((item) => <li key={item}>{item}</li>)}</ul></section></div><aside><h4>Be careful</h4><ul>{subject.cautions.map((item) => <li key={item}>{item}</li>)}</ul></aside></details> : <p className="subject-teacher-boundary"><strong>Teacher planning stays in Teacher view.</strong> A teacher can open the curriculum boundary, evidence ideas and planning cautions there.</p>}
    </article>
  </section>;
}
