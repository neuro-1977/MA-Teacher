export type QuestioningStage = 'early-learning' | 'ks1' | 'ks2' | 'ks3' | 'ks4' | 'post16-adult';
export type QuestioningSubject = 'English' | 'Mathematics' | 'Science' | 'History and histories' | 'Languages' | 'Computing and IT';
export type QuestioningPurpose = 'Activate prior knowledge' | 'Elicit reasoning' | 'Surface a misconception' | 'Support transfer' | 'Prompt reflection';

export interface QuestioningPlanningEntry {
  id: string;
  subject: QuestioningSubject;
  stages: QuestioningStage[];
  stageLabel: string;
  purpose: QuestioningPurpose;
  prompt: string;
  followUp: string;
  evidenceToNotice: string;
  caution: string;
}

export const questioningStageLabels: Record<QuestioningStage, string> = {
  'early-learning': 'Early learning', ks1: 'KS1', ks2: 'KS2', ks3: 'KS3', ks4: 'KS4', 'post16-adult': 'Post-16 / adult',
};

export const questioningPlanningEntries: QuestioningPlanningEntry[] = [
  {
    id: 'english-evidence-choice', subject: 'English', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', purpose: 'Elicit reasoning',
    prompt: 'Which detail most strongly supports your interpretation, and why is it stronger than another possible detail?',
    followUp: 'What would make you revise that choice?',
    evidenceToNotice: 'A distinction between quotation, inference and explanation, plus willingness to compare alternatives.',
    caution: 'Do not imply that one quotation has a permanently correct meaning outside its context.',
  },
  {
    id: 'english-audience-transfer', subject: 'English', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', purpose: 'Support transfer',
    prompt: 'How would you reshape this explanation for an audience that does not already know the topic?',
    followUp: 'Which word, example or structural choice would you change first?',
    evidenceToNotice: 'Deliberate choices about register, assumed knowledge, sequencing and explanation.',
    caution: 'Audience adaptation is not permission to remove necessary precision or evidence.',
  },
  {
    id: 'maths-equivalence-test', subject: 'Mathematics', stages: ['ks1', 'ks2', 'ks3'], stageLabel: 'KS1-KS3 lens', purpose: 'Surface a misconception',
    prompt: 'How could we test whether these two representations have the same value?',
    followUp: 'Can you show a different test that should reach the same conclusion?',
    evidenceToNotice: 'Use of a valid operation, representation or invariant rather than visual resemblance alone.',
    caution: 'Choose representations suited to the learner; symbolic manipulation is not always the clearest first proof.',
  },
  {
    id: 'maths-method-comparison', subject: 'Mathematics', stages: ['ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'KS2-adult lens', purpose: 'Prompt reflection',
    prompt: 'Where do these two methods make the same decision, and where do they differ?',
    followUp: 'When might one method be more transparent or efficient than the other?',
    evidenceToNotice: 'Attention to mathematical structure, assumptions and intermediate steps instead of preference alone.',
    caution: 'Efficiency is context dependent; do not label a valid method inferior merely because it is longer.',
  },
  {
    id: 'science-observation-inference', subject: 'Science', stages: ['ks1', 'ks2', 'ks3'], stageLabel: 'KS1-KS3 lens', purpose: 'Surface a misconception',
    prompt: 'What did you directly observe, and what did you infer from that observation?',
    followUp: 'What other inference could fit the same observation?',
    evidenceToNotice: 'Separation of measurement or description from an explanation that remains open to testing.',
    caution: 'Do not dismiss an inference because it is uncertain; identify what further evidence would discriminate between explanations.',
  },
  {
    id: 'science-variable-transfer', subject: 'Science', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', purpose: 'Support transfer',
    prompt: 'If this variable changed in a new setting, which relationship would you expect to remain and which might not?',
    followUp: 'Which assumption from the original investigation are you relying on?',
    evidenceToNotice: 'A conditional prediction tied to mechanism, controlled variables and limits of the original evidence.',
    caution: 'A classroom pattern is not automatically a universal law; preserve uncertainty and scope.',
  },
  {
    id: 'history-source-provenance', subject: 'History and histories', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', purpose: 'Activate prior knowledge',
    prompt: 'What do we already know about who created this source, when, for whom and for what purpose?',
    followUp: 'How does that knowledge shape what the source can and cannot help us investigate?',
    evidenceToNotice: 'Use of provenance to define evidential value without reducing the source to reliable or unreliable.',
    caution: 'Bias does not make a source useless; it may itself be evidence when the enquiry is explicit.',
  },
  {
    id: 'history-continuity-scale', subject: 'History and histories', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', purpose: 'Elicit reasoning',
    prompt: 'What changed, what continued, and for whom across this period?',
    followUp: 'Would your judgement change if the timescale or group changed?',
    evidenceToNotice: 'A bounded claim that recognises uneven experience, chronology and the scale of analysis.',
    caution: 'Avoid presenting one group, region or account as the complete historical experience.',
  },
  {
    id: 'languages-register-choice', subject: 'Languages', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', purpose: 'Support transfer',
    prompt: 'Which expression fits this speaker, audience and setting, and what would you change in a more formal setting?',
    followUp: 'What clue tells you that the relationship or context has changed?',
    evidenceToNotice: 'A context-sensitive register choice rather than word-for-word substitution.',
    caution: 'Register varies by region and community; examples require current, qualified language review.',
  },
  {
    id: 'languages-cognate-check', subject: 'Languages', stages: ['ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'KS2-adult lens', purpose: 'Surface a misconception',
    prompt: 'This word resembles one you know. What evidence would confirm that the meaning is genuinely related here?',
    followUp: 'Could grammar, context or a false cognate change your first interpretation?',
    evidenceToNotice: 'Use of sentence context, morphology, reference evidence or usage rather than appearance alone.',
    caution: 'Do not teach resemblance as proof; false cognates and partial overlaps are common.',
  },
  {
    id: 'computing-trace-state', subject: 'Computing and IT', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', purpose: 'Elicit reasoning',
    prompt: 'What is the state before this instruction, what changes, and what is the state immediately afterwards?',
    followUp: 'At which exact step does the observed result first diverge from the intended result?',
    evidenceToNotice: 'A traceable sequence of state changes that localises behaviour rather than guesses at a final symptom.',
    caution: 'Do not hide intermediate state when it is the evidence needed to explain the defect.',
  },
  {
    id: 'computing-new-input', subject: 'Computing and IT', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', purpose: 'Support transfer',
    prompt: 'Which parts of this solution still hold when the input, scale or environment changes?',
    followUp: 'What boundary case would most strongly test your answer?',
    evidenceToNotice: 'Explicit assumptions, invariants, failure modes and a testable boundary case.',
    caution: 'A passing example is not general proof; distinguish demonstrated behaviour from expected behaviour.',
  },
  {
    id: 'early-english-story-change', subject: 'English', stages: ['early-learning'], stageLabel: 'Early-learning lens', purpose: 'Elicit reasoning',
    prompt: 'Show, tell or act what happened first. What changed next?',
    followUp: 'Which picture, object, action or word helped you decide?',
    evidenceToNotice: 'A chosen event and an observable clue used to connect two moments in a story or shared experience.',
    caution: 'Accept speech, sign, gesture, movement, drawing and supported communication; do not treat recall speed as comprehension.',
  },
  {
    id: 'early-maths-sorting-rule', subject: 'Mathematics', stages: ['early-learning'], stageLabel: 'Early-learning lens', purpose: 'Surface a misconception',
    prompt: 'Can you put together the things that belong together? What is the same about them?',
    followUp: 'Where would this new object go, and can you show why?',
    evidenceToNotice: 'A consistent visible sorting property and whether the same rule is applied to a new object.',
    caution: 'A different valid sorting rule is not an error; ask the learner to show the rule before judging the group.',
  },
  {
    id: 'early-science-notice-change', subject: 'Science', stages: ['early-learning'], stageLabel: 'Early-learning lens', purpose: 'Activate prior knowledge',
    prompt: 'What do you notice before we change it? What do you think we might notice afterwards?',
    followUp: 'What stayed the same, and what changed when we looked again?',
    evidenceToNotice: 'A direct sensory observation, a prediction and a later comparison kept as separate contributions.',
    caution: 'Use safe, accessible observation routes and never require tasting, touching, sound or vision as the only evidence channel.',
  },
  {
    id: 'early-history-personal-sequence', subject: 'History and histories', stages: ['early-learning'], stageLabel: 'Early-learning lens', purpose: 'Elicit reasoning',
    prompt: 'Which came before and which came after? What clue helps you put them in that order?',
    followUp: 'Could we swap any two, or would a clue stop us?',
    evidenceToNotice: 'Use of an image, object, routine or narrated clue to establish relative sequence.',
    caution: 'Use consented, non-sensitive examples; do not assume every learner has the same family structure, memories or photographs.',
  },
  {
    id: 'early-languages-meaning-choice', subject: 'Languages', stages: ['early-learning'], stageLabel: 'Early-learning lens', purpose: 'Support transfer',
    prompt: 'When you hear this word or phrase, which object, picture, action or place does it belong with?',
    followUp: 'Can you use the word, sign, gesture or agreed communication method in a new little situation?',
    evidenceToNotice: 'A meaningful association and an attempt to use it in a changed context, not pronunciation conformity alone.',
    caution: 'Do not mock accent, home language, code-switching, silence or an alternative communication mode; examples need qualified language review.',
  },
  {
    id: 'early-computing-physical-sequence', subject: 'Computing and IT', stages: ['early-learning'], stageLabel: 'Early-learning lens', purpose: 'Elicit reasoning',
    prompt: 'Which instruction should our person, toy or floor robot follow first, and where will it be afterwards?',
    followUp: 'What one instruction would you change if it went somewhere else?',
    evidenceToNotice: 'An ordered instruction, predicted state or position, and a local repair after observed movement.',
    caution: 'Keep the activity physical and collaborative where useful; do not equate device familiarity or fine motor control with computational thinking.',
  },
];
