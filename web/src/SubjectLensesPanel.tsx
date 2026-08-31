import { useMemo, useState } from 'react';
import { subjectLenses, type SubjectLensId } from './subject-lenses';
import { InfoTip } from './InfoTip';
import './subject-lenses.css';

type SubjectFilter = 'all' | SubjectLensId;

export function SubjectLensesPanel() {
  const [filter, setFilter] = useState<SubjectFilter>('all');
  const visible = useMemo(() => filter === 'all' ? subjectLenses : subjectLenses.filter((lens) => lens.id === filter), [filter]);

  return <section id="workspace-subjects" className="subject-lenses-shell" aria-labelledby="subject-lenses-title">
    <header>
      <div>
        <p>EXPLORE SUBJECTS</p>
        <h2 id="subject-lenses-title">Different subjects use different ways to think.</h2>
        <span>Choose a subject to see what people may do, make, ask, or check.</span>
      </div>
      <div className="subject-lenses-tools"><InfoTip label="What is a subject guide?">This is planning help, not a full curriculum. A teacher still checks the right official source for the learner's place and age.</InfoTip><label>Choose a subject<select value={filter} onChange={(event) => setFilter(event.target.value as SubjectFilter)}><option value="all">All {subjectLenses.length} subjects</option>{subjectLenses.map((lens) => <option key={lens.id} value={lens.id}>{lens.label}</option>)}</select></label></div>
    </header>

    <p className="subject-lenses-count" role="status">Showing {visible.length} subject {visible.length === 1 ? 'lens' : 'lenses'}.</p>

    <div className="subject-lenses-grid">
      {visible.map((lens) => <article key={lens.id}>
        <div className="subject-lens-heading"><div><p>{lens.id.toUpperCase()}</p><h3>{lens.label}</h3></div><code>{lens.id}</code></div>
        <b className="subject-promise">{lens.promise}</b>
        <details><summary>Open teacher planning notes</summary><div className="subject-lens-columns"><section><h4>Ways to think</h4><ul>{lens.disciplinaryHabits.map((item) => <li key={item}>{item}</li>)}</ul></section><section><h4>Ways to show learning</h4><ul>{lens.evidenceForms.map((item) => <li key={item}>{item}</li>)}</ul></section><section><h4>Good planning questions</h4><ul>{lens.planningQuestions.map((item) => <li key={item}>{item}</li>)}</ul></section></div><aside><h4>Be careful</h4><ul>{lens.cautions.map((item) => <li key={item}>{item}</li>)}</ul></aside></details>
      </article>)}
    </div>
  </section>;
}
