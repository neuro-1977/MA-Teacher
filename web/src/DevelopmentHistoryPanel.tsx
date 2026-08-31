import { useState } from 'react';
import './development-history.css';

type BreadcrumbRecord = {
  id: string;
  recordedUtc: string;
  actor: string;
  workstream: string;
  behavior: string;
  reference: string;
  evidenceState: string;
  evidenceDetail: string;
  verificationState: string;
  verificationDetail: string;
  crewActivity: string;
  crewResponse: string;
  externalAssistantUsed: boolean;
  externalAutomationUsed: boolean;
  contentSha256: string;
  integrityValid: boolean;
};

type BreadcrumbCursor = { recordedUtc: string; id: string };

type IntegrityAudit = {
  total: number;
  valid: number;
  missingIntegrity: number;
  mismatchedIntegrity: number;
  issueIds: string[];
  issuesTruncated: boolean;
};

type BreadcrumbContext = {
  requestedLimit: number;
  returnedCount: number;
  completeHistory: boolean;
  integrityClean: boolean;
  firstBreadcrumbId: string | null;
  lastBreadcrumbId: string | null;
  integrity: IntegrityAudit;
  records: BreadcrumbRecord[];
};

type ContextResponse = { ok: boolean; context?: BreadcrumbContext; error?: string };
type PageResponse = {
  ok: boolean;
  page?: {
    requestedLimit: number;
    returnedCount: number;
    hasMore: boolean;
    nextOlderCursor: BreadcrumbCursor | null;
    records: BreadcrumbRecord[];
  };
  error?: string;
};

function displayTime(value: string) {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString();
}

async function readJson<T>(response: Response): Promise<T> {
  try {
    return await response.json() as T;
  } catch {
    throw new Error(`Canonical history returned HTTP ${response.status} without valid JSON.`);
  }
}

function boundedHistoryError(value: unknown, fallback: string) {
  const message = value instanceof Error ? value.message : fallback;
  return message.replace(/\s+/g, ' ').trim().slice(0, 300);
}

