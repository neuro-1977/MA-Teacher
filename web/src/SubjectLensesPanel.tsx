import { useMemo, useState } from 'react';
import { subjectLenses, type SubjectLensId } from './subject-lenses';
import './subject-lenses.css';

type SubjectFilter = 'all' | SubjectLensId;

export function SubjectLensesPanel() {
  const [filter, setFilter] = useState<SubjectFilter>('all');
  const visible = useMemo(() => filter === 'all' ? subjectLenses : subjectLenses.filter((lens) => lens.id === filter), [filter]);

  return <section id="workspace-subjects" className="subject-lenses-shell" aria-labelledby="subject-lenses-title">
    <header>
      <div>
        <p>DISCIPLINARY THINKING · REVIEW THE CURRICULUM SEPARATELY</p>
        <h2 id="subject-lenses-title">Plan for what the subject asks learners to do.</h2>
        <span>These lenses shape author decisions. They are not curriculum statements or automatic lesson recommendations.</span>
      </div>
      <label>Subject lens<select value={filter} onChange={(event) => setFilter(event.target.value as SubjectFilter)}><option value="all">All six subjects</option>{subjectLenses.map((lens) => <option key={lens.id} value={lens.id}>{lens.label}</option>)}</select></label>
    </header>

    <p className="subject-lenses-count" role="status">Showing {visible.length} subject {visible.length === 1 ? 'lens' : 'lenses'}.</p>

    <div className="subject-lenses-grid">
      {visible.map((lens) => <article key={lens.id}>
        <div className="subject-lens-heading"><div><p>{lens.id.toUpperCase()}</p><h3>{lens.label}</h3></div><code>{lens.id}</code></div>
        <b className="subject-promise">{lens.promise}</b>
        <div className="subject-lens-columns">
          <section><h4>Disciplinary habits</h4><ul>{lens.disciplinaryHabits.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section><h4>Useful evidence forms</h4><ul>{lens.evidenceForms.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section><h4>Planning questions</h4><ul>{lens.planningQuestions.map((item) => <li key={item}>{item}</li>)}</ul></section>
        </div>
        <aside><h4>Do not overclaim</h4><ul>{lens.cautions.map((item) => <li key={item}>{item}</li>)}</ul></aside>
      </article>)}
    </div>
  </section>;
}
