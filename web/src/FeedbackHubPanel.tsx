import { InfoTip } from './InfoTip';
import './feedback-hub.css';

const issueUrl = 'https://github.com/neuro-1977/MA-Teacher/issues/new/choose';
const discussionUrl = 'https://github.com/neuro-1977/MA-Teacher/discussions';

export function FeedbackHubPanel() {
  const prompts = [
    ['Something broke', 'Tell us what you clicked, what you expected, and what happened.'],
    ['Something was hard', 'Tell us which words or steps were confusing.'],
    ['I have an idea', 'Tell us what would make learning or teaching easier.'],
    ['Something helped', 'Tell us what worked well so we keep the good parts.'],
  ] as const;

  return <section id="workspace-feedback-hub" className="feedback-hub" aria-labelledby="feedback-hub-title">
    <header><div><p>YOUR IDEAS MATTER</p><h2 id="feedback-hub-title">Help us build MA-Teacher.</h2><span>You use the app, so you can spot things the builders miss.</span></div><InfoTip label="What happens to feedback?">Public feedback can be copied into the local development queue. Serenity and the project team review it before planning changes.</InfoTip></header>
    <div className="feedback-hub__prompts">{prompts.map(([title, body]) => <article key={title}><strong>{title}</strong><span>{body}</span></article>)}</div>
    <aside><strong>Ask an adult before posting.</strong><span>Use made-up examples. Never share your real name, school, work, passwords, health details, or the MA-Teacher database in public.</span></aside>
    <div className="feedback-hub__actions"><a href={discussionUrl} target="_blank" rel="noreferrer"><strong>Share an idea</strong><span>Join a friendly GitHub Discussion.</span></a><a href={issueUrl} target="_blank" rel="noreferrer"><strong>Report a problem</strong><span>Open a guided GitHub Issue.</span></a></div>
    <details><summary>For teachers and developers</summary><p>Feedback is evidence to investigate, not proof that a diagnosis is correct. Run <code>./scripts/sync-github-feedback.ps1</code> to bring public issue text and comments into the local feedback queue before planning work.</p></details>
  </section>;
}
