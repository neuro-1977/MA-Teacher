import { useEffect, useMemo, useState } from 'react';
import './curriculum-coverage.css';

type Lane = { id: string; jurisdiction: string; ageScope: string; stageModel: string; subjectScope: string; sourceTitle: string; sourceUrl: string; sourceKind: string; coverageState: string; evidence: string; gap: string; nextAction: string };
type Overview = { ok: boolean; overallState: string; editorialSnapshotDate: string; lanes: Lane[]; rules: string[] };

export function CurriculumCoveragePanel() {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [jurisdiction, setJurisdiction] = useState('all');
  useEffect(() => { fetch('/api/curriculum/coverage').then(async response => {
    const payload = await response.json(); if (!response.ok || !payload.ok) throw new Error(payload.error || `HTTP ${response.status}`); setOverview(payload);
  }).catch(() => setOverview(null)); }, []);
  const jurisdictions = useMemo(() => ['all', ...Array.from(new Set((overview?.lanes ?? []).map(lane => lane.jurisdiction)))], [overview]);
  const lanes = (overview?.lanes ?? []).filter(lane => jurisdiction === 'all' || lane.jurisdiction === jurisdiction);
  return <section id="workspace-coverage" className="coverage-shell" aria-labelledby="coverage-title">
    <header><div><p>CURRICULUM COVERAGE, NOT WISHFUL THINKING</p><h2 id="coverage-title">Every age and jurisdiction needs its own authority.</h2></div><span>{overview?.overallState ?? 'coverage registry unavailable'}</span></header>
    <nav>{jurisdictions.map(value => <button key={value} className={value === jurisdiction ? 'active' : ''} onClick={() => setJurisdiction(value)}>{value}</button>)}</nav>
    <div className="coverage-grid">{lanes.map(lane => <article key={lane.id}>
      <header><span>{lane.ageScope}</span><strong>{lane.coverageState}</strong></header><h3>{lane.jurisdiction} · {lane.stageModel}</h3><p className="coverage-subjects">{lane.subjectScope}</p>
      <dl><dt>Evidence</dt><dd>{lane.evidence}</dd><dt>Gap</dt><dd>{lane.gap}</dd><dt>Next</dt><dd>{lane.nextAction}</dd></dl>
      <footer>{lane.sourceUrl ? <a href={lane.sourceUrl} target="_blank" rel="noreferrer">{lane.sourceTitle}</a> : lane.sourceTitle}<span>{lane.sourceKind}</span></footer>
    </article>)}</div>
  </section>;
}
