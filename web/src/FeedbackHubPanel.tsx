import { useMemo, useState } from 'react';
import { InfoTip } from './InfoTip';
import './feedback-hub.css';

const issueUrl = 'https://github.com/neuro-1977/MA-Teacher/issues/new/choose';
const discussionUrl = 'https://github.com/neuro-1977/MA-Teacher/discussions';

const feedbackKinds = [
  { id: 'broken', title: 'Something broke', prompt: 'What did you try? What did you expect? What happened instead?' },
  { id: 'hard', title: 'Something was hard', prompt: 'Which word, button or step was difficult to understand?' },
  { id: 'idea', title: 'I have an idea', prompt: 'What would make learning or teaching easier?' },
  { id: 'helped', title: 'Something helped', prompt: 'What worked well, and why did it help?' },
] as const;

const feedbackAreas = [
  'Getting started',
  'Lessons',
  'Practice and work',
  'Teacher feedback',
  'Progress',
  'Explore subjects',
  'Printing or setup',
  'Another part of the app',
] as const;

type FeedbackKindId = typeof feedbackKinds[number]['id'];

type FeedbackFinding = {
  id: string;
  message: string;
};

const feedbackSafetyRules: ReadonlyArray<{ id: string; message: string; pattern: RegExp }> = [
  { id: 'email', message: 'Remove the email address.', pattern: /\b[\w.+-]+@[\w.-]+\.[a-z]{2,}\b/i },
  { id: 'phone', message: 'Remove the phone number.', pattern: /(?:\+?\d[\d\s().-]{6,}\d)/ },
  { id: 'web-link', message: 'Remove the web link. An adult can add a safe link later if it is needed.', pattern: /\b(?:https?:\/\/|www\.)\S+/i },
  { id: 'secret', message: 'Remove passwords, passcodes, keys, tokens and recovery codes.', pattern: /\b(?:password|passcode|secret\s+code|recovery\s+code|api[- ]?key|access\s+token)\b/i },
  { id: 'identity', message: 'Remove your name, school, home address or postcode.', pattern: /\b(?:my name is|i am called|my school is|i go to .{0,35}school|i live at|my address is|my postcode is)\b/i },
  { id: 'account', message: 'Remove usernames, gamer names and account names.', pattern: /\b(?:my username is|my user name is|my gamer(?:tag| name) is|my account is|find me on)\b/i },
  { id: 'age-or-class', message: 'Remove your age, birthday, class, year group or teacher name.', pattern: /\b(?:i am|i'm)\s+(?:[4-9]|1[0-8])\s*(?:years? old|yo)\b|\b(?:my birthday is|my class is|i am in class|i'm in class|my teacher is|i am in year|i'm in year)\b/i },
  { id: 'health', message: 'Remove private health or diagnosis details.', pattern: /\b(?:my diagnosis|my medical|my medication|my health condition)\b/i },
  { id: 'learner-work', message: 'Do not paste lesson answers or homework into public feedback. Describe the problem with a made-up example.', pattern: /\b(?:my answer is|the answer i wrote|here is my homework|my homework is)\b/i },
  { id: 'unsafe-language', message: 'Remove rude, hateful or unsafe words. Describe what happened without repeating them.', pattern: /\b(?:f[\W_]*u[\W_]*c[\W_]*k(?:ing|ed)?|s[\W_]*h[\W_]*i[\W_]*t(?:ty)?|c[\W_]*u[\W_]*n[\W_]*t(?:s)?|b[\W_]*i[\W_]*t[\W_]*c[\W_]*h(?:es)?|b[\W_]*a[\W_]*s[\W_]*t[\W_]*a[\W_]*r[\W_]*d(?:s)?)\b/i },
];

export function inspectFeedbackDraft(value: string): FeedbackFinding[] {
  const text = value.trim();
  const findings: FeedbackFinding[] = [];
  if (text.length < 20) findings.push({ id: 'too-short', message: 'Add a little more detail so an adult can understand what happened.' });
  for (const rule of feedbackSafetyRules) {
    if (rule.pattern.test(text)) findings.push({ id: rule.id, message: rule.message });
  }
  return findings;
}

function buildReviewedDraft(kindId: FeedbackKindId, area: string, draft: string) {
  const kind = feedbackKinds.find((entry) => entry.id === kindId) ?? feedbackKinds[0];
  return [
    'MA-Teacher feedback',
    `Type: ${kind.title}`,
    `Part of the app: ${area}`,
    '',
    draft.trim(),
    '',
    'Privacy note: this draft was checked in MA-Teacher and reviewed by an adult before sharing. It uses no real learner record.',
  ].join('\n');
}

export function FeedbackHubPanel() {
  const [kindId, setKindId] = useState<FeedbackKindId>('idea');
  const [area, setArea] = useState<string>(feedbackAreas[0]);
  const [draft, setDraft] = useState('');
  const [checked, setChecked] = useState(false);
  const [adultReviewed, setAdultReviewed] = useState(false);
  const [copyStatus, setCopyStatus] = useState('');
  const selectedKind = feedbackKinds.find((entry) => entry.id === kindId) ?? feedbackKinds[0];
  const findings = useMemo(() => inspectFeedbackDraft(draft), [draft]);
  const draftReady = checked && findings.length === 0;
  const canShare = draftReady && adultReviewed;
  const reviewedDraft = useMemo(() => buildReviewedDraft(kindId, area, draft), [kindId, area, draft]);

  const resetReview = () => {
    setChecked(false);
    setAdultReviewed(false);
    setCopyStatus('');
  };

  const copyDraft = async () => {
    if (!canShare) return;
    try {
      await navigator.clipboard.writeText(reviewedDraft);
      setCopyStatus('Copied. An adult can now paste the reviewed draft into the public form.');
    } catch {
      setCopyStatus('Copy did not work. An adult can select the reviewed text below and copy it normally.');
    }
  };

  const clearDraft = () => {
    setDraft('');
    setKindId('idea');
    setArea(feedbackAreas[0]);
    resetReview();
  };

  return <section id="workspace-feedback-hub" className="feedback-hub" aria-labelledby="feedback-hub-title">
    <header>
      <div>
        <p>YOUR IDEAS MATTER</p>
        <h2 id="feedback-hub-title">Tell us what you think.</h2>
        <span>Write a private draft here. Nothing leaves this page by itself.</span>
      </div>
      <InfoTip label="What happens to feedback?">Your draft stays on this page. A responsible adult reads it and decides whether it should be shared. A report is something to investigate, not proof that a bug or idea is correct.</InfoTip>
    </header>

    <ol className="feedback-hub__steps" aria-label="Three feedback steps">
      <li><strong>1</strong><span>Write a made-up example</span></li>
      <li><strong>2</strong><span>Check it for private details</span></li>
      <li><strong>3</strong><span>Ask an adult to decide what happens next</span></li>
    </ol>

    <form className="feedback-hub__draft" onSubmit={(event) => { event.preventDefault(); setChecked(true); setAdultReviewed(false); setCopyStatus(''); }}>
      <fieldset>
        <legend>What would you like to tell us?</legend>
        <div className="feedback-hub__kind-grid">
          {feedbackKinds.map((kind) => <button
            key={kind.id}
            type="button"
            aria-pressed={kindId === kind.id}
            onClick={() => { setKindId(kind.id); resetReview(); }}
          >
            <strong>{kind.title}</strong>
            <span>{kind.prompt}</span>
          </button>)}
        </div>
      </fieldset>

      <label className="feedback-hub__field">
        <strong>Which part of MA-Teacher?</strong>
        <select value={area} onChange={(event) => { setArea(event.target.value); resetReview(); }}>
          {feedbackAreas.map((entry) => <option key={entry}>{entry}</option>)}
        </select>
      </label>

      <label className="feedback-hub__field">
        <strong>{selectedKind.title}</strong>
        <span>{selectedKind.prompt}</span>
        <textarea
          value={draft}
          maxLength={1200}
          rows={6}
          placeholder="Use a made-up example. Do not use names, ages, school details, usernames, addresses, passwords, health details or lesson answers."
          onChange={(event) => { setDraft(event.target.value); resetReview(); }}
        />
        <small>{draft.length} / 1200 characters</small>
      </label>

      <aside>
        <strong>Keep private things private.</strong>
        <span>Do not include a real name, age, birthday, class, school, teacher, address, phone number, email, username, password, health detail, photo or lesson answer.</span>
      </aside>

      <div className="feedback-hub__check-actions">
        <button type="submit">Check my draft</button>
        <button type="button" onClick={clearDraft}>Clear</button>
      </div>
    </form>

    {checked && <section className={`feedback-hub__result ${draftReady ? 'is-ready' : 'has-findings'}`} aria-live="polite">
      {draftReady ? <>
        <strong>Your draft passed these simple checks.</strong>
        <span>These simple checks can miss things. A responsible adult must still read every word and decide whether to share it.</span>
      </> : <>
        <strong>Please change your draft before sharing it.</strong>
        <ul>{findings.map((finding) => <li key={finding.id}>{finding.message}</li>)}</ul>
      </>}
    </section>}

    {draftReady && <section className="feedback-hub__adult-review" aria-labelledby="adult-review-title">
      <div>
        <p>ADULT REVIEW</p>
        <h3 id="adult-review-title">A responsible adult checks this next.</h3>
        <span>The app cannot prove that a draft is safe. Read it, remove anything private, and share it only if it is useful and safe.</span>
      </div>
      <label><input type="checkbox" checked={adultReviewed} onChange={(event) => { setAdultReviewed(event.target.checked); setCopyStatus(''); }} /> I am a responsible adult. I read every word and will decide whether to post it.</label>
      <textarea className="feedback-hub__reviewed-text" readOnly value={reviewedDraft} rows={9} aria-label="Reviewed feedback draft" />
      <div className="feedback-hub__adult-actions">
        <button type="button" disabled={!canShare} onClick={copyDraft}>Copy reviewed draft</button>
        {canShare
          ? <a href={kindId === 'broken' ? issueUrl : discussionUrl} target="_blank" rel="noreferrer">Open the adult public feedback page</a>
          : <span>Public feedback stays locked until an adult checks the box.</span>}
      </div>
      {copyStatus && <p className="feedback-hub__copy-status" role="status">{copyStatus}</p>}
    </section>}

    <details>
      <summary>For responsible adults</summary>
      <p>This draft disappears when the page reloads. MA-Teacher does not send it, save it, or post it for you. Copying and opening the public feedback page remain separate adult choices.</p>
    </details>
  </section>;
}
