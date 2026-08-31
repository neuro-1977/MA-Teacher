import { FormEvent, useEffect, useMemo, useState } from 'react';
import './teaching-proposals.css';

type StudyPlan = { id: string; subject: string; learningStage: string; goal: string; status: string };
type Candidate = { id: string; subject: string; learningStage: string; statementText: string; reviewState: string };
type Proposal = { id: string; studyPlanId: string; subject: string; learningStage: string; proposalKind: string; producerKind: string; producerIdentity: string; recordedBy: string; content: string; rationale: string; limitations: string; status: string; createdUtc: string; evidenceCount: number; latestReviewId?: string; latestReviewerIdentity?: string; latestDecision?: string; latestReviewNote?: string; latestReviewedUtc?: string };

const proposalKinds = ['lesson-outline', 'explanation', 'worked-example', 'guided-practice', 'independent-practice', 'check-draft', 'differentiation', 'feedback-draft'];
const producerKinds = ['human', 'local-model', 'browser-assisted-agent', 'external-agent', 'imported'];
const decisions = ['accepted-for-editing', 'rejected', 'deferred'];

async function readJson<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const response = await fetch(endpoint, options); const payload = await response.json();
  if (!response.ok || payload.ok === false) throw new Error(payload.error || payload.state || `HTTP ${response.status}`);
  return payload as T;
}

