import { useMemo, useState } from 'react';
import { assessmentGroups, assessmentPatterns, type AssessmentGroup } from './assessment-patterns';
import './assessment-patterns.css';

function groupLabel(group: AssessmentGroup) {
  if (group === 'all') return 'All evidence types';
  return group.charAt(0).toUpperCase() + group.slice(1);
}

export function AssessmentPatternsPanel() {
  const [group, setGroup] = useState<AssessmentGroup>('all');
  const visible = useMemo(() => group === 'all' ? assessmentPatterns : assessmentPatterns.filter((pattern) => pattern.group === group), [group]);

  return <section id="workspace-assessment-design" className="assessment-patterns-shell" aria-labelledby="assessment-patterns-title">
    <header>
      <div>
        <p>MANUAL CHECK DESIGN · HUMAN REVIEW ONLY</p>
        <h2 id="assessment-patterns-title">Ask for evidence you can actually inspect.</h2>
        <span>Adapt a frame deliberately, then author the real check and criteria in the database-backed practice workspace.</span>
      </div>
      <label>Evidence type<select value={group} onChange={(event) => setGroup(event.target.value as AssessmentGroup)}>{assessmentGroups.map((item) => <option key={item} value={item}>{groupLabel(item)}</option>)}</select></label>
    </header>

    <p className="assessment-patterns-count" role="status">Showing {visible.length} of {assessmentPatterns.length} assessment patterns.</p>

    <div className="assessment-patterns-grid">
      {visible.map((pattern) => <article key={pattern.id}>
        <div className="assessment-pattern-heading"><div><p>{groupLabel(pattern.group).toUpperCase()}</p><h3>{pattern.title}</h3></div><code>{pattern.id}</code></div>
        <section><h4>Use when</h4><span>{pattern.useWhen}</span></section>
        <blockquote><b>Planning frame</b><span>{pattern.promptFrame}</span></blockquote>
        <div className="assessment-pattern-columns">
          <section><h4>Observable criteria</h4><ul>{pattern.criteria.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section><h4>Feedback questions</h4><ul>{pattern.feedbackQuestions.map((item) => <li key={item}>{item}</li>)}</ul></section>
        </div>
        <aside><b>Boundary</b><span>{pattern.caution}</span></aside>
      </article>)}
    </div>
  </section>;
}
