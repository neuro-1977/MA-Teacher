import { useMemo, useState } from 'react';
import { feedbackPlanningEntries, feedbackStageLabels, type FeedbackMoment, type FeedbackStage, type FeedbackSubject } from './feedback-planning-data';
import './feedback-planning.css';

const subjects: Array<'All subjects' | FeedbackSubject> = ['All subjects', ...Array.from(new Set(feedbackPlanningEntries.map((entry) => entry.subject)))];
const moments: Array<'All moments' | FeedbackMoment> = ['All moments', 'During learning', 'After an attempt', 'During revision'];

export function FeedbackPlanningPanel() {
  const [subject, setSubject] = useState<(typeof subjects)[number]>('All subjects');
  const [moment, setMoment] = useState<(typeof moments)[number]>('All moments');
  const [stage, setStage] = useState<'all' | FeedbackStage>('all');
  const [query, setQuery] = useState('');

  const entries = useMemo(() => {
    const needle = query.trim().toLocaleLowerCase();
    return feedbackPlanningEntries.filter((entry) => {
      if (subject !== 'All subjects' && entry.subject !== subject) return false;
      if (moment !== 'All moments' && entry.moment !== moment) return false;
      if (stage !== 'all' && !entry.stages.includes(stage)) return false;
      return !needle || [entry.subject, entry.stageLabel, entry.moment, entry.observedEvidence, entry.feedbackStem, entry.learnerAction, entry.caution]
        .some((value) => value.toLocaleLowerCase().includes(needle));
    });
  }, [moment, query, stage, subject]);

  return (
    <section className="feedback-planning" id="workspace-feedback-planning" aria-labelledby="feedback-planning-title">
      <header className="feedback-planning__header">
        <div><p className="feedback-planning__eyebrow">Evidence before judgement</p><h2 id="feedback-planning-title">Descriptive feedback planning</h2><p>Turn one observable feature into a bounded explanation and a learner-owned next action.</p></div>
        <span className="feedback-planning__count" aria-live="polite">{entries.length} of {feedbackPlanningEntries.length}</span>
      </header>
      <p className="feedback-planning__boundary" role="note">Static planning language only. No grade, praise score, diagnosis, learner profile, response capture, or approval record is produced.</p>
      <div className="feedback-planning__filters" aria-label="Feedback bank filters">
        <label>Subject<select value={subject} onChange={(event) => setSubject(event.target.value as (typeof subjects)[number])}>{subjects.map((value) => <option key={value}>{value}</option>)}</select></label>
        <label>Moment<select value={moment} onChange={(event) => setMoment(event.target.value as (typeof moments)[number])}>{moments.map((value) => <option key={value}>{value}</option>)}</select></label>
        <label>Stage lens<select value={stage} onChange={(event) => setStage(event.target.value as 'all' | FeedbackStage)}><option value="all">All stage lenses</option>{Object.entries(feedbackStageLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        <label>Search<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="evidence, revision, boundary..." /></label>
      </div>
      {entries.length === 0 ? <p className="feedback-planning__empty">No entries match. Widen the filters rather than fabricating feedback.</p> : (
        <div className="feedback-planning__grid">{entries.map((entry) => (
          <article className="feedback-card" key={entry.id}>
            <div className="feedback-card__meta"><span>{entry.subject}</span><span>{entry.stageLabel}</span><strong>{entry.moment}</strong></div>
            <dl>
              <div><dt>Observed evidence</dt><dd>{entry.observedEvidence}</dd></div>
              <div className="feedback-card__stem"><dt>Feedback stem</dt><dd>{entry.feedbackStem}</dd></div>
              <div><dt>Learner action</dt><dd>{entry.learnerAction}</dd></div>
              <div className="feedback-card__caution"><dt>Caution</dt><dd>{entry.caution}</dd></div>
            </dl>
          </article>
        ))}</div>
      )}
    </section>
  );
}
