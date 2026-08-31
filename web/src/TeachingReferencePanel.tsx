import { useEffect, useMemo, useState } from 'react';
import './teaching-references.css';

type Source = { id: string; publisher: string; title: string; authorityClass: string; sourceUrl: string; publishedDate: string; editorialSnapshotDate: string; scope: string; useBoundary: string; reviewState: string };
type Principle = { id: string; sourceId: string; category: string; title: string; summary: string; applicability: string; caution: string; sourceLocator: string; evidenceState: string };
type ReferenceResponse = { ok: boolean; databaseAuthority: string; schemaVersion: number; editorialSnapshotDate: string; sources: Source[]; principles: Principle[]; boundaries: string[] };

export function TeachingReferencePanel() {
  const [data, setData] = useState<ReferenceResponse | null>(null);
  const [category, setCategory] = useState('all');
  const [state, setState] = useState('Loading teaching references...');
  useEffect(() => { fetch('/api/teaching/references').then(async response => {
    const payload = await response.json();
    if (!response.ok || !payload.ok) throw new Error(payload.error || `HTTP ${response.status}`);
    setData(payload); setState('Reference registry loaded from install-root SQLite');
  }).catch(error => setState(`Reference registry unavailable: ${error instanceof Error ? error.message : 'unknown error'}`)); }, []);
  const categories = useMemo(() => ['all', ...Array.from(new Set((data?.principles ?? []).map(value => value.category)))], [data]);
  const visible = (data?.principles ?? []).filter(value => category === 'all' || value.category === category);
  const sourceById = new Map((data?.sources ?? []).map(source => [source.id, source]));
  return <section id="workspace-references" className="teaching-reference-shell" aria-labelledby="teaching-reference-title">
    <header><div><p>TEACHING REFERENCE LIBRARY</p><h2 id="teaching-reference-title">Evidence informs judgement. It does not replace it.</h2></div><span>{data?.editorialSnapshotDate ?? 'unloaded'} snapshot</span></header>
    <div className="reference-boundary"><strong>Boundary</strong> These are short source-linked editorial summaries for operator review. They are not accepted curriculum statements, automatic prescriptions, or proof of learner impact.</div>
    <nav aria-label="Teaching reference categories">{categories.map(value => <button key={value} className={category === value ? 'active' : ''} onClick={() => setCategory(value)}>{value}</button>)}</nav>
    <div className="reference-card-grid">{visible.map(principle => { const source = sourceById.get(principle.sourceId); return <article key={principle.id}>
      <div className="reference-card-meta"><span>{principle.category}</span><span>{principle.evidenceState}</span></div><h3>{principle.title}</h3><p>{principle.summary}</p>
      <dl><dt>Apply thoughtfully</dt><dd>{principle.applicability}</dd><dt>Do not infer</dt><dd>{principle.caution}</dd></dl>
      {source && <footer><a href={source.sourceUrl} target="_blank" rel="noreferrer">{source.publisher}: {source.title}</a><span>{principle.sourceLocator} · {source.reviewState}</span></footer>}
    </article>; })}</div><output>{state}</output>
  </section>;
}
