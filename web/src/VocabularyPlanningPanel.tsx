import { useMemo, useState } from 'react';
import { vocabularyEntries } from './vocabulary-planning-data';
import './vocabulary-planning.css';

export function VocabularyPlanningPanel() {
  const [subject, setSubject] = useState('all'); const [stage, setStage] = useState('all'); const [query, setQuery] = useState('');
  const subjects = useMemo(() => Array.from(new Set(vocabularyEntries.map(value => value.subject))).sort(), []);
  const stages = ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'];
  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return vocabularyEntries.filter(value => (subject === 'all' || value.subject === subject) && (stage === 'all' || value.stages.includes(stage as never))
      && (!needle || [value.term, value.subject, value.learnerMeaning, value.disciplinaryPrecision, value.modelUse, value.retrievalPrompt].some(text => text.toLowerCase().includes(needle))));
  }, [query, stage, subject]);
  return <section id="workspace-vocabulary-planning" className="vocabulary-planning" aria-labelledby="vocabulary-planning-title"><header><div><p>DISCIPLINARY VOCABULARY · STATIC GUIDANCE</p><h2 id="vocabulary-planning-title">Teach the word, the distinction, and how the subject uses it.</h2><span>These original entries support planning. They are not accepted curriculum, a learner vocabulary profile, or evidence that a term was taught.</span></div></header>
    <div className="vocabulary-planning-boundary"><strong>Meaning before performance</strong><span>Stage is a planning lens. A learner needing an earlier explanation does not authorize an ability, language, disability, or attainment inference.</span></div>
    <div className="vocabulary-planning-filters"><label>Subject<select value={subject} onChange={event => setSubject(event.target.value)}><option value="all">All subjects</option>{subjects.map(value => <option key={value}>{value}</option>)}</select></label><label>Stage lens<select value={stage} onChange={event => setStage(event.target.value)}><option value="all">All stages</option>{stages.map(value => <option key={value}>{value}</option>)}</select></label><label>Search<input type="search" value={query} onChange={event => setQuery(event.target.value)} placeholder="Term, meaning, model or prompt" /></label></div>
    <p className="vocabulary-planning-count" role="status">Showing {visible.length} of {vocabularyEntries.length} original vocabulary entries.</p>
    <div className="vocabulary-planning-grid">{visible.length === 0 ? <p>No vocabulary entry matches this filter.</p> : visible.map(value => <article key={value.id}><header><div><p>{value.subject.toUpperCase()} · {value.stageLabel.toUpperCase()}</p><h3>{value.term}</h3></div><code>{value.id}</code></header><section><h4>Learner-facing meaning</h4><p>{value.learnerMeaning}</p></section><section><h4>Disciplinary precision</h4><p>{value.disciplinaryPrecision}</p></section><section className="vocabulary-model"><h4>Model use</h4><p>{value.modelUse}</p></section><section className="vocabulary-nonexample"><h4>Non-example</h4><p>{value.nonExample}</p></section><footer><div><b>Retrieval cue</b><span>{value.retrievalPrompt}</span></div><div><b>Caution</b><span>{value.caution}</span></div></footer></article>)}</div>
  </section>;
}
