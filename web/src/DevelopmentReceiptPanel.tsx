import { useState } from 'react';
import { evidenceStates, type EvidenceStateId } from './evidence-status';
import './development-receipt.css';

const verificationStates = ['not-run', 'observed', 'accepted', 'failed', 'not-applicable'] as const;

type ReceiptDraft = {
  id: string;
  recordedUtc: string;
  actor: string;
  workstream: string;
  behavior: string;
  reference: string;
  evidenceState: EvidenceStateId;
  evidenceDetail: string;
  verificationState: typeof verificationStates[number];
  verificationDetail: string;
  crewActivity: string;
  crewResponse: string;
  externalAssistantUsed: boolean;
  externalAutomationUsed: boolean;
};

type ReceiptMutation = {
  ok: boolean;
  state: string;
  inserted: boolean;
  record?: { id: string; contentSha256: string; integrityValid: boolean } | null;
  error?: string | null;
};

const initialDraft: ReceiptDraft = {
  id: '',
  recordedUtc: '',
  actor: '',
  workstream: '',
  behavior: '',
  reference: '',
  evidenceState: 'source-present',
  evidenceDetail: 'Source changed; build, tests, runtime and human review were not run.',
  verificationState: 'not-run',
  verificationDetail: 'No verification was performed for this receipt.',
  crewActivity: 'none',
  crewResponse: 'none',
  externalAssistantUsed: false,
  externalAutomationUsed: false,
};

function currentUtcSecond() {
  return new Date().toISOString().replace(/\.\d{3}Z$/, 'Z');
}

