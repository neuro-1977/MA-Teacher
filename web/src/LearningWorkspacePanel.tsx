import { FormEvent, useEffect, useMemo, useState } from 'react'
import './learning-workspace.css'
import './candidate-review.css'
import './document-library.css'
import './document-text.css'
import './revision-drift.css'

type TeachingGate = { id: string; capability: string; state: string; requiredEvidence: string }
type TeachingPrinciple = { id: string; title: string; ruleText: string }
type TeachingOverview = { learnerProfiles: number; studyPlans: number; principles: TeachingPrinciple[]; gates: TeachingGate[] }
type Learner = { id: string; displayName: string; ageBand: string; learningStage: string; locale: string }
type StudyPlan = { id: string; learnerId: string; subject: string; learningStage: string; goal: string; status: string }
type Workspace = { ok: boolean; learners: Learner[]; studyPlans: StudyPlan[] }
type MutationResult = { ok: boolean; state: string; id?: string; error?: string }
type SourceRevision = { id: number; sourceId: string; fetchedUtc: string; sha256: string; bodyBytes: number }
type CurriculumCandidate = { id: string; sourceRevisionId: number; subject: string; learningStage: string; statementText: string; sourceLocator: string; statementSha256: string; reviewState: string }
type RevisionResponse = { ok: boolean; revisions: SourceRevision[] }
type CandidateResponse = { ok: boolean; candidates: CurriculumCandidate[] }
type CurriculumDocument = { id: string; sourceId: string; discoveredFromRevisionId: number; title: string; documentUrl: string; mediaTypeHint: string; discoveryState: string; latestRevisionId?: number; latestSha256?: string; latestBodyBytes?: number }
type DocumentResponse = { ok: boolean; documents: CurriculumDocument[] }
type DocumentTextBlock = { id: string; documentRevisionId: number; documentId: string; ordinal: number; sourceLocator: string; textContent: string; textSha256: string; parserId: string; extractionState: string }
type DocumentBlockResponse = { ok: boolean; blocks: DocumentTextBlock[] }
type DocumentRevision = { id: number; documentId: string; fetchedUtc: string; contentType: string; sha256: string; bodyBytes: number }
type DocumentRevisionResponse = { ok: boolean; revisions: DocumentRevision[] }
type DriftComparison = { id: string; scopeKind: string; ownerId: string; olderRevisionId: number; newerRevisionId: number; olderSha256: string; newerSha256: string; state: string; olderItems: number; newerItems: number; addedItems: number; removedItems: number; unchangedItems: number; latestDecision?: string; latestNote?: string }
type DriftResponse = { ok: boolean; comparisons: DriftComparison[] }

const subjects = ['english', 'mathematics', 'science', 'history', 'languages', 'computing', 'cross-curricular', 'other']
const stableId = (prefix: string) => `${prefix}-${crypto.randomUUID()}`

async function readJson<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...options })
  const body = await response.json() as T & { error?: string }
  if (!response.ok) throw new Error(body.error || `Request failed with HTTP ${response.status}.`)
  return body
}

