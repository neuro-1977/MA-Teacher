import { useMemo, useState } from 'react';
import { teachingPatternAuthority, teachingPatterns, teachingSubjects, type TeachingSubject } from './teaching-patterns';
import './teaching-patterns.css';

function labelSubject(subject: TeachingSubject) {
  if (subject === 'all') return 'All subjects';
  if (subject === 'cross-curricular') return 'Cross-curricular';
  return subject.charAt(0).toUpperCase() + subject.slice(1);
}

export function TeachingPatternsPanel() {
  const [subject, setSubject] = useState<TeachingSubject>('all');
  const visiblePatterns = useMemo(() => subject === 'all' ? teachingPatterns : teachingPatterns.filter((pattern) => pattern.subjects.includes(subject)), [subject]);

  return <section id="workspace-patterns" className="patterns-shell" aria-labelledby="patterns-title">
    <header>
      <div>
        <p>AUTHORING SCAFFOLDS · NOT CURRICULUM</p>
        <h2 id="patterns-title">Choose a teaching structure deliberately.</h2>
        <span>Use a pattern to plan. Link the resulting lesson to reviewed curriculum evidence separately.</span>
      </div>
      <label>Subject view<select value={subject} onChange={(event) => setSubject(event.target.value as TeachingSubject)}>{teachingSubjects.map((item) => <option key={item} value={item}>{labelSubject(item)}</option>)}</select></label>
    </header>

    <p className="patterns-count" role="note"><strong>{teachingPatternAuthority.evidenceState}</strong> · {teachingPatternAuthority.stageBoundary}</p>
    <p className="patterns-count" role="status">Showing {visiblePatterns.length} of {teachingPatterns.length} patterns.</p>

    <div className="patterns-grid">
      {visiblePatterns.map((pattern) => <article key={pattern.id}>
        <div className="pattern-heading"><div><p>{pattern.subjects.map(labelSubject).join(' · ')}</p><h3>{pattern.title}</h3></div><code>{pattern.id}</code></div>
        <b>{pattern.purpose}</b>
        <ol>{pattern.sequence.map((step) => <li key={step}>{step}</li>)}</ol>
        <details><summary>Adaptation prompts</summary><ul>{pattern.adaptationPrompts.map((prompt) => <li key={prompt}>{prompt}</li>)}</ul></details>
        <aside><b>Boundary</b><span>{pattern.caution}</span></aside>
      </article>)}
    </div>
  </section>;
}