export function DevelopmentReceiptPanel() {
  const [draft, setDraft] = useState<ReceiptDraft>(initialDraft);
  const [confirmation, setConfirmation] = useState('');
  const [state, setState] = useState<'idle' | 'submitting' | 'result'>('idle');
  const [result, setResult] = useState<ReceiptMutation | null>(null);

  function update<K extends keyof ReceiptDraft>(key: K, value: ReceiptDraft[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
    setConfirmation('');
    setResult(null);
    setState('idle');
  }

  async function appendReceipt(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (confirmation !== 'APPEND' || state === 'submitting') return;
    setState('submitting');
    setResult(null);
    try {
      const response = await fetch('/api/development/breadcrumbs', {
        method: 'POST',
        cache: 'no-store',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          'X-MA-Teacher-Intent': 'append-development-breadcrumb',
        },
        body: JSON.stringify(draft),
      });
      let body: ReceiptMutation;
      try {
        body = await response.json() as ReceiptMutation;
      } catch {
        body = { ok: false, state: 'failed', inserted: false, error: `Receipt endpoint returned HTTP ${response.status} without valid JSON.` };
      }
      setResult(body);
      setState('result');
      if (body.ok) setConfirmation('');
    } catch (caught) {
      setResult({
        ok: false,
        state: 'failed',
        inserted: false,
        error: caught instanceof Error ? caught.message : 'Canonical receipt request failed.',
      });
      setState('result');
    }
  }

  return (
    <section id="workspace-development-receipt" className="development-receipt-panel" aria-labelledby="development-receipt-title">
      <header>
        <p className="development-receipt-kicker">Immutable database receipt</p>
        <h2 id="development-receipt-title">Append a truthful development breadcrumb</h2>
        <p>Review every field. Existing IDs cannot be edited, and conflicting content must fail rather than overwrite history.</p>
      </header>

      <form onSubmit={appendReceipt}>
        <div className="development-receipt-grid">
          <label>Breadcrumb ID
            <input required maxLength={160} pattern="[a-z0-9._-]+" value={draft.id} onChange={(event) => update('id', event.target.value)} placeholder="ma-teacher-161-short-description" />
          </label>
          <label>Recorded UTC
            <span className="development-receipt-inline"><input required value={draft.recordedUtc} onChange={(event) => update('recordedUtc', event.target.value)} placeholder="2026-08-30T12:34:56Z" /><button type="button" onClick={() => update('recordedUtc', currentUtcSecond())}>Use current UTC</button></span>
          </label>
          <label>Actor
            <input required maxLength={120} value={draft.actor} onChange={(event) => update('actor', event.target.value)} placeholder="Exact worker identity" />
          </label>
          <label>Workstream
            <input required maxLength={200} value={draft.workstream} onChange={(event) => update('workstream', event.target.value)} placeholder="Exact bounded workstream" />
          </label>
        </div>

        <label>Behavior changed
          <textarea required maxLength={2000} rows={4} value={draft.behavior} onChange={(event) => update('behavior', event.target.value)} />
        </label>
        <label>Source reference
          <textarea required maxLength={2000} rows={3} value={draft.reference} onChange={(event) => update('reference', event.target.value)} placeholder="Repository-owned files or canonical record IDs" />
        </label>

        <div className="development-receipt-grid">
          <label>Evidence state
            <select value={draft.evidenceState} onChange={(event) => update('evidenceState', event.target.value as ReceiptDraft['evidenceState'])}>
              {evidenceStates.map((item) => <option key={item.id} value={item.id}>{item.id} - {item.label}</option>)}
            </select>
          </label>
          <label>Verification state
            <select value={draft.verificationState} onChange={(event) => update('verificationState', event.target.value as ReceiptDraft['verificationState'])}>
              {verificationStates.map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </label>
        </div>

        <label>Evidence detail
          <textarea required maxLength={4000} rows={3} value={draft.evidenceDetail} onChange={(event) => update('evidenceDetail', event.target.value)} />
        </label>
        <label>Verification detail
          <textarea required maxLength={4000} rows={3} value={draft.verificationDetail} onChange={(event) => update('verificationDetail', event.target.value)} />
        </label>

        <div className="development-receipt-grid">
          <label>Crew activity
            <textarea required maxLength={1000} rows={2} value={draft.crewActivity} onChange={(event) => update('crewActivity', event.target.value)} />
          </label>
          <label>Crew response
            <textarea required maxLength={2000} rows={2} value={draft.crewResponse} onChange={(event) => update('crewResponse', event.target.value)} />
          </label>
        </div>

        <fieldset>
          <legend>External system use</legend>
          <label><input type="checkbox" checked={draft.externalAssistantUsed} onChange={(event) => update('externalAssistantUsed', event.target.checked)} /> external assistant was used</label>
          <label><input type="checkbox" checked={draft.externalAutomationUsed} onChange={(event) => update('externalAutomationUsed', event.target.checked)} /> external automation was used</label>
        </fieldset>

        <div className="development-receipt-confirmation">
          <label>Type <strong>APPEND</strong> to confirm immutable insertion
            <input value={confirmation} onChange={(event) => setConfirmation(event.target.value)} autoComplete="off" />
          </label>
          <button type="submit" disabled={confirmation !== 'APPEND' || state === 'submitting'}>
            {state === 'submitting' ? 'Appending…' : 'Append canonical breadcrumb'}
          </button>
        </div>
      </form>

      {result ? (
        <div className={`development-receipt-result ${result.ok ? 'is-success' : 'is-failure'}`} role="status">
          <strong>{result.state.toUpperCase()}</strong>
          <span>{result.ok ? `Canonical ID: ${result.record?.id ?? draft.id}` : result.error || 'Receipt was not accepted.'}</span>
          {result.record?.contentSha256 ? <code>{result.record.contentSha256}</code> : null}
          {result.record ? <span>Integrity read-back: {result.record.integrityValid ? 'valid' : 'invalid'}</span> : null}
        </div>
      ) : null}

      <p className="development-receipt-boundary" role="note">
        Draft fields live only in this page. A receipt becomes canonical only after an accepted API response. This surface cannot edit, delete, complete, verify, or change project readiness.
      </p>
    </section>
  );
}
