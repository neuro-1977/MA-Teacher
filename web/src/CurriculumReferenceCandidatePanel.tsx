import { useMemo, useState } from 'react';
import { curriculumReferenceCandidates, type CurriculumCandidateState, type CurriculumJurisdiction } from './curriculum-reference-candidates';
import { curriculumGlobalDriftWatch, curriculumSourceWatchByCandidateId, curriculumSourceWatchItems } from './curriculum-source-watchlist';
import './curriculum-reference-candidates.css';

const jurisdictions: Array<'All jurisdictions' | CurriculumJurisdiction> = ['All jurisdictions', 'England', 'Wales', 'Scotland', 'Northern Ireland'];
const states: Array<'All candidate states' | CurriculumCandidateState> = ['All candidate states', 'Pending governed review', 'Consultation only', 'Research incomplete'];

export function CurriculumReferenceCandidatePanel() {
  const [jurisdiction, setJurisdiction] = useState<(typeof jurisdictions)[number]>('All jurisdictions');
  const [state, setState] = useState<(typeof states)[number]>('All candidate states');
  const [query, setQuery] = useState('');
  const [watchOnly, setWatchOnly] = useState(false);

  const candidates = useMemo(() => {
    const needle = query.trim().toLocaleLowerCase();
    return curriculumReferenceCandidates.filter((candidate) => {
      const watch = curriculumSourceWatchByCandidateId.get(candidate.id);
      if (jurisdiction !== 'All jurisdictions' && candidate.jurisdiction !== jurisdiction) return false;
      if (state !== 'All candidate states' && candidate.state !== state) return false;
      if (watchOnly && !watch) return false;
      return !needle || [candidate.id, candidate.jurisdiction, candidate.title, candidate.publisher, candidate.scope, candidate.state, candidate.caution,
        watch?.id ?? '', watch?.label ?? '', watch?.state ?? '', watch?.trigger ?? '', watch?.preserve ?? '', watch?.forbidden ?? '', ...(watch?.evidence ?? [])]
        .some((value) => value.toLocaleLowerCase().includes(needle));
    });
  }, [jurisdiction, query, state, watchOnly]);

  return (
    <section className="curriculum-candidates" id="workspace-curriculum-reference-candidates" aria-labelledby="curriculum-candidates-title">
      <header className="curriculum-candidates__header">
        <div><p className="curriculum-candidates__eyebrow">Official-source intake / unaccepted</p><h2 id="curriculum-candidates-title">Curriculum reference candidates</h2><p>Review jurisdiction, status and non-equivalence cautions before opening an official source or creating a governed reference.</p></div>
        <span aria-live="polite">{candidates.length} of {curriculumReferenceCandidates.length} / {curriculumSourceWatchItems.length} watches</span>
      </header>
      <div className="curriculum-candidates__boundary" role="note">Filtering and watch guidance do not fetch, copy, approve, supersede, schedule or persist curriculum. Every candidate remains outside canonical reference authority.</div>
      <div className="curriculum-candidates__filters" aria-label="Curriculum candidate filters">
        <label>Jurisdiction<select value={jurisdiction} onChange={(event) => setJurisdiction(event.target.value as (typeof jurisdictions)[number])}>{jurisdictions.map((value) => <option key={value}>{value}</option>)}</select></label>
        <label>Candidate state<select value={state} onChange={(event) => setState(event.target.value as (typeof states)[number])}>{states.map((value) => <option key={value}>{value}</option>)}</select></label>
        <label>Search<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="EYFS, adult, consultation..." /></label>
        <label className="curriculum-candidates__watch-toggle"><input type="checkbox" checked={watchOnly} onChange={(event) => setWatchOnly(event.target.checked)} />Watch triggers only</label>
      </div>
      <aside className="curriculum-watch curriculum-watch--global" aria-label="Global curriculum source drift watch">
        <div className="curriculum-watch__heading"><strong>{curriculumGlobalDriftWatch.id} / {curriculumGlobalDriftWatch.label}</strong><span>{curriculumGlobalDriftWatch.state}</span></div>
        <p><b>Trigger:</b> {curriculumGlobalDriftWatch.trigger}</p><div><b>Collect:</b><ul>{curriculumGlobalDriftWatch.evidence.map((item) => <li key={item}>{item}</li>)}</ul></div><p><b>Preserve:</b> {curriculumGlobalDriftWatch.preserve}</p><p><b>Never infer:</b> {curriculumGlobalDriftWatch.forbidden}</p>
      </aside>
      {candidates.length === 0 ? <p className="curriculum-candidates__empty">No candidates match. Change the filters rather than inventing a jurisdiction mapping.</p> : (
        <div className="curriculum-candidates__grid">{candidates.map((candidate) => (
          <article className="curriculum-candidate" key={candidate.id}>
            <div className="curriculum-candidate__meta"><span>{candidate.jurisdiction}</span><strong data-state={candidate.state}>{candidate.state}</strong></div>
            <h3>{candidate.title}</h3><p className="curriculum-candidate__publisher">{candidate.publisher}</p>
            <dl><div><dt>Declared scope</dt><dd>{candidate.scope}</dd></div><div className="curriculum-candidate__caution"><dt>Intake caution</dt><dd>{candidate.caution}</dd></div><div><dt>Research receipt</dt><dd>{candidate.id} / {candidate.researchedOn}</dd></div></dl>
            {curriculumSourceWatchByCandidateId.has(candidate.id) && (() => { const watch = curriculumSourceWatchByCandidateId.get(candidate.id)!; return <aside className="curriculum-watch" aria-label={`${watch.label} evidence watch`}><div className="curriculum-watch__heading"><strong>{watch.id}</strong><span>{watch.state}</span></div><p><b>Trigger:</b> {watch.trigger}</p><div><b>Collect:</b><ul>{watch.evidence.map((item) => <li key={item}>{item}</li>)}</ul></div><p><b>Preserve:</b> {watch.preserve}</p><p><b>Never infer:</b> {watch.forbidden}</p></aside>; })()}
            <a href={candidate.url} target="_blank" rel="noreferrer noopener">Open official source <span aria-hidden="true">-&gt;</span></a>
          </article>
        ))}</div>
      )}
    </section>
  );
}