export function DevelopmentHistoryPanel() {
  const [context, setContext] = useState<BreadcrumbContext | null>(null);
  const [records, setRecords] = useState<BreadcrumbRecord[]>([]);
  const [nextCursor, setNextCursor] = useState<BreadcrumbCursor | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [state, setState] = useState<'idle' | 'loading' | 'loaded' | 'error'>('idle');
  const [error, setError] = useState<string | null>(null);

  async function loadContext() {
    setState('loading');
    setError(null);
    try {
      const response = await fetch('/api/development/breadcrumbs?limit=100&issueLimit=20', {
        cache: 'no-store',
        headers: { Accept: 'application/json' },
      });
      const body = await readJson<ContextResponse>(response);
      if (!response.ok || !body.ok || !body.context) {
        throw new Error(body.error || `Canonical history request failed with HTTP ${response.status}.`);
      }
      const loaded = body.context;
      setContext(loaded);
      setRecords(loaded.records);
      setHasMore(!loaded.completeHistory && loaded.records.length > 0);
      setNextCursor(!loaded.completeHistory && loaded.records.length > 0
        ? { recordedUtc: loaded.records[0].recordedUtc, id: loaded.records[0].id }
        : null);
      setState('loaded');
    } catch (caught) {
      setState('error');
      setError(boundedHistoryError(caught, 'Canonical history could not be loaded.'));
    }
  }

  async function loadOlder() {
    if (!nextCursor || state === 'loading') return;
    setState('loading');
    setError(null);
    try {
      const query = new URLSearchParams({
        limit: '100',
        beforeUtc: nextCursor.recordedUtc,
        beforeId: nextCursor.id,
      });
      const response = await fetch(`/api/development/breadcrumbs/page?${query.toString()}`, {
        cache: 'no-store',
        headers: { Accept: 'application/json' },
      });
      const body = await readJson<PageResponse>(response);
      if (!response.ok || !body.ok || !body.page) {
        throw new Error(body.error || `Older-history request failed with HTTP ${response.status}.`);
      }
      setRecords((current) => {
        const known = new Set(current.map((record) => record.id));
        return [...body.page!.records.filter((record) => !known.has(record.id)), ...current];
      });
      setHasMore(body.page.hasMore);
      setNextCursor(body.page.nextOlderCursor);
      setState('loaded');
    } catch (caught) {
      setState('error');
      setError(boundedHistoryError(caught, 'Older canonical history could not be loaded.'));
    }
  }

  return (
    <section id="workspace-development-history" className="development-history-panel" aria-labelledby="development-history-title">
      <header>
        <div>
          <p className="development-history-kicker">Database-owned continuity</p>
          <h2 id="development-history-title">Canonical development history</h2>
          <p>Read integrity-bound breadcrumbs without opening SQLite, invoking a model, or changing project state.</p>
        </div>
        <button type="button" onClick={loadContext} disabled={state === 'loading'}>
          {state === 'loading' && records.length === 0 ? 'Loading…' : records.length > 0 ? 'Reload context' : 'Load canonical context'}
        </button>
      </header>

      {error ? <div className="development-history-error" role="alert"><span>{error}</span>{records.length > 0 ? <small>Previously loaded records remain visible. The failed request did not refresh or extend them; re-read canonical context before acting.</small> : null}</div> : null}

      {context ? (
        <div className={`development-history-integrity ${context.integrityClean ? 'is-clean' : 'has-issues'}`} role="status">
          <strong>{context.integrityClean ? 'INTEGRITY CLEAN' : 'INTEGRITY ISSUES'}</strong>
          <span>{context.integrity.valid} valid of {context.integrity.total}</span>
          <span>{context.integrity.missingIntegrity} missing hashes</span>
          <span>{context.integrity.mismatchedIntegrity} mismatches</span>
          <span>{records.length} records loaded</span>
          <span>{context.completeHistory && !hasMore ? 'complete bounded history' : 'older history available by explicit page'}</span>
        </div>
      ) : (
        <p className="development-history-empty">
          Context has not been requested. This panel does not auto-load, poll, seed, repair, or append records.
        </p>
      )}

      {context && !context.integrityClean && context.integrity.issueIds.length > 0 ? (
        <div className="development-history-issues" role="alert">
          <strong>Bounded issue IDs</strong>
          <span>{context.integrity.issueIds.join(', ')}</span>
          {context.integrity.issuesTruncated ? <em>Additional issue IDs were truncated by the API bound.</em> : null}
        </div>
      ) : null}

      {records.length > 0 ? (
        <div className="development-history-list">
          {[...records].reverse().map((record) => (
            <details key={record.id} className={record.integrityValid ? '' : 'has-integrity-failure'}>
              <summary>
                <span><strong>{record.id}</strong><small>{displayTime(record.recordedUtc)} · {record.actor}</small></span>
                <span>{record.evidenceState} · {record.verificationState}</span>
              </summary>
              <dl>
                <div><dt>Workstream</dt><dd>{record.workstream}</dd></div>
                <div><dt>Behavior</dt><dd>{record.behavior}</dd></div>
                <div><dt>Reference</dt><dd>{record.reference}</dd></div>
                <div><dt>Evidence</dt><dd><strong>{record.evidenceState}</strong> · {record.evidenceDetail}</dd></div>
                <div><dt>Verification</dt><dd><strong>{record.verificationState}</strong> · {record.verificationDetail}</dd></div>
                <div><dt>Crew activity</dt><dd>{record.crewActivity}</dd></div>
                <div><dt>Crew response</dt><dd>{record.crewResponse}</dd></div>
                <div><dt>external assistant used</dt><dd>{record.externalAssistantUsed ? 'true' : 'false'}</dd></div>
                <div><dt>external automation used</dt><dd>{record.externalAutomationUsed ? 'true' : 'false'}</dd></div>
                <div><dt>Integrity</dt><dd>{record.integrityValid ? 'valid' : 'invalid'} · {record.contentSha256 || 'missing hash'}</dd></div>
              </dl>
            </details>
          ))}
        </div>
      ) : null}

      {hasMore && nextCursor ? (
        <button type="button" className="development-history-older" onClick={loadOlder} disabled={state === 'loading'}>
          {state === 'loading' ? 'Loading older history…' : 'Load next older page'}
        </button>
      ) : null}

      <p className="development-history-boundary" role="note">
        This surface is read-only. A visible record is continuity evidence, not proof that its referenced behavior was built, run, accepted, or completed.
      </p>
    </section>
  );
}