export function LearningWorkspacePanel() {
  const [overview, setOverview] = useState<TeachingOverview | null>(null)
  const [workspace, setWorkspace] = useState<Workspace>({ ok: true, learners: [], studyPlans: [] })
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')
  const [learnerId, setLearnerId] = useState(() => stableId('learner'))
  const [displayName, setDisplayName] = useState('')
  const [ageBand, setAgeBand] = useState('')
  const [learningStage, setLearningStage] = useState('')
  const [planId, setPlanId] = useState(() => stableId('plan'))
  const [planLearnerId, setPlanLearnerId] = useState('')
  const [subject, setSubject] = useState('science')
  const [planStage, setPlanStage] = useState('')
  const [goal, setGoal] = useState('')
  const [revisions, setRevisions] = useState<SourceRevision[]>([])
  const [selectedRevisionId, setSelectedRevisionId] = useState('')
  const [candidates, setCandidates] = useState<CurriculumCandidate[]>([])
  const [documents, setDocuments] = useState<CurriculumDocument[]>([])
  const [documentBlocks, setDocumentBlocks] = useState<DocumentTextBlock[]>([])
  const [documentRevisions, setDocumentRevisions] = useState<DocumentRevision[]>([])
  const [driftComparisons, setDriftComparisons] = useState<DriftComparison[]>([])
  const [driftScope, setDriftScope] = useState<'source' | 'document'>('source')
  const [olderRevisionId, setOlderRevisionId] = useState('')
  const [newerRevisionId, setNewerRevisionId] = useState('')
  const [driftNote, setDriftNote] = useState('')

  const selectedLearner = useMemo(
    () => workspace.learners.find((learner) => learner.id === planLearnerId),
    [workspace.learners, planLearnerId],
  )

  async function refresh() {
    const [nextOverview, nextWorkspace, revisionResponse, candidateResponse, documentResponse, blockResponse, documentRevisionResponse, driftResponse] = await Promise.all([
      readJson<TeachingOverview>('/api/teaching/overview'),
      readJson<Workspace>('/api/teaching/workspace'),
      readJson<RevisionResponse>('/api/curriculum/revisions'),
      readJson<CandidateResponse>('/api/curriculum/candidates'),
      readJson<DocumentResponse>('/api/curriculum/documents'),
      readJson<DocumentBlockResponse>('/api/curriculum/document-blocks'),
      readJson<DocumentRevisionResponse>('/api/curriculum/document-revisions'),
      readJson<DriftResponse>('/api/curriculum/drift'),
    ])
    setOverview(nextOverview)
    setWorkspace(nextWorkspace)
    setPlanLearnerId((current) => current || nextWorkspace.learners[0]?.id || '')
    setRevisions(revisionResponse.revisions)
    setSelectedRevisionId((current) => current || String(revisionResponse.revisions[0]?.id || ''))
    setCandidates(candidateResponse.candidates)
    setDocuments(documentResponse.documents)
    setDocumentBlocks(blockResponse.blocks)
    setDocumentRevisions(documentRevisionResponse.revisions)
    setDriftComparisons(driftResponse.comparisons)
  }

  useEffect(() => {
    refresh().catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Teaching workspace unavailable.'))
  }, [])

  useEffect(() => {
    if (selectedLearner && !planStage) setPlanStage(selectedLearner.learningStage)
  }, [selectedLearner, planStage])

  async function submitLearner(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<MutationResult>('/api/teaching/learners', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'manage-learning-workspace' },
        body: JSON.stringify({ id: learnerId, displayName, ageBand, learningStage, locale: 'en-GB', accessibility: {}, preferences: {} }),
      })
      setNotice(result.state === 'already-present' ? 'Learner already recorded. Nothing was duplicated.' : 'Learner continuity recorded locally.')
      setLearnerId(stableId('learner')); setDisplayName(''); setAgeBand(''); setLearningStage('')
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Learner could not be recorded.') }
    finally { setBusy(false) }
  }

  async function submitPlan(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<MutationResult>('/api/teaching/study-plans', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'manage-learning-workspace' },
        body: JSON.stringify({ id: planId, learnerId: planLearnerId, subject, learningStage: planStage, goal }),
      })
      setNotice(result.state === 'already-present' ? 'Study plan already recorded. Nothing was duplicated.' : 'Study plan recorded locally.')
      setPlanId(stableId('plan')); setGoal('')
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Study plan could not be recorded.') }
    finally { setBusy(false) }
  }

  async function extractCandidates() {
    if (!selectedRevisionId) return
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ ok: boolean; inserted: number; existing: number }>('/api/curriculum/candidates/extract', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'review-curriculum-candidates' },
        body: JSON.stringify({ revisionId: Number(selectedRevisionId) }),
      })
      setNotice(`Candidate scan recorded ${result.inserted} new and ${result.existing} existing rows. Nothing was accepted automatically.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Candidate extraction failed.') }
    finally { setBusy(false) }
  }

  async function reviewCandidate(id: string, decision: 'accept' | 'reject') {
    setBusy(true); setError(''); setNotice('')
    try {
      await readJson<MutationResult>('/api/curriculum/candidates/review', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'review-curriculum-candidates' },
        body: JSON.stringify({ id, decision, note: '' }),
      })
      setNotice(`Candidate ${decision === 'accept' ? 'accepted' : 'rejected'} with a local review receipt.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Candidate review failed.') }
    finally { setBusy(false) }
  }

  async function discoverDocuments() {
    if (!selectedRevisionId) return
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ discovered: number; inserted: number; existing: number }>('/api/curriculum/documents/discover', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'manage-curriculum-documents' },
        body: JSON.stringify({ revisionId: Number(selectedRevisionId) }),
      })
      setNotice(`Document discovery found ${result.discovered}: ${result.inserted} new, ${result.existing} already known. Nothing was downloaded.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Document discovery failed.') }
    finally { setBusy(false) }
  }

  async function captureDocument(documentId: string) {
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ state: string; bodyBytes: number; sha256?: string }>('/api/curriculum/documents/capture', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'manage-curriculum-documents' },
        body: JSON.stringify({ documentId }),
      })
      setNotice(`Document ${result.state}; ${result.bodyBytes.toLocaleString()} bytes retained as unparsed evidence.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Document capture failed.') }
    finally { setBusy(false) }
  }

  async function parseDocument(documentRevisionId: number) {
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ state: string; blocksFound: number; inserted: number; existing: number }>('/api/curriculum/documents/parse', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'parse-curriculum-document' },
        body: JSON.stringify({ documentRevisionId }),
      })
      setNotice(`Parser recorded ${result.blocksFound} unreviewed blocks: ${result.inserted} new, ${result.existing} existing.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Document parsing failed.') }
    finally { setBusy(false) }
  }

  async function scanDocumentBlocks(documentRevisionId: number) {
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ blocksScanned: number; candidatesFound: number; inserted: number; evidenceLinks: number }>('/api/curriculum/document-blocks/extract-candidates', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'review-curriculum-candidates' },
        body: JSON.stringify({ documentRevisionId }),
      })
      setNotice(`Scanned ${result.blocksScanned} blocks and found ${result.candidatesFound} review candidates; ${result.inserted} new, ${result.evidenceLinks} provenance links added.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Document candidate scan failed.') }
    finally { setBusy(false) }
  }

  async function compareRevisions() {
    if (!olderRevisionId || !newerRevisionId) return
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ comparisonId: string; state: string; added: number; removed: number; unchanged: number }>('/api/curriculum/drift/compare', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'reconcile-curriculum-revisions' },
        body: JSON.stringify({ scopeKind: driftScope, olderRevisionId: Number(olderRevisionId), newerRevisionId: Number(newerRevisionId) }),
      })
      setNotice(`Revision comparison recorded: ${result.state}; ${result.added} added, ${result.removed} removed, ${result.unchanged} unchanged.`)
      await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Revision comparison failed.') }
    finally { setBusy(false) }
  }

  async function recordDriftDisposition(comparisonId: string, decision: 'reviewed-no-impact' | 'reviewed-action-required' | 'deferred') {
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await readJson<{ state: string }>('/api/curriculum/drift/disposition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'reconcile-curriculum-revisions' },
        body: JSON.stringify({ comparisonId, decision, note: driftNote }),
      })
      setNotice(`Revision disposition recorded: ${result.state}. No curriculum row was changed automatically.`)
      setDriftNote(''); await refresh()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Revision disposition failed.') }
    finally { setBusy(false) }
  }

  return <section className="learning-workspace" aria-labelledby="learning-workspace-title">
    <header className="learning-workspace__header"><div><span className="learning-workspace__eyebrow">LOCAL LEARNING CONTINUITY</span><h2 id="learning-workspace-title">Set the learner. Name the destination.</h2><p>Profiles and plans persist locally. Lessons, marking and tutoring remain locked behind evidence gates.</p></div><div className="learning-workspace__counts"><strong>{overview?.learnerProfiles ?? workspace.learners.length}</strong><span>learners</span><strong>{overview?.studyPlans ?? workspace.studyPlans.length}</strong><span>plans</span></div></header>
    {(notice || error) && <div className={`learning-workspace__notice ${error ? 'is-error' : ''}`} role="status">{error || notice}</div>}
    <div className="learning-workspace__forms">
      <form onSubmit={submitLearner} className="learning-card"><span className="learning-card__number">01</span><h3>Learner</h3><label>Display name<input value={displayName} onChange={(event) => setDisplayName(event.target.value)} maxLength={80} required /></label><label>Age band<input value={ageBand} onChange={(event) => setAgeBand(event.target.value)} maxLength={40} placeholder="e.g. 11-14 or adult learner" required /></label><label>Learning stage<input value={learningStage} onChange={(event) => setLearningStage(event.target.value)} maxLength={80} placeholder="e.g. Key Stage 3, GCSE foundation, beginner" required /></label><button disabled={busy}>Record learner</button></form>
      <form onSubmit={submitPlan} className="learning-card"><span className="learning-card__number">02</span><h3>Study plan</h3><label>Learner<select value={planLearnerId} onChange={(event) => { setPlanLearnerId(event.target.value); setPlanStage(workspace.learners.find((item) => item.id === event.target.value)?.learningStage || '') }} required><option value="">Select a recorded learner</option>{workspace.learners.map((learner) => <option key={learner.id} value={learner.id}>{learner.displayName}</option>)}</select></label><label>Subject<select value={subject} onChange={(event) => setSubject(event.target.value)}>{subjects.map((item) => <option key={item}>{item}</option>)}</select></label><label>Stage<input value={planStage} onChange={(event) => setPlanStage(event.target.value)} maxLength={80} required /></label><label>Learning goal<textarea value={goal} onChange={(event) => setGoal(event.target.value)} minLength={3} maxLength={500} required /></label><button disabled={busy || !workspace.learners.length}>Record study plan</button></form>
    </div>
    <div className="learning-workspace__ledger"><article><h3>Active plans</h3>{workspace.studyPlans.length ? workspace.studyPlans.map((plan) => <div className="plan-row" key={plan.id}><strong>{plan.subject}</strong><span>{plan.learningStage}</span><p>{plan.goal}</p></div>) : <p className="empty-state">No plans yet. Record a learner first.</p>}</article><article><h3>Evidence gates</h3>{overview?.gates.map((gate) => <div className="gate-row" key={gate.id}><span data-state={gate.state}>{gate.state}</span><strong>{gate.capability}</strong><p>{gate.requiredEvidence}</p></div>)}</article></div>
    <section className="candidate-review" aria-labelledby="candidate-review-title"><header><div><span className="learning-workspace__eyebrow">SOURCE REVIEW</span><h3 id="candidate-review-title">Curriculum candidates</h3><p>Scanning finds review candidates. Only a deliberate operator decision changes their review state.</p></div><div className="candidate-review__scan"><select aria-label="Captured source revision" value={selectedRevisionId} onChange={(event) => setSelectedRevisionId(event.target.value)}><option value="">No captured revision</option>{revisions.map((revision) => <option value={revision.id} key={revision.id}>Revision {revision.id} · {revision.sourceId}</option>)}</select><button type="button" disabled={busy || !selectedRevisionId} onClick={extractCandidates}>Scan selected revision</button></div></header><div className="candidate-review__list">{candidates.length ? candidates.map((candidate) => <article key={candidate.id}><div className="candidate-review__meta"><span>{candidate.subject}</span><span>{candidate.learningStage}</span><span>{candidate.reviewState}</span><span>rev {candidate.sourceRevisionId} · {candidate.sourceLocator}</span></div><p>{candidate.statementText}</p><div className="candidate-review__actions"><code>{candidate.statementSha256.slice(0, 16)}</code><button type="button" disabled={busy || candidate.reviewState === 'accepted'} onClick={() => reviewCandidate(candidate.id, 'accept')}>Accept</button><button type="button" disabled={busy || candidate.reviewState === 'rejected'} onClick={() => reviewCandidate(candidate.id, 'reject')}>Reject</button></div></article>) : <p className="empty-state">No candidates. Capture an official source revision before scanning.</p>}</div></section>
    <section className="document-library" aria-labelledby="document-library-title"><header><div><span className="learning-workspace__eyebrow">OFFICIAL ATTACHMENTS</span><h3 id="document-library-title">Linked curriculum documents</h3><p>Discover links first. Capture exact bytes separately. Extracted text remains unreviewed.</p></div><button type="button" disabled={busy || !selectedRevisionId} onClick={discoverDocuments}>Discover from selected revision</button></header><div className="document-library__grid">{documents.length ? documents.map((document) => { const parseSupported = document.mediaTypeHint.includes('pdf') || document.mediaTypeHint.includes('opendocument') || document.mediaTypeHint.includes('wordprocessingml'); const blockCount = document.latestRevisionId ? documentBlocks.filter((block) => block.documentRevisionId === document.latestRevisionId).length : 0; return <article key={document.id}><div className="document-library__state"><span>{document.mediaTypeHint.split('/').slice(-1)[0]}</span><strong>{document.discoveryState}</strong></div><h4>{document.title}</h4><p>{document.sourceId} · found in revision {document.discoveredFromRevisionId}</p>{document.latestSha256 ? <dl><dt>Latest hash</dt><dd><code>{document.latestSha256.slice(0, 20)}</code></dd><dt>Bytes</dt><dd>{document.latestBodyBytes?.toLocaleString()}</dd><dt>Loaded blocks</dt><dd>{blockCount.toLocaleString()}</dd></dl> : <p className="document-library__uncaptured">Link recorded. Bytes not captured.</p>}<div className="document-library__actions"><a href={document.documentUrl} target="_blank" rel="noreferrer">View official source</a><button type="button" disabled={busy} onClick={() => captureDocument(document.id)}>{document.latestRevisionId ? 'Check for revision' : 'Capture document'}</button>{document.latestRevisionId && <button type="button" disabled={busy || !parseSupported} title={parseSupported ? 'Extract unreviewed text blocks' : 'No parser is available'} onClick={() => parseDocument(document.latestRevisionId!)}>{parseSupported ? 'Extract text' : 'Parser locked'}</button>}{document.latestRevisionId && blockCount > 0 && <button type="button" disabled={busy} onClick={() => scanDocumentBlocks(document.latestRevisionId!)}>Find candidates</button>}</div></article> }) : <p className="empty-state">No linked documents discovered from the selected source revision.</p>}</div></section>
    <section className="document-text" aria-labelledby="document-text-title"><header><div><span className="learning-workspace__eyebrow">UNREVIEWED EXTRACTION</span><h3 id="document-text-title">Document text blocks</h3></div><strong>{documentBlocks.length.toLocaleString()} blocks</strong></header><p className="document-text__boundary">Text extraction proves only that characters were recovered from a captured revision. It does not prove statutory classification, objective boundaries, or teaching accuracy.</p><div className="document-text__list">{documentBlocks.length ? documentBlocks.slice(0, 200).map((block) => <article key={block.id}><div><span>{block.extractionState}</span><span>revision {block.documentRevisionId}</span><span>{block.sourceLocator}</span></div><p>{block.textContent}</p><footer><code>{block.textSha256.slice(0, 16)}</code><small>{block.parserId}</small></footer></article>) : <p className="empty-state">No ODT or DOCX text blocks have been extracted.</p>}</div>{documentBlocks.length > 200 && <p className="document-text__limit">Showing the first 200 of {documentBlocks.length.toLocaleString()} blocks. The database retains the bounded result set.</p>}</section>
    <section className="revision-drift" aria-labelledby="revision-drift-title"><header><div><span className="learning-workspace__eyebrow">REVISION RECONCILIATION</span><h3 id="revision-drift-title">What changed, without rewriting history</h3><p>Compare revisions from the same source or document. A comparison records evidence only.</p></div></header><div className="revision-drift__controls"><label>Scope<select value={driftScope} onChange={(event) => { setDriftScope(event.target.value as 'source' | 'document'); setOlderRevisionId(''); setNewerRevisionId('') }}><option value="source">Source pages and candidates</option><option value="document">Documents and text blocks</option></select></label><label>Older revision<select value={olderRevisionId} onChange={(event) => setOlderRevisionId(event.target.value)}><option value="">Select revision</option>{(driftScope === 'source' ? revisions : documentRevisions).map((revision) => <option key={revision.id} value={revision.id}>#{revision.id} · {'sourceId' in revision ? revision.sourceId : revision.documentId} · {revision.sha256.slice(0, 10)}</option>)}</select></label><label>Newer revision<select value={newerRevisionId} onChange={(event) => setNewerRevisionId(event.target.value)}><option value="">Select revision</option>{(driftScope === 'source' ? revisions : documentRevisions).map((revision) => <option key={revision.id} value={revision.id}>#{revision.id} · {'sourceId' in revision ? revision.sourceId : revision.documentId} · {revision.sha256.slice(0, 10)}</option>)}</select></label><button type="button" disabled={busy || !olderRevisionId || !newerRevisionId || olderRevisionId === newerRevisionId} onClick={compareRevisions}>Compare revisions</button></div><label className="revision-drift__note">Disposition note<textarea value={driftNote} onChange={(event) => setDriftNote(event.target.value)} maxLength={1000} placeholder="Optional operator reasoning retained with the next disposition" /></label><div className="revision-drift__list">{driftComparisons.length ? driftComparisons.map((comparison) => <article key={comparison.id}><div className="revision-drift__state"><span>{comparison.scopeKind}</span><strong>{comparison.state}</strong><span>{comparison.ownerId}</span></div><h4>Revision {comparison.olderRevisionId} → {comparison.newerRevisionId}</h4><div className="revision-drift__counts"><span><b>{comparison.addedItems}</b> added</span><span><b>{comparison.removedItems}</b> removed</span><span><b>{comparison.unchangedItems}</b> unchanged</span></div>{comparison.latestDecision && <p className="revision-drift__decision">Latest: {comparison.latestDecision}{comparison.latestNote ? ` · ${comparison.latestNote}` : ''}</p>}<div className="revision-drift__actions"><button type="button" disabled={busy || comparison.state.includes('coverage-unproven')} title={comparison.state.includes('coverage-unproven') ? 'Complete extraction coverage before recording no impact' : ''} onClick={() => recordDriftDisposition(comparison.id, 'reviewed-no-impact')}>No curriculum impact</button><button type="button" disabled={busy} onClick={() => recordDriftDisposition(comparison.id, 'reviewed-action-required')}>Action required</button><button type="button" disabled={busy} onClick={() => recordDriftDisposition(comparison.id, 'deferred')}>Defer</button></div></article>) : <p className="empty-state">No revision comparisons have been recorded.</p>}</div></section>
    <details className="learning-workspace__principles"><summary>Teaching contract and boundaries</summary><div>{overview?.principles.map((principle) => <article key={principle.id}><strong>{principle.title}</strong><p>{principle.ruleText}</p></article>)}</div></details>
  </section>
}
