import { useEffect, useState } from 'react';
import tutorIcon from '../../icon-large.png';

type CurriculumStage = { id: string; ages: string; years: string };
type CurriculumSource = {
  id: string;
  subject: string;
  title: string;
  stageScope: string;
  sourceUrl: string;
  status: string;
  scopeNote: string;
  latestFetchedUtc: string | null;
  latestSha256: string | null;
  latestBodyBytes: number | null;
};
type CurriculumOverview = {
  boundary: string;
  stages: CurriculumStage[];
  sources: CurriculumSource[];
  subjectLanes: Array<{ id: string; subject: string; stageScope: string; sourceId: string; teachingFocus: string; evidenceState: string }>;
  implementationGates: Array<{ id: string; sequence: number; title: string; requiredEvidence: string; status: string }>;
};

export function App() {
  const [overview, setOverview] = useState<CurriculumOverview | null>(null);
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'unavailable'>('loading');
  const [refreshState, setRefreshState] = useState<'idle' | 'working' | 'complete' | 'failed'>('idle');

  const loadOverview = (signal?: AbortSignal) => fetch('/api/curriculum/overview', { signal })
    .then((response) => {
      if (!response.ok) throw new Error(`Curriculum API returned ${response.status}`);
      return response.json() as Promise<CurriculumOverview>;
    })
    .then((value) => { setOverview(value); setLoadState('ready'); });

  useEffect(() => {
    const controller = new AbortController();
    loadOverview(controller.signal)
      .catch((error: unknown) => {
        if ((error as Error).name !== 'AbortError') setLoadState('unavailable');
      });
    return () => controller.abort();
  }, []);

  const refreshSources = async () => {
    if (refreshState === 'working') return;
    setRefreshState('working');
    try {
      const response = await fetch('/api/curriculum/refresh', {
        method: 'POST',
        headers: { 'X-MA-Teacher-Intent': 'refresh-curriculum-sources' },
      });
      if (!response.ok) throw new Error(`Source capture returned ${response.status}`);
      await loadOverview();
      setRefreshState('complete');
    } catch {
      setRefreshState('failed');
    }
  };

  return (
    <main className="shell">
      <header className="masthead">
        <div><span className="eyebrow">MA-TEACHER / EVIDENCE CONSOLE</span><h1>Curriculum <em>0.1.0</em></h1></div>
        <span className={`status status-${loadState}`}>{loadState === 'ready' ? 'DATABASE ONLINE' : loadState.toUpperCase()}</span>
      </header>

      <section className="hero" aria-labelledby="purpose-heading">
        <img className="tutor-icon" src={tutorIcon} alt="MA-Teacher potato-shaped tutor wearing a graduation cap" />
        <div>
          <p className="eyebrow">ALL AGES / EVIDENCE BEFORE ANSWERS</p>
          <h2 id="purpose-heading">Know what is official. Know what is interpretation. Never bluff the gap.</h2>
          <p>MA-Teacher now owns a local curriculum evidence database. It registers official sources and their scope before any objective, lesson, assessment, or tutor response can claim authority.</p>
        </div>
      </section>

      <section className="stage-strip" aria-label="English curriculum key stages">
        {(overview?.stages ?? []).map((stage) => <article key={stage.id}><b>{stage.id}</b><span>{stage.ages}</span><small>{stage.years}</small></article>)}
        {loadState !== 'ready' && <p>Curriculum stage evidence is unavailable until the local database API responds.</p>}
      </section>

      <section className="evidence" aria-labelledby="evidence-heading">
        <div className="section-heading">
          <div><p className="eyebrow">REGISTERED OFFICIAL SOURCES</p><h2 id="evidence-heading">Provenance map</h2></div>
          <div className="capture-controls"><span>{overview ? `${overview.sources.length} records` : 'No evidence loaded'}</span><button type="button" disabled={loadState !== 'ready' || refreshState === 'working'} onClick={refreshSources}>{refreshState === 'working' ? 'Capturing...' : 'Capture official sources'}</button></div>
        </div>
        {overview && <p className="boundary">{overview.boundary}</p>}
        <div className="source-grid">
          {(overview?.sources ?? []).map((source) => (
            <article className="source-card" key={source.id}>
              <div className="source-meta"><span>{source.subject}</span><b>{source.stageScope}</b></div>
              <h3>{source.title}</h3>
              <p>{source.scopeNote}</p>
              {source.latestSha256 && <code title={source.latestSha256}>SHA-256 {source.latestSha256.slice(0, 16)} / {source.latestBodyBytes?.toLocaleString()} bytes</code>}
              <div className="source-foot"><span>{source.status.replaceAll('-', ' ')}</span><a href={source.sourceUrl} target="_blank" rel="noreferrer">GOV.UK source</a></div>
            </article>
          ))}
        </div>
        {refreshState === 'failed' && <p className="capture-result capture-failed">Capture failed or returned incomplete evidence. Existing revisions were preserved.</p>}
        {refreshState === 'complete' && <p className="capture-result">Capture completed. Identical source hashes were retained as one revision.</p>}
      </section>

      <section className="subjects" aria-label="Database-owned subject lanes">
        {(overview?.subjectLanes ?? []).map((lane, index) => <article key={lane.id}><div><b>{String(index + 1).padStart(2, '0')}</b><span>{lane.stageScope}</span></div><strong>{lane.subject}</strong><small>{lane.teachingFocus}</small><em>{lane.evidenceState.replaceAll('_', ' ')}</em></article>)}
      </section>

      <section className="gates" aria-labelledby="gates-heading">
        <div><p className="eyebrow">NEXT PROOF GATES</p><h2 id="gates-heading">Registered is not taught</h2></div>
        <ol>{(overview?.implementationGates ?? []).map((gate) => <li key={gate.id}><strong>{gate.title}</strong><span>{gate.requiredEvidence}</span><em>{gate.status.replaceAll('_', ' ')}</em></li>)}</ol>
      </section>
      <footer>PRIVATE / ONE-FOLDER DATA / LOOPBACK 5201 / CAPTAINNEURO</footer>
    </main>
  );
}
