import { useCallback, useEffect, useMemo, useState } from 'react';
import './project-readiness.css';

type Gate = { id: string; area: string; capability: string; state: string; evidence: string; nextEvidence: string; ownerBoundary: string };
type Board = { ok: boolean; databaseAuthority: string; schemaVersion: number; productVersion: string; completionState: string; gates: Gate[]; rules: string[] };
type ReadinessStatus = 'loading' | 'ready' | 'error';

function boundedReadinessError(value: unknown) {
  const message = value instanceof Error ? value.message : 'The readiness database could not be loaded.';
  return message.replace(/\s+/g, ' ').trim().slice(0, 220);
}

export function ProjectReadinessPanel() {
  const [board, setBoard] = useState<Board | null>(null);
  const [expanded, setExpanded] = useState(false);
  const [status, setStatus] = useState<ReadinessStatus>('loading');
  const [error, setError] = useState('');
  const loadBoard = useCallback(async () => {
    setStatus('loading');
    setError('');
    try {
      const response = await fetch('/api/project/readiness', { headers: { Accept: 'application/json' } });
      const payload = await response.json() as Board & { error?: string };
      if (!response.ok || !payload.ok) throw new Error(payload.error || `Readiness returned HTTP ${response.status}`);
      setBoard(payload);
      setStatus('ready');
    } catch (value) {
      setBoard(null);
      setStatus('error');
      setError(boundedReadinessError(value));
    }
  }, []);
  useEffect(() => { void loadBoard(); }, [loadBoard]);
  const counts = useMemo(() => {
    const result: Record<string, number> = {};
    for (const gate of board?.gates ?? []) result[gate.state] = (result[gate.state] ?? 0) + 1;
    return result;
  }, [board]);
  return <section id="workspace-readiness" className="readiness-board">
    <button className="readiness-summary" type="button" onClick={() => setExpanded(value => !value)} aria-expanded={expanded}>
      <span><strong>MA-TEACHER {board ? board.productVersion : status === 'loading' ? 'LOADING' : 'VERSION UNAVAILABLE'} · CONTINUATION BOARD</strong><small>{board ? `${board.gates.length} explicit capability gates` : status === 'loading' ? 'loading readiness once' : 'readiness database unavailable'}</small></span>
      <span className="readiness-counts">{Object.entries(counts).map(([state, count]) => <em key={state}>{count} {state}</em>)}</span>
      <b>{expanded ? 'CLOSE' : 'OPEN'}</b>
    </button>
    {expanded && <div className="readiness-detail">{board ? <>
        <div className="readiness-warning"><strong>PROJECT STATE: {board.completionState}</strong> Source exists, but current work has not been built, launched, packaged or runtime-proven.</div>
        <div className="readiness-warning" aria-label="Readiness authority and governing rules"><strong>AUTHORITY: {board.databaseAuthority} / SCHEMA {board.schemaVersion}</strong><ul>{board.rules.map((rule) => <li key={rule}>{rule}</li>)}</ul></div>
        <div className="readiness-gates">{board.gates.map(gate => <article key={gate.id}>
          <header><span>{gate.area}</span><strong>{gate.state}</strong></header><h3>{gate.capability}</h3>
          <p><b>Evidence now</b>{gate.evidence}</p><p><b>Next proof</b>{gate.nextEvidence}</p><footer>{gate.ownerBoundary}</footer>
        </article>)}</div>
      </> : <div className="readiness-warning" role={status === 'error' ? 'alert' : 'status'}>
        <strong>{status === 'loading' ? 'READINESS LOADING' : 'READINESS UNAVAILABLE'}</strong>
        {status === 'loading' ? 'One bounded request is in progress. No polling is running.' : error || 'No readiness response is available.'}
        <button type="button" onClick={() => void loadBoard()} disabled={status === 'loading'}>{status === 'loading' ? 'Loading...' : 'Retry readiness'}</button>
      </div>}</div>}
  </section>;
}
