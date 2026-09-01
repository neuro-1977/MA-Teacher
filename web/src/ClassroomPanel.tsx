import { useCallback, useEffect, useMemo, useState } from 'react'
import './ClassroomPanel.css'

type Learner = { id: string; displayName: string }
type StudyPlan = { id: string; learnerId: string }
type Lesson = { id: string; studyPlanId: string; title: string }
type ReviewLesson = { id: string; latestDecision?: string | null; latestReviewCurrent: boolean }
type ClassroomStatus = {
  ok: boolean
  running: boolean
  classroomUrl?: string | null
  activeInvites: number
  connectedLearners: number
  error?: string | null
  safetyIncidents?: Array<{ id: string; lastSeenUtc: string; learnerId: string; lessonId: string; surface: string; categories: string[]; occurrenceCount: number }>
}
type InviteResult = {
  ok: boolean
  state: string
  classroomUrl?: string | null
  code?: string | null
  expiresUtc?: string | null
  error?: string | null
}
type PrinterOverview = {
  ok: boolean
  printers: Array<{ name: string; isDefault: boolean }>
  requests: Array<{ id: string; requestedUtc: string; learnerId: string; lessonId: string; documentKind: string; state: string; error?: string | null }>
  error?: string | null
}

export default function ClassroomPanel() {
  const [learners, setLearners] = useState<Learner[]>([])
  const [plans, setPlans] = useState<StudyPlan[]>([])
  const [lessons, setLessons] = useState<Lesson[]>([])
  const [approvedIds, setApprovedIds] = useState<Set<string>>(new Set())
  const [learnerId, setLearnerId] = useState('')
  const [lessonId, setLessonId] = useState('')
  const [durationMinutes, setDurationMinutes] = useState(60)
  const [status, setStatus] = useState<ClassroomStatus | null>(null)
  const [invite, setInvite] = useState<InviteResult | null>(null)
  const [message, setMessage] = useState('Loading the classroom controls...')
  const [busy, setBusy] = useState(false)
  const [printing, setPrinting] = useState<PrinterOverview | null>(null)
  const [printerName, setPrinterName] = useState('')

  const refresh = useCallback(async () => {
    try {
      const [workspaceResponse, reviewResponse, statusResponse, printerResponse] = await Promise.all([
        fetch('/api/teaching/workspace', { cache: 'no-store' }),
        fetch('/api/teaching/lesson-reviews', { cache: 'no-store' }),
        fetch('/api/classroom/status', { cache: 'no-store' }),
        fetch('/api/printing/status', { cache: 'no-store' }),
      ])
      const workspace = await workspaceResponse.json()
      const reviews = await reviewResponse.json()
      const classroom = await statusResponse.json()
      const printerStatus: PrinterOverview = await printerResponse.json()
      setLearners(Array.isArray(workspace.learners) ? workspace.learners : [])
      setPlans(Array.isArray(workspace.studyPlans) ? workspace.studyPlans : [])
      setLessons(Array.isArray(workspace.lessonDrafts) ? workspace.lessonDrafts : [])
      setApprovedIds(new Set((Array.isArray(reviews.lessons) ? reviews.lessons : [])
        .filter((lesson: ReviewLesson) => lesson.latestReviewCurrent && lesson.latestDecision === 'approved-for-use')
        .map((lesson: ReviewLesson) => lesson.id)))
      setStatus(classroom)
      setPrinting(printerStatus)
      setPrinterName((current) => current || printerStatus.printers.find((printer) => printer.isDefault)?.name || printerStatus.printers[0]?.name || '')
      setMessage(classroom.error || (classroom.running ? 'Classroom sharing is on.' : 'Classroom sharing is off.'))
    } catch {
      setMessage('The classroom controls could not be loaded.')
    }
  }, [])

  useEffect(() => { void refresh() }, [refresh])

  const planById = useMemo(() => new Map(plans.map((plan) => [plan.id, plan])), [plans])
  const availableLessons = useMemo(() => lessons.filter((lesson) => {
    const plan = planById.get(lesson.studyPlanId)
    return approvedIds.has(lesson.id) && (!learnerId || plan?.learnerId === learnerId)
  }), [approvedIds, learnerId, lessons, planById])

  const createInvite = async () => {
    if (!learnerId || !lessonId) { setMessage('Choose a learner and an approved lesson first.'); return }
    setBusy(true)
    setInvite(null)
    try {
      const response = await fetch('/api/classroom/invites', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'create-classroom-invite' },
        body: JSON.stringify({ learnerId, lessonId, durationMinutes }),
      })
      const result: InviteResult = await response.json()
      setInvite(result)
      setMessage(result.ok ? 'Invite ready. Give this link and code only to the named learner.' : result.error || 'The invite was refused.')
      await refresh()
    } catch { setMessage('The invite could not be created.') }
    finally { setBusy(false) }
  }

  const stopSharing = async () => {
    setBusy(true)
    try {
      const response = await fetch('/api/classroom/stop', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'stop-classroom-sharing' },
        body: '{}',
      })
      const result = await response.json()
      setInvite(null)
      setMessage(result.message || result.error || 'Classroom sharing stopped.')
      await refresh()
    } catch { setMessage('Classroom sharing could not be stopped.') }
    finally { setBusy(false) }
  }

  const copyInvite = async () => {
    if (!invite?.classroomUrl || !invite.code) return
    try {
      await navigator.clipboard.writeText(`${invite.classroomUrl}\nCode: ${invite.code}`)
      setMessage('The classroom link and code were copied.')
    } catch { setMessage('Copy was blocked. Read the link and code from the card.') }
  }

  const decidePrint = async (requestId: string, approve: boolean) => {
    if (approve && !printerName) { setMessage('Choose a detected printer first.'); return }
    setBusy(true)
    try {
      const response = await fetch(approve ? '/api/printing/approve' : '/api/printing/decline', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': approve ? 'approve-local-print' : 'decline-local-print' },
        body: JSON.stringify(approve ? { requestId, printerName } : { requestId }),
      })
      const result = await response.json()
      setMessage(result.ok ? (approve ? 'The approved document was sent to the printer.' : 'The print request was declined.') : result.error || 'The print decision failed.')
      await refresh()
    } catch { setMessage('The print decision could not be completed.') }
    finally { setBusy(false) }
  }

  const printSafetyReport = async () => {
    if (!printerName) { setMessage('Choose a detected printer first.'); return }
    setBusy(true)
    try {
      const response = await fetch('/api/printing/safety-report', { method:'POST', headers:{'Content-Type':'application/json','X-MA-Teacher-Intent':'print-safety-report'}, body:JSON.stringify({printerName}) })
      const result = await response.json()
      setMessage(result.ok ? 'The safety report was sent to the printer.' : result.error || 'The safety report did not print.')
    } catch { setMessage('The safety report could not be printed.') }
    finally { setBusy(false) }
  }

  const joinedLearners = status?.connectedLearners ?? 0
  const relayStage = status?.error ? 'problem' : joinedLearners > 0 ? 'joined' : status?.running ? 'waiting' : 'ready'
  const relayHeadline = relayStage === 'problem'
    ? 'The classroom link needs an IT check.'
    : relayStage === 'joined'
      ? `${joinedLearners} learner${joinedLearners === 1 ? '' : 's'} joined.`
      : relayStage === 'waiting'
        ? 'The invite is ready. Waiting for the learner.'
        : 'Ready to make a private classroom link.'
  const relayDetail = relayStage === 'problem'
    ? 'No learner access was opened. The message below explains what to check.'
    : relayStage === 'joined'
      ? 'The learner can now see only the approved lesson and practice check you shared.'
      : relayStage === 'waiting'
        ? 'The learner opens the link on the same school network and enters the one-use code.'
        : 'Choose one learner and one teacher-approved lesson. The local link starts only when you make the invite.'

  return (
    <section className="classroom-panel" aria-labelledby="classroom-panel-title">
      <header>
        <div>
          <p className="classroom-panel__eyebrow">Teacher control</p>
          <h2 id="classroom-panel-title">Share one lesson</h2>
          <p>Make a short-lived classroom link for one learner and one approved lesson. Students do not install an app.</p>
        </div>
        <span className={`classroom-panel__state ${status?.running ? 'is-on' : ''}`}>{status ? (status.running ? 'Sharing on' : 'Sharing off') : 'Checking'}</span>
      </header>

      <div className={`classroom-panel__readiness is-${relayStage}`} role="status" aria-live="polite">
        <span className="classroom-panel__readiness-label">Classroom status</span>
        <strong>{relayHeadline}</strong>
        <p>{relayDetail}</p>
        {status?.running && status.classroomUrl && <small>{status.classroomUrl}</small>}
      </div>

      <ol className="classroom-panel__journey" aria-label="Classroom sharing steps">
        <li className={relayStage === 'ready' || relayStage === 'problem' ? 'is-current' : 'is-done'}><span>1</span><div><strong>Make invite</strong><small>One learner, one lesson</small></div></li>
        <li className={relayStage === 'waiting' ? 'is-current' : relayStage === 'joined' ? 'is-done' : ''}><span>2</span><div><strong>Learner joins</strong><small>Same school network</small></div></li>
        <li className={relayStage === 'joined' ? 'is-current' : ''}><span>3</span><div><strong>Learn safely</strong><small>Stop sharing when done</small></div></li>
      </ol>

      <div className="classroom-panel__notice" role="note">
        <strong>Same managed school network only.</strong> The learner uses a web browser, not another app or an internet account. Ask school IT to use a Private or Domain network.
      </div>

      <div className="classroom-panel__form">
        <label>1. Learner
          <select value={learnerId} onChange={(event) => { setLearnerId(event.target.value); setLessonId('') }}>
            <option value="">Choose a learner</option>
            {learners.map((learner) => <option key={learner.id} value={learner.id}>{learner.displayName}</option>)}
          </select>
        </label>
        <label>2. Approved lesson
          <select value={lessonId} onChange={(event) => setLessonId(event.target.value)} disabled={!learnerId}>
            <option value="">Choose an approved lesson</option>
            {availableLessons.map((lesson) => <option key={lesson.id} value={lesson.id}>{lesson.title}</option>)}
          </select>
        </label>
        <label>3. Link lasts
          <select value={durationMinutes} onChange={(event) => setDurationMinutes(Number(event.target.value))}>
            <option value={30}>30 minutes</option>
            <option value={60}>1 hour</option>
            <option value={120}>2 hours</option>
            <option value={240}>4 hours</option>
          </select>
        </label>
      </div>

      <div className="classroom-panel__actions">
        <button type="button" className="classroom-panel__primary" onClick={createInvite} disabled={busy || !learnerId || !lessonId}>Make learner invite</button>
        <button type="button" className="classroom-panel__stop" onClick={stopSharing} disabled={busy || !status?.running}>Stop sharing and sign everyone out</button>
        <button type="button" onClick={() => void refresh()} disabled={busy}>Refresh</button>
      </div>

      {invite?.ok && <div className="classroom-panel__invite">
        <h3>Give these to the named learner</h3>
        <span>Classroom link</span><strong>{invite.classroomUrl}</strong>
        <span>Code (works once)</span><strong className="classroom-panel__code">{invite.code}</strong>
        <span>Ends</span><strong>{invite.expiresUtc ? new Date(invite.expiresUtc).toLocaleString() : 'Soon'}</strong>
        <button type="button" onClick={copyInvite}>Copy link and code</button>
      </div>}

      <p className="classroom-panel__message" aria-live="polite">{message}</p>
      <footer className="classroom-panel__counts">
        <div><strong>{status?.activeInvites ?? 0}</strong><span>active invite{(status?.activeInvites ?? 0) === 1 ? '' : 's'}</span></div>
        <div><strong>{joinedLearners}</strong><span>learner{joinedLearners === 1 ? '' : 's'} joined</span></div>
        <small>Stopping sharing revokes every invite and signs learners out immediately.</small>
      </footer>
      {(status?.safetyIncidents?.length ?? 0) > 0 && <div className="classroom-panel__safety-reports">
        <h3>Learner safety reports</h3>
        <p>Talk to the learner. These reports are evidence for follow-up, not automatic punishment.</p>
        {status?.safetyIncidents?.map((incident) => {
          const learner = learners.find((value) => value.id === incident.learnerId)
          const lesson = lessons.find((value) => value.id === incident.lessonId)
          return <article key={incident.id}>
            <strong>{learner?.displayName || incident.learnerId}</strong>
            <span>{lesson?.title || incident.lessonId}</span>
            <span>{incident.categories.join(', ').replaceAll('-', ' ')}</span>
            <small>{new Date(incident.lastSeenUtc).toLocaleString()} · {incident.occurrenceCount} time(s)</small>
          </article>
        })}
      </div>}
      <div className="classroom-panel__printing">
        <h3>Teacher-approved printing</h3>
        <p>Learners can ask. Only you can choose a Windows printer and approve.</p>
        <label>Local printer
          <select value={printerName} onChange={(event) => setPrinterName(event.target.value)}>
            <option value="">No printer selected</option>
            {printing?.printers.map((printer) => <option key={printer.name} value={printer.name}>{printer.name}{printer.isDefault ? ' (Windows default)' : ''}</option>)}
          </select>
        </label>
        {printing?.error && <p className="classroom-panel__printer-error">{printing.error}</p>}
        {(printing?.requests.filter((request) => request.state === 'pending').length ?? 0) === 0
          ? <div className="classroom-panel__no-prints">No print requests are waiting.</div>
          : printing?.requests.filter((request) => request.state === 'pending').map((request) => {
            const learner = learners.find((value) => value.id === request.learnerId)
            const lesson = lessons.find((value) => value.id === request.lessonId)
            return <article key={request.id}>
              <div><strong>{learner?.displayName || request.learnerId}</strong><span>{request.documentKind} · {lesson?.title || request.lessonId}</span></div>
              <button type="button" className="classroom-panel__primary" disabled={busy || !printerName} onClick={() => void decidePrint(request.id, true)}>Approve and print</button>
              <button type="button" disabled={busy} onClick={() => void decidePrint(request.id, false)}>Decline</button>
            </article>
          })}
        <button type="button" disabled={busy || !printerName} onClick={() => void printSafetyReport()}>Print teacher safety report</button>
      </div>
    </section>
  )
}