export function TeachingProposalPanel() {
  const [plans, setPlans] = useState<StudyPlan[]>([]); const [candidates, setCandidates] = useState<Candidate[]>([]); const [proposals, setProposals] = useState<Proposal[]>([]);
  const [planId, setPlanId] = useState(''); const [id, setId] = useState(''); const [kind, setKind] = useState(proposalKinds[0]);
  const [producerKind, setProducerKind] = useState(producerKinds[0]); const [producerIdentity, setProducerIdentity] = useState(''); const [recordedBy, setRecordedBy] = useState('');
  const [content, setContent] = useState(''); const [rationale, setRationale] = useState(''); const [limitations, setLimitations] = useState(''); const [evidenceIds, setEvidenceIds] = useState<string[]>([]);
  const [confirmed, setConfirmed] = useState(false); const [selectedProposal, setSelectedProposal] = useState(''); const [reviewId, setReviewId] = useState('');
  const [reviewer, setReviewer] = useState(''); const [decision, setDecision] = useState(decisions[0]); const [reviewNote, setReviewNote] = useState('');
  const [reviewConfirmed, setReviewConfirmed] = useState(false);
  const [state, setState] = useState('Loading proposal evidence...'); const [busy, setBusy] = useState(false);

  async function refresh() {
    setConfirmed(false); setReviewConfirmed(false);
    try {
      const [workspace, candidateData, proposalData] = await Promise.all([
        readJson<{ studyPlans: StudyPlan[] }>('/api/teaching/workspace'),
        readJson<{ candidates: Candidate[] }>('/api/curriculum/candidates'),
        readJson<{ proposals: Proposal[] }>('/api/teaching/proposals'),
      ]);
      setPlans((workspace.studyPlans ?? []).filter(value => value.status === 'active'));
      setCandidates((candidateData.candidates ?? []).filter(value => value.reviewState === 'accepted'));
      setProposals(proposalData.proposals ?? []); setState('Current database state loaded. No model was called. Reconfirm any proposal or review after inspecting the refreshed evidence.');
    } catch (error) { setState(error instanceof Error ? error.message : 'Proposal evidence could not be loaded.'); }
  }
  useEffect(() => { void refresh(); }, []);
  const plan = plans.find(value => value.id === planId);
  const eligible = useMemo(() => candidates.filter(value => plan && value.subject === plan.subject && value.learningStage === plan.learningStage), [candidates, plan]);
  function updateProposalField(setter: (value: string) => void, value: string) { setConfirmed(false); setter(value); }
  function updateReviewField(setter: (value: string) => void, value: string) { setReviewConfirmed(false); setter(value); }
  function selectProposal(value: string) { setReviewConfirmed(false); setSelectedProposal(value); }
  function toggleEvidence(candidateId: string) { setConfirmed(false); setEvidenceIds(current => current.includes(candidateId) ? current.filter(value => value !== candidateId) : current.length < 12 ? [...current, candidateId] : current); }

  async function submitProposal(event: FormEvent) {
    event.preventDefault(); if (!confirmed) { setState('Confirm the proposal boundary before writing.'); return; }
    setBusy(true);
    try {
      const result = await readJson<{ state: string }>('/api/teaching/proposals', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'record-teaching-proposal' }, body: JSON.stringify({ id, studyPlanId: planId, proposalKind: kind, producerKind, producerIdentity, recordedBy, content, rationale, limitations, curriculumCandidateIds: evidenceIds }) });
      setState(result.state); setConfirmed(false); await refresh();
    } catch (error) { setState(error instanceof Error ? error.message : 'Proposal write failed.'); } finally { setBusy(false); }
  }

  async function submitReview(event: FormEvent) {
    event.preventDefault(); if (!reviewConfirmed) { setState('Confirm the immutable proposal-review boundary before writing.'); return; } setBusy(true);
    try {
      const result = await readJson<{ state: string }>('/api/teaching/proposal-reviews', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'review-teaching-proposal' }, body: JSON.stringify({ reviewId, proposalId: selectedProposal, reviewerIdentity: reviewer, decision, note: reviewNote }) });
      setState(result.state); setReviewConfirmed(false); await refresh();
    } catch (error) { setState(error instanceof Error ? error.message : 'Review write failed.'); } finally { setBusy(false); }
  }

  return <section id="workspace-proposals" className="teaching-proposals" aria-labelledby="teaching-proposals-title">
    <header><div><p>PROPOSALS · EVIDENCE LINKED · NEVER AUTO-APPLIED</p><h2 id="teaching-proposals-title">Let assistance propose. Keep teaching authority human.</h2></div><button type="button" onClick={() => void refresh()} disabled={busy}>Refresh</button></header>
    <div className="teaching-proposal-boundary"><strong>Hard boundary</strong><span>Accepted-for-editing means only that a person may deliberately edit or copy the material. It does not approve curriculum, change a lesson, score a learner, or prove the named producer or reviewer controlled this client.</span></div>
    <form className="teaching-proposal-form" onSubmit={submitProposal}>
      <h3>Record an unreviewed proposal</h3>
      <div className="teaching-proposal-fields"><label>Stable proposal ID<input required pattern="[a-z0-9_-]{3,64}" value={id} onChange={event => updateProposalField(setId, event.target.value)} placeholder="science-cells-explanation-01" /></label>
        <label>Active study plan<select required value={planId} onChange={event => { setConfirmed(false); setPlanId(event.target.value); setEvidenceIds([]); }}><option value="">Select a plan</option>{plans.map(value => <option key={value.id} value={value.id}>{value.subject} · {value.learningStage} · {value.goal}</option>)}</select></label>
        <label>Proposal kind<select value={kind} onChange={event => updateProposalField(setKind, event.target.value)}>{proposalKinds.map(value => <option key={value}>{value}</option>)}</select></label>
        <label>Producer kind<select value={producerKind} onChange={event => updateProposalField(setProducerKind, event.target.value)}>{producerKinds.map(value => <option key={value}>{value}</option>)}</select></label>
        <label>Producer identity<input required maxLength={160} value={producerIdentity} onChange={event => updateProposalField(setProducerIdentity, event.target.value)} placeholder="Exact model, agent, person, or import identity" /></label>
        <label>Recorded by<input required maxLength={120} value={recordedBy} onChange={event => updateProposalField(setRecordedBy, event.target.value)} placeholder="Exact recorder identity" /></label></div>
      <label>Proposed material<textarea required minLength={20} maxLength={16000} rows={8} value={content} onChange={event => updateProposalField(setContent, event.target.value)} /></label>
      <label>Evidence rationale<textarea required minLength={10} maxLength={4000} rows={3} value={rationale} onChange={event => updateProposalField(setRationale, event.target.value)} placeholder="Why the linked evidence supports this bounded proposal" /></label>
      <label>Known limitations<textarea required minLength={5} maxLength={4000} rows={3} value={limitations} onChange={event => updateProposalField(setLimitations, event.target.value)} placeholder="Uncertainty, missing evidence, transfer limits, or review needs" /></label>
      <fieldset><legend>Accepted curriculum evidence · select 1–12</legend>{!plan ? <p>Select an active study plan first.</p> : eligible.length === 0 ? <p>No accepted exact-subject and exact-stage candidates are available.</p> : eligible.map(value => <label key={value.id}><input type="checkbox" checked={evidenceIds.includes(value.id)} onChange={() => toggleEvidence(value.id)} /><span><strong>{value.id}</strong>{value.statementText}</span></label>)}</fieldset>
      <label className="teaching-proposal-confirm"><input type="checkbox" checked={confirmed} onChange={event => setConfirmed(event.target.checked)} />Record this as unreviewed draft material only. Do not apply it to a lesson.</label>
      <button disabled={busy || !confirmed || evidenceIds.length === 0}>Record proposal</button>
    </form>
    <div className="teaching-proposal-ledger"><h3>Proposal ledger</h3>{proposals.length === 0 ? <p>No proposals recorded.</p> : proposals.map(value => <article key={value.id} className={selectedProposal === value.id ? 'selected' : ''}>
      <header><div><strong>{value.id}</strong><span>{value.subject} · {value.learningStage} · {value.proposalKind}</span></div><em>{value.latestDecision ?? value.status}</em></header><p>{value.content}</p>
      <dl><div><dt>Producer</dt><dd>{value.producerKind} · {value.producerIdentity}</dd></div><div><dt>Recorded by</dt><dd>{value.recordedBy}</dd></div><div><dt>Evidence</dt><dd>{value.evidenceCount} accepted candidates</dd></div><div><dt>Rationale</dt><dd>{value.rationale}</dd></div><div><dt>Limitations</dt><dd>{value.limitations}</dd></div>{value.latestReviewId && <div><dt>Latest review</dt><dd>{value.latestDecision} · {value.latestReviewerIdentity} · {value.latestReviewNote}</dd></div>}</dl>
      <button type="button" onClick={() => selectProposal(value.id)}>Review this proposal</button></article>)}</div>
    <form className="teaching-proposal-review" onSubmit={submitReview}><h3>Append an immutable review</h3><p>{selectedProposal ? `Reviewing ${selectedProposal}` : 'Select a proposal from the ledger.'}</p>
      <div><label>Stable review ID<input required pattern="[a-z0-9_-]{3,64}" value={reviewId} onChange={event => updateReviewField(setReviewId, event.target.value)} /></label><label>Reviewer identity<input required maxLength={120} value={reviewer} onChange={event => updateReviewField(setReviewer, event.target.value)} /></label><label>Decision<select value={decision} onChange={event => updateReviewField(setDecision, event.target.value)}>{decisions.map(value => <option key={value}>{value}</option>)}</select></label></div>
      <label>Review note<textarea required minLength={5} maxLength={4000} rows={4} value={reviewNote} onChange={event => updateReviewField(setReviewNote, event.target.value)} /></label><label className="teaching-proposal-confirm"><input type="checkbox" checked={reviewConfirmed} onChange={event => setReviewConfirmed(event.target.checked)} />Record this review against the currently selected proposal evidence without applying it to a lesson.</label><button disabled={busy || !selectedProposal || !reviewConfirmed}>Record immutable review</button></form>
    <output className="teaching-proposal-state">{state}</output>
  </section>;
}
