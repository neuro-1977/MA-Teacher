import { useMemo, useState } from 'react';
import { feedbackPlanningEntries } from './feedback-planning-data';
import { questioningPlanningEntries } from './questioning-planning-data';
import { stageLenses } from './stage-lenses';
import { subjectLenses } from './subject-lenses';
import { vocabularyEntries } from './vocabulary-planning-data';
import { workedExamples } from './worked-examples';
import './teaching-planning-packet.css';

type CopyState = 'idle' | 'copied' | 'failed';

export function TeachingPlanningPacketPanel() {
  const [subjectId, setSubjectId] = useState<string>(subjectLenses[0].id);
  const [stageId, setStageId] = useState<string>(stageLenses[0].id);
  const [vocabularyId, setVocabularyId] = useState('');
  const [questioningId, setQuestioningId] = useState('');
  const [feedbackId, setFeedbackId] = useState('');
  const [exampleId, setExampleId] = useState('');
  const [copyState, setCopyState] = useState<CopyState>('idle');

  const subject = subjectLenses.find((item) => item.id === subjectId) ?? subjectLenses[0];
  const stage = stageLenses.find((item) => item.id === stageId) ?? stageLenses[0];
  const vocabularyMatches = vocabularyEntries.filter((item) => item.subject === subject.label && item.stages.some((value) => value === stage.id));
  const questioningMatches = questioningPlanningEntries.filter((item) => item.subject === subject.label && item.stages.some((value) => value === stage.id));
  const feedbackMatches = feedbackPlanningEntries.filter((item) => item.subject === subject.label && item.stages.some((value) => value === stage.id));
  const exampleMatches = workedExamples.filter((item) => item.subject === subject.label && item.stage === stage.id);
  const vocabulary = vocabularyMatches.find((item) => item.id === vocabularyId) ?? vocabularyMatches[0];
  const question = questioningMatches.find((item) => item.id === questioningId) ?? questioningMatches[0];
  const feedback = feedbackMatches.find((item) => item.id === feedbackId) ?? feedbackMatches[0];
  const example = exampleMatches.find((item) => item.id === exampleId) ?? exampleMatches[0];
  const populated = [vocabulary, question, feedback, example].filter(Boolean).length;

  const packetText = useMemo(() => [
    'MA-TEACHER / NON-CANONICAL TEACHING PLANNING PACKET',
    `Subject: ${subject.label}`,
    `Stage lens: ${stage.label} (${stage.support})`,
    `Curriculum boundary: ${stage.curriculumBoundary}`,
    '',
    vocabulary ? `VOCABULARY / ${vocabulary.id}\nTerm: ${vocabulary.term}\nLearner meaning: ${vocabulary.learnerMeaning}\nDisciplinary precision: ${vocabulary.disciplinaryPrecision}\nModel use: ${vocabulary.modelUse}\nRetrieval prompt: ${vocabulary.retrievalPrompt}\nCaution: ${vocabulary.caution}` : 'VOCABULARY / NO MATCH - do not invent an entry.',
    '',
    question ? `QUESTIONING / ${question.id}\nPurpose: ${question.purpose}\nPrompt: ${question.prompt}\nFollow-up: ${question.followUp}\nEvidence to notice: ${question.evidenceToNotice}\nCaution: ${question.caution}` : 'QUESTIONING / NO MATCH - do not invent an entry.',
    '',
    feedback ? `FEEDBACK / ${feedback.id}\nMoment: ${feedback.moment}\nOnly when observed: ${feedback.observedEvidence}\nFeedback stem: ${feedback.feedbackStem}\nLearner action: ${feedback.learnerAction}\nCaution: ${feedback.caution}` : 'FEEDBACK / NO MATCH - do not invent an entry.',
    '',
    example ? `WORKED EXAMPLE / ${example.id}\nTitle: ${example.title}\nSource boundary: ${example.sourceBoundary}\nLearning intention: ${example.learningIntention}\nModel: ${example.model}\nCheck prompt: ${example.checkPrompt}\nSuccess criteria:\n${example.successCriteria.map((item) => `- ${item}`).join('\n')}\nHuman-review example: ${example.humanReview}\nNext evidence: ${example.nextEvidence}` : 'WORKED EXAMPLE / NO MATCH - do not invent an entry.',
    '',
    'Boundary: source-present planning data only. Selection is not curriculum acceptance, learner evidence, a lesson record, delivery proof, diagnosis, progression, or evidence of effectiveness.',
  ].join('\n'), [example, feedback, question, stage, subject, vocabulary]);

  function changeSubject(value: string) {
    setSubjectId(value);
    setVocabularyId(''); setQuestioningId(''); setFeedbackId(''); setExampleId(''); setCopyState('idle');
  }

  function changeStage(value: string) {
    setStageId(value);
    setVocabularyId(''); setQuestioningId(''); setFeedbackId(''); setExampleId(''); setCopyState('idle');
  }

  async function copyPacket() {
    if (!navigator.clipboard) return setCopyState('failed');
    try { await navigator.clipboard.writeText(packetText); setCopyState('copied'); }
    catch { setCopyState('failed'); }
  }

  return (
    <section className="planning-packet" id="workspace-planning-packet" aria-labelledby="planning-packet-title">
      <header><div><p>Four-bank evidence selector</p><h2 id="planning-packet-title">Teaching planning packet</h2><span>Choose rather than merge: every selected item keeps its own evidence and refusal boundary.</span></div><strong>{populated}/4 BANKS POPULATED</strong></header>
      <aside className="planning-packet__boundary" role="note"><b>Source-present planning aid.</b> A match is not a recommendation for a learner, curriculum approval, assessment result, or evidence that the material works. Feedback language applies only when its named evidence is actually observed.</aside>
      <div className="planning-packet__scope">
        <label>Subject<select value={subjectId} onChange={(event) => changeSubject(event.target.value)}>{subjectLenses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Stage lens<select value={stageId} onChange={(event) => changeStage(event.target.value)}>{stageLenses.map((item) => <option key={item.id} value={item.id}>{item.label} / {item.support}</option>)}</select></label>
      </div>
      <div className="planning-packet__grid">
        <article><label>Vocabulary<select value={vocabulary?.id ?? ''} disabled={!vocabulary} onChange={(event) => { setVocabularyId(event.target.value); setCopyState('idle'); }}>{vocabularyMatches.length === 0 ? <option value="">No matching entry</option> : vocabularyMatches.map((item) => <option key={item.id} value={item.id}>{item.term} / {item.id}</option>)}</select></label>{vocabulary ? <><h3>{vocabulary.term}</h3><p>{vocabulary.learnerMeaning}</p><dl><dt>Precision</dt><dd>{vocabulary.disciplinaryPrecision}</dd><dt>Model</dt><dd>{vocabulary.modelUse}</dd><dt>Retrieve</dt><dd>{vocabulary.retrievalPrompt}</dd><dt>Caution</dt><dd>{vocabulary.caution}</dd></dl></> : <EvidenceGap bank="vocabulary" />}</article>
        <article><label>Questioning<select value={question?.id ?? ''} disabled={!question} onChange={(event) => { setQuestioningId(event.target.value); setCopyState('idle'); }}>{questioningMatches.length === 0 ? <option value="">No matching entry</option> : questioningMatches.map((item) => <option key={item.id} value={item.id}>{item.purpose} / {item.id}</option>)}</select></label>{question ? <><h3>{question.prompt}</h3><dl><dt>Follow-up</dt><dd>{question.followUp}</dd><dt>Evidence to notice</dt><dd>{question.evidenceToNotice}</dd><dt>Caution</dt><dd>{question.caution}</dd></dl></> : <EvidenceGap bank="questioning" />}</article>
        <article><label>Descriptive feedback<select value={feedback?.id ?? ''} disabled={!feedback} onChange={(event) => { setFeedbackId(event.target.value); setCopyState('idle'); }}>{feedbackMatches.length === 0 ? <option value="">No matching entry</option> : feedbackMatches.map((item) => <option key={item.id} value={item.id}>{item.moment} / {item.id}</option>)}</select></label>{feedback ? <><h3>{feedback.feedbackStem}</h3><dl><dt>Only when observed</dt><dd>{feedback.observedEvidence}</dd><dt>Learner action</dt><dd>{feedback.learnerAction}</dd><dt>Caution</dt><dd>{feedback.caution}</dd></dl></> : <EvidenceGap bank="feedback" />}</article>
        <article><label>Worked example<select value={example?.id ?? ''} disabled={!example} onChange={(event) => { setExampleId(event.target.value); setCopyState('idle'); }}>{exampleMatches.length === 0 ? <option value="">No matching entry</option> : exampleMatches.map((item) => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>{example ? <><h3>{example.title}</h3><p>{example.learningIntention}</p><dl><dt>Source boundary</dt><dd>{example.sourceBoundary}</dd><dt>Model</dt><dd>{example.model}</dd><dt>Check</dt><dd>{example.checkPrompt}</dd><dt>Next evidence</dt><dd>{example.nextEvidence}</dd></dl></> : <EvidenceGap bank="worked-example" />}</article>
      </div>
      <footer><button type="button" onClick={copyPacket}>Copy selected packet</button><span aria-live="polite">{copyState === 'copied' ? 'Copied with source IDs and boundaries.' : copyState === 'failed' ? 'Clipboard refused. Nothing was saved.' : 'Browser-memory selection only; no model call or database write.'}</span></footer>
    </section>
  );
}

function EvidenceGap({ bank }: { bank: string }) {
  return <div className="planning-packet__gap"><strong>No {bank} match.</strong><span>This is visible missing evidence, not permission to improvise or borrow from another subject or stage.</span></div>;
}
