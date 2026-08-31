import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import logoUrl from '../../icon-large.png'
import './ClassroomStudentShell.css'

type LessonSection = { sequence: number; kind: string; content: string }
type Check = { id: string; prompt: string; successCriteria: string; responseMode: string }
type Attempt = { id: string; checkId: string; responseText: string; submittedUtc: string; reviewState: string; outcome?: string | null; feedback?: string | null; attachmentName?: string | null }
type ClassroomView = {
  ok: boolean
  error?: string
  learner?: { id: string; name: string }
  lesson?: { id: string; title: string; goal: string; subject: string; stage: string; sections: LessonSection[] }
  checks?: Check[]
  attempts?: Attempt[]
  printRequests?: Array<{ id: string; documentKind: string; state: string }>
  boundaries?: string[]
}

const allowedFileTypes = ['application/pdf','application/vnd.openxmlformats-officedocument.wordprocessingml.document','application/vnd.oasis.opendocument.text','text/plain','image/png','image/jpeg','image/webp']

export default function ClassroomStudentShell() {
  const [view, setView] = useState<ClassroomView | null>(null)
  const [joining, setJoining] = useState(true)
  const [code, setCode] = useState('')
  const [message, setMessage] = useState('Enter the code your teacher gave you.')
  const [tab, setTab] = useState<'lesson'|'practice'|'feedback'>('lesson')
  const [answers, setAnswers] = useState<Record<string,string>>({})
  const [files, setFiles] = useState<Record<string,File|null>>({})
  const [busyCheck, setBusyCheck] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const response = await fetch('/api/classroom/me', { cache:'no-store', credentials:'same-origin' })
      if (response.status === 401) { setJoining(true); setView(null); return }
      const result: ClassroomView = await response.json()
      setView(result)
      setJoining(!result.ok)
      if (!result.ok) setMessage(result.error || 'Ask your teacher for a new classroom code.')
    } catch { setJoining(true); setMessage('The teacher classroom is not ready. Ask your teacher to check it.') }
  }, [])

  useEffect(() => { void load() }, [load])

  const join = async (event: FormEvent) => {
    event.preventDefault()
    setMessage('Checking your code...')
    try {
      const response = await fetch('/api/classroom/join', { method:'POST', headers:{'Content-Type':'application/json'}, credentials:'same-origin', body:JSON.stringify({code}) })
      const result = await response.json()
      if (!result.ok) { setMessage(result.error || 'That code did not work.'); return }
      setMessage('You are in. Your lesson is ready.')
      await load()
    } catch { setMessage('The classroom could not be reached. Ask your teacher for help.') }
  }

  const toBase64 = (file: File) => new Promise<string>((resolve,reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result).split(',',2)[1] || '')
    reader.onerror = () => reject(reader.error)
    reader.readAsDataURL(file)
  })

  const submit = async (check: Check) => {
    const file = files[check.id]
    if (file && (file.size > 10 * 1024 * 1024 || !allowedFileTypes.includes(file.type))) { setMessage('Choose one allowed file smaller than 10 MB.'); return }
    if (!(answers[check.id] || '').trim() && !file) { setMessage('Write an answer or choose one file first.'); return }
    setBusyCheck(check.id)
    setMessage('Sending your work to your teacher...')
    try {
      const response = await fetch('/api/classroom/attempts', {
        method:'POST', headers:{'Content-Type':'application/json'}, credentials:'same-origin',
        body:JSON.stringify({ checkId:check.id, responseText:answers[check.id] || '', attachmentName:file?.name || null, attachmentMediaType:file?.type || null, attachmentBase64:file ? await toBase64(file) : null }),
      })
      const result = await response.json()
      if (!result.ok) { setMessage(result.error || 'Your work was not saved.'); return }
      setAnswers((current) => ({...current,[check.id]:''}))
      setFiles((current) => ({...current,[check.id]:null}))
      setMessage('Work saved. Your teacher will read it.')
      await load()
    } catch { setMessage('Your work could not be sent. It has not been marked as saved.') }
    finally { setBusyCheck(null) }
  }

  const logout = async () => {
    try { await fetch('/api/classroom/logout',{method:'POST',headers:{'Content-Type':'application/json'},credentials:'same-origin',body:'{}'}) } catch {}
    setView(null); setJoining(true); setCode(''); setMessage('You have left the classroom.')
  }

  const requestPrint = async (kind: 'lesson'|'feedback') => {
    setMessage('Asking your teacher...')
    try {
      const response = await fetch('/api/classroom/print-requests',{method:'POST',headers:{'Content-Type':'application/json'},credentials:'same-origin',body:JSON.stringify({kind})})
      const result = await response.json()
      setMessage(result.ok ? 'Print request saved. Your teacher must approve it.' : result.error || 'The print request was not saved.')
      await load()
    } catch { setMessage('The print request could not be sent.') }
  }

  const attemptsByCheck = useMemo(() => {
    const map = new Map<string,Attempt[]>()
    for (const attempt of view?.attempts || []) map.set(attempt.checkId,[...(map.get(attempt.checkId)||[]),attempt])
    return map
  },[view?.attempts])

  if (joining || !view?.ok || !view.lesson) return <main className="student-classroom student-classroom--join">
    <section className="student-classroom__join-card">
      <img src={logoUrl} alt="MA-Teacher smiling tutor wearing a graduation cap" />
      <p className="student-classroom__eyebrow">Your local classroom</p>
      <h1>Ready to learn?</h1>
      <p>Type the one-use code your teacher gave you. You do not need an account or an app.</p>
      <form onSubmit={join}>
        <label>Classroom code<input value={code} onChange={(event)=>setCode(event.target.value.toUpperCase())} autoComplete="one-time-code" maxLength={20} placeholder="ABCD-EFGH-IJKL" /></label>
        <button type="submit" disabled={!code.trim()}>Join my lesson</button>
      </form>
      <p aria-live="polite">{message}</p>
      <small>This works only while your teacher is sharing on the school network.</small>
    </section>
  </main>

  return <main className="student-classroom">
    <header className="student-classroom__topbar">
      <div><p className="student-classroom__eyebrow">Hi {view.learner?.name}</p><h1>{view.lesson.title}</h1><p>{view.lesson.subject} · {view.lesson.stage}</p></div>
      <button type="button" onClick={logout}>Leave classroom</button>
    </header>
    <nav aria-label="Your lesson steps">
      <button className={tab==='lesson'?'is-active':''} onClick={()=>setTab('lesson')}>1. My lesson</button>
      <button className={tab==='practice'?'is-active':''} onClick={()=>setTab('practice')}>2. Try it</button>
      <button className={tab==='feedback'?'is-active':''} onClick={()=>setTab('feedback')}>3. My feedback</button>
    </nav>
    <p className="student-classroom__message" aria-live="polite">{message}</p>
    <div className="student-classroom__print-request">
      <span>Need paper?</span>
      <button type="button" onClick={() => void requestPrint(tab === 'feedback' ? 'feedback' : 'lesson')}>Ask my teacher to print {tab === 'feedback' ? 'my feedback' : 'this lesson'}</button>
      {(view.printRequests || []).some((request) => request.state === 'pending' && request.documentKind === (tab === 'feedback' ? 'feedback' : 'lesson')) && <strong>Waiting for teacher</strong>}
    </div>
    {tab==='lesson' && <section className="student-classroom__page"><div className="student-classroom__goal"><span>Today we are learning to</span><strong>{view.lesson.goal}</strong></div>{view.lesson.sections.sort((a,b)=>a.sequence-b.sequence).map((section)=><article key={`${section.sequence}-${section.kind}`}><p>{section.kind}</p><div>{section.content}</div></article>)}</section>}
    {tab==='practice' && <section className="student-classroom__page"><h2>Show what you know</h2><p>Your teacher reads your work. The computer does not invent a score.</p>{(view.checks||[]).length===0?<div className="student-classroom__empty">Your teacher has not added a practice check yet.</div>:(view.checks||[]).map((check)=><article className="student-classroom__check" key={check.id}><h3>{check.prompt}</h3><p><strong>A good answer will:</strong> {check.successCriteria}</p><label>Your answer<textarea value={answers[check.id]||''} onChange={(event)=>setAnswers((current)=>({...current,[check.id]:event.target.value}))} maxLength={12000} /></label><label className="student-classroom__file">Or add one file<input type="file" accept=".pdf,.docx,.odt,.txt,.png,.jpg,.jpeg,.webp" onChange={(event)=>setFiles((current)=>({...current,[check.id]:event.target.files?.[0]||null}))} /></label><button type="button" onClick={()=>void submit(check)} disabled={busyCheck===check.id}>{busyCheck===check.id?'Saving...':'Send to my teacher'}</button></article>)}</section>}
    {tab==='feedback' && <section className="student-classroom__page"><h2>What my teacher said</h2>{(view.attempts||[]).length===0?<div className="student-classroom__empty">Your feedback will appear here after you send work and your teacher reviews it.</div>:(view.checks||[]).map((check)=><article className="student-classroom__feedback" key={check.id}><h3>{check.prompt}</h3>{(attemptsByCheck.get(check.id)||[]).map((attempt)=><div key={attempt.id}><span>{new Date(attempt.submittedUtc).toLocaleString()}</span><p>{attempt.responseText || attempt.attachmentName || 'File submitted'}</p><strong>{attempt.reviewState==='reviewed'?(attempt.outcome||'Reviewed'):'Waiting for teacher review'}</strong>{attempt.feedback&&<blockquote>{attempt.feedback}</blockquote>}</div>)}</article>)}</section>}
  </main>
}
