import { useState } from 'react';
import './continuation-snapshot.css';

type SnapshotStatus = 'idle' | 'loading' | 'ready' | 'error';

async function readJsonEndpoint(endpoint: string) {
  const response = await fetch(endpoint, { headers: { Accept: 'application/json' } });
  if (!response.ok) throw new Error(`${endpoint} returned HTTP ${response.status}`);
  return response.json() as Promise<unknown>;
}

function boundedError(value: unknown) {
  const message = value instanceof Error ? value.message : 'The continuation snapshot could not be loaded.';
  return message.replace(/\s+/g, ' ').trim().slice(0, 220);
}

export function ContinuationSnapshotPanel() {
  const [status, setStatus] = useState<SnapshotStatus>('idle');
  const [snapshot, setSnapshot] = useState('');
  const [message, setMessage] = useState('Nothing has been loaded. The database-backed APIs remain authoritative.');

  const loadSnapshot = async () => {
    setStatus('loading');
    setSnapshot('');
    setMessage('Reading readiness, curriculum coverage, and canonical development context once. No automatic polling is running.');
    try {
      const [readiness, coverage, developmentHistory] = await Promise.all([
        readJsonEndpoint('/api/project/readiness'),
        readJsonEndpoint('/api/curriculum/coverage'),
        readJsonEndpoint('/api/development/breadcrumbs?limit=200&issueLimit=20'),
      ]);
      const payload = JSON.stringify({
        snapshotType: 'ma-teacher-continuation',
        assembledInBrowserAtUtc: new Date().toISOString(),
        clockAuthority: 'browser clock; not a database, service, source-retrieval, or canonical receipt timestamp',
        authority: 'database-backed application APIs',
        completionClaim: 'none',
        actorBoundary: 'snapshot only; canonical receipt actors are included without inferring omitted history',
        readiness,
        curriculumCoverage: coverage,
        developmentHistory,
      }, null, 2);
      setSnapshot(payload);
      setStatus('ready');
      setMessage('Snapshot loaded in browser memory. It has not been written to disk or recorded as a new receipt.');
    } catch (error) {
      setStatus('error');
      setMessage(boundedError(error));
    }
  };

  const copySnapshot = async () => {
    if (!snapshot) return;
    try {
      await navigator.clipboard.writeText(snapshot);
      setMessage('Copied to the system clipboard. The clipboard copy is not canonical and may outlive this view.');
    } catch (error) {
      setMessage(`Clipboard copy failed: ${boundedError(error)}`);
    }
  };

  const clearSnapshot = () => {
    setSnapshot('');
    setStatus('idle');
    setMessage('In-memory snapshot cleared. No database record was changed.');
  };

  return <section id="workspace-continuation" className="continuation-shell" aria-labelledby="continuation-title">
    <header>
      <div>
        <p>RESUME FROM DATABASE TRUTH</p>
        <h2 id="continuation-title">Create a bounded continuation snapshot.</h2>
        <span>Readiness, coverage, and bounded canonical history. No learner records, lesson bodies, attempts, or reviews.</span>
      </div>
      <strong>{status.toUpperCase()}</strong>
    </header>

    <div className="continuation-actions">
      <button type="button" onClick={() => void loadSnapshot()} disabled={status === 'loading'}>{status === 'loading' ? 'Loading once...' : 'Load current snapshot'}</button>
      <button type="button" onClick={() => void copySnapshot()} disabled={!snapshot}>Copy snapshot</button>
      <button type="button" onClick={clearSnapshot} disabled={!snapshot}>Clear view</button>
    </div>

    <p className={`continuation-message ${status === 'error' ? 'is-error' : ''}`} role={status === 'error' ? 'alert' : 'status'}>{message}</p>

    {snapshot ? <pre tabIndex={0} aria-label="Current MA-Teacher continuation snapshot">{snapshot}</pre> : <div className="continuation-empty">Load manually when an operator or agent needs current database-backed continuation context.</div>}
  </section>;
}
