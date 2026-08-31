export type FeedbackStage = 'early-learning' | 'ks1' | 'ks2' | 'ks3' | 'ks4' | 'post16-adult';
export type FeedbackSubject = 'English' | 'Mathematics' | 'Science' | 'History and histories' | 'Languages' | 'Computing and IT' | 'Geography and environment' | 'Art and design' | 'Music and performance' | 'Physical education' | 'Citizenship and media literacy' | 'Health and wellbeing' | 'Money and life skills' | 'Religion, belief and philosophy';
export type FeedbackMoment = 'During learning' | 'After an attempt' | 'During revision';

export interface FeedbackPlanningEntry {
  id: string;
  subject: FeedbackSubject;
  stages: FeedbackStage[];
  stageLabel: string;
  moment: FeedbackMoment;
  observedEvidence: string;
  feedbackStem: string;
  learnerAction: string;
  caution: string;
}

export const feedbackStageLabels: Record<FeedbackStage, string> = {
  'early-learning': 'Early learning', ks1: 'KS1', ks2: 'KS2', ks3: 'KS3', ks4: 'KS4', 'post16-adult': 'Post-16 / adult',
};

export const feedbackPlanningEntries: FeedbackPlanningEntry[] = [
  {
    id: 'english-claim-evidence-link', subject: 'English', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', moment: 'After an attempt',
    observedEvidence: 'The response states an interpretation and includes a relevant detail, but does not explain the connection.',
    feedbackStem: 'Your detail is relevant to the claim. The missing step is explaining what that detail lets the reader infer.',
    learnerAction: 'Add one sentence beginning with “This suggests...” and name the exact word or feature doing the work.',
    caution: 'Do not prescribe one interpretation when another can be supported explicitly by the text.',
  },
  {
    id: 'english-audience-revision', subject: 'English', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', moment: 'During revision',
    observedEvidence: 'The content is accurate, but the explanation assumes knowledge the stated audience may not have.',
    feedbackStem: 'The main idea is present. A reader new to the topic may not yet know why this term or step matters.',
    learnerAction: 'Choose one assumed term and add a short definition or concrete example before using it.',
    caution: 'Clarity does not require removing disciplinary precision or flattening the learner’s voice.',
  },
  {
    id: 'maths-method-trace', subject: 'Mathematics', stages: ['ks1', 'ks2', 'ks3'], stageLabel: 'KS1-KS3 lens', moment: 'During learning',
    observedEvidence: 'The final answer is given, but the representation does not show how the value was produced.',
    feedbackStem: 'I can see the result, but not yet the mathematical decision that produced it.',
    learnerAction: 'Show one intermediate representation, operation or equality that another learner could follow.',
    caution: 'Do not equate brevity with guessing when the learner can explain the method orally or with another representation.',
  },
  {
    id: 'maths-error-localisation', subject: 'Mathematics', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', moment: 'After an attempt',
    observedEvidence: 'A valid method begins correctly, then one transformation changes the value or relation.',
    feedbackStem: 'Your method is viable up to this transformation. Compare the expression immediately before and after it.',
    learnerAction: 'Annotate what operation was applied to each side or term, then repair only the first diverging step.',
    caution: 'Do not label the whole method wrong when the evidence localises one repairable step.',
  },
  {
    id: 'science-observation-language', subject: 'Science', stages: ['early-learning', 'ks1', 'ks2'], stageLabel: 'Early learning-KS2 lens', moment: 'During learning',
    observedEvidence: 'A direct observation and an explanation are blended into one statement.',
    feedbackStem: 'You have noticed something and proposed why it happened. Let us separate those two useful ideas.',
    learnerAction: 'First say only what could be seen, heard or measured; then add “I think this happened because...”',
    caution: 'Do not treat an age-appropriate causal idea as a failure merely because it is not yet formal scientific language.',
  },
  {
    id: 'science-claim-scope', subject: 'Science', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', moment: 'During revision',
    observedEvidence: 'The conclusion describes a broad rule from a limited sample or uncontrolled investigation.',
    feedbackStem: 'The data supports a pattern in this investigation. The present wording reaches beyond the evidence collected.',
    learnerAction: 'Rewrite the claim to name the sample, range or conditions, then identify one further test.',
    caution: 'Do not demand certainty where the discipline requires a bounded, evidence-sensitive conclusion.',
  },
  {
    id: 'history-source-use', subject: 'History and histories', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', moment: 'After an attempt',
    observedEvidence: 'The source is described accurately, but its relevance to the enquiry is not established.',
    feedbackStem: 'You have identified what the source shows. The next step is explaining how that helps answer this enquiry.',
    learnerAction: 'Link one feature of the source to the enquiry, then state one limit created by its provenance or scope.',
    caution: 'Do not reduce evaluation to reliable or unreliable; usefulness depends on the question being asked.',
  },
  {
    id: 'history-plural-experience', subject: 'History and histories', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', moment: 'During revision',
    observedEvidence: 'A supported account is presented as though it represents every group, place or point in the period.',
    feedbackStem: 'This claim is supported for the group you discuss. Its current wording makes the scope wider than the evidence.',
    learnerAction: 'Name the group, place and timescale in the claim, then compare one contrasting experience if evidence allows.',
    caution: 'Do not invent balance by adding an unevidenced “other side”; plurality still requires sources.',
  },
  {
    id: 'languages-retrieval-repair', subject: 'Languages', stages: ['ks1', 'ks2', 'ks3'], stageLabel: 'KS1-KS3 lens', moment: 'During learning',
    observedEvidence: 'The intended meaning is clear, but one retrieved form does not match the sentence context.',
    feedbackStem: 'Your message is understandable. This word needs to agree with or fit the surrounding sentence.',
    learnerAction: 'Use the model sentence to compare the changing part, then say or write the whole phrase again.',
    caution: 'Correct the target form without mocking accent, dialect, transfer, hesitation or an intelligible alternative.',
  },
  {
    id: 'languages-register-revision', subject: 'Languages', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', moment: 'During revision',
    observedEvidence: 'The language is grammatically plausible, but the register does not match the stated relationship or setting.',
    feedbackStem: 'The sentence communicates the idea. The audience and setting call for a different level of formality.',
    learnerAction: 'Replace one greeting, pronoun or request form and explain what changed in the relationship.',
    caution: 'Register varies across regions and communities; use current qualified language evidence rather than stereotypes.',
  },
  {
    id: 'computing-trace-first-divergence', subject: 'Computing and IT', stages: ['ks2', 'ks3', 'ks4'], stageLabel: 'KS2-KS4 lens', moment: 'After an attempt',
    observedEvidence: 'The reported symptom is accurate, but the explanation jumps directly to an untested cause.',
    feedbackStem: 'You have captured the visible failure. The cause is not proven yet.',
    learnerAction: 'Trace the state after each step and mark the first point where observed and expected behaviour differ.',
    caution: 'Do not reward confident diagnosis more than reproducible evidence.',
  },
  {
    id: 'computing-boundary-test', subject: 'Computing and IT', stages: ['ks3', 'ks4', 'post16-adult'], stageLabel: 'KS3-adult lens', moment: 'During revision',
    observedEvidence: 'The solution works for the demonstrated input but its assumptions and limits are not visible.',
    feedbackStem: 'This example demonstrates one successful path. It does not yet show how the solution behaves at its boundaries.',
    learnerAction: 'Name one assumption and add a smallest, largest, empty or malformed input that tests it.',
    caution: 'A new test result is evidence about that case, not automatic proof of all possible inputs.',
  },
  {
    id: 'early-english-retell-sequence', subject: 'English', stages: ['early-learning'], stageLabel: 'Early-learning lens', moment: 'During learning',
    observedEvidence: 'The learner communicates one story event but the relationship to another event is not yet visible.',
    feedbackStem: 'You showed me this part of the story. I want to see what came just before or after it.',
    learnerAction: 'Choose, move, draw, act, say or sign one connected event and place the two in order.',
    caution: 'Accept multimodal retelling and adult-supported communication; do not grade memory speed, speech fluency or performance confidence.',
  },
  {
    id: 'early-maths-visible-rule', subject: 'Mathematics', stages: ['early-learning'], stageLabel: 'Early-learning lens', moment: 'After an attempt',
    observedEvidence: 'Objects have been grouped, but the property used for grouping is not yet observable to another person.',
    feedbackStem: 'You made groups. Show me the part that makes the objects in this group belong together.',
    learnerAction: 'Point to, name or demonstrate one shared property, then try the same rule with one new object.',
    caution: 'A surprising rule can still be mathematically consistent; establish the learner\'s rule before correcting the grouping.',
  },
  {
    id: 'early-history-sequence-clue', subject: 'History and histories', stages: ['early-learning'], stageLabel: 'Early-learning lens', moment: 'During learning',
    observedEvidence: 'Two events or objects are placed in an order, but no temporal clue has been communicated.',
    feedbackStem: 'You chose an order. Show me the clue that tells us which one belongs first.',
    learnerAction: 'Point to, describe, act or match one clue, then decide whether the order should stay or change.',
    caution: 'Use non-sensitive shared stories or routines and avoid assumptions about homes, families, possessions or remembered events.',
  },
  {
    id: 'early-languages-meaningful-use', subject: 'Languages', stages: ['early-learning'], stageLabel: 'Early-learning lens', moment: 'During learning',
    observedEvidence: 'The learner associates a word, phrase, sign or sound pattern with the intended object or action in one context.',
    feedbackStem: 'You connected this language with the meaning here. Let us try it when the picture, person or place changes.',
    learnerAction: 'Choose or create one new situation and communicate the same meaning using an available mode.',
    caution: 'Do not make accent imitation, eye contact, speech or one prestige variety the condition for successful meaning.',
  },
  {
    id: 'early-computing-local-repair', subject: 'Computing and IT', stages: ['early-learning'], stageLabel: 'Early-learning lens', moment: 'After an attempt',
    observedEvidence: 'A physical sequence begins as intended and then produces a different position or action at one step.',
    feedbackStem: 'The first step did what you planned. This is the first place where the movement changed.',
    learnerAction: 'Replay only that step, then swap, remove or replace one instruction and observe what happens.',
    caution: 'Do not label the whole sequence wrong or treat device control, reading, speed or fine motor skill as the learning goal.',
  },
  {
    id: 'geography-pattern-evidence-link', subject: 'Geography and environment', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'After an attempt',
    observedEvidence: 'A place, feature or spatial pattern is identified, but the proposed explanation is not yet connected to the map, observation or data used.',
    feedbackStem: 'You have shown where the pattern appears. The next step is linking one piece of place evidence to one possible reason for it.',
    learnerAction: 'Point to or annotate one location, observation or data value, then explain what it supports and compare another place or scale.',
    caution: 'Adapt the response mode to the learner; one place or correlation must not be presented as a universal cause.',
  },
  {
    id: 'art-choice-process-link', subject: 'Art and design', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'During revision',
    observedEvidence: 'A purposeful visual or material choice is present in the work, but its relationship to the intended idea, audience or effect is not yet explained.',
    feedbackStem: 'This choice is visible in the work. Help the viewer understand what you wanted it to communicate or change.',
    learnerAction: 'Compare one trial with the current work, then name or demonstrate one choice you kept, changed or would test next.',
    caution: 'Do not turn personal taste, neatness, expensive materials, drawing fluency or one cultural style into a quality score.',
  },
  {
    id: 'music-rehearsal-first-change', subject: 'Music and performance', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'During learning',
    observedEvidence: 'A phrase, rhythm, movement or ensemble part begins as intended, then timing, pitch, sequence or coordination changes at an observable point.',
    feedbackStem: 'The opening matches your plan. This short part is the first place where the sound or movement changes.',
    learnerAction: 'Listen to, mark, rehearse or perform only that short part, then join it back to the section before it.',
    caution: 'Do not confuse confidence, volume, solo performance, notation reading or one performance tradition with musical understanding.',
  },
  {
    id: 'pe-focused-movement-change', subject: 'Physical education', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'During learning',
    observedEvidence: 'The intended movement or decision is attempted safely, and one observable phase affects balance, control, space, timing or accuracy.',
    feedbackStem: 'The whole action is underway. We will change one small part so you can notice what it does.',
    learnerAction: 'Choose one safe cue, repeat at a suitable pace, then describe or show what changed before adding another cue.',
    caution: 'Do not judge body shape, fitness, speed, confidence or competition result as the learning outcome; stop for pain or safety concerns.',
  },
  {
    id: 'citizenship-claim-source-check', subject: 'Citizenship and media literacy', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'After an attempt',
    observedEvidence: 'A claim or viewpoint is repeated accurately, but its author, evidence, date, purpose or missing context has not yet been considered.',
    feedbackStem: 'You have captured what the source says. We still need to check what makes it useful for this exact claim.',
    learnerAction: 'Identify one source feature and one piece of supporting evidence, then name one question or missing context before deciding what follows.',
    caution: 'Do not demand personal political beliefs, reward agreement, use unsafe live searches or create false balance between unequal evidence.',
  },
  {
    id: 'wellbeing-scenario-safety-step', subject: 'Health and wellbeing', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'After an attempt',
    observedEvidence: 'A safe action is suggested for a fictional scenario, but the point for stopping, seeking help or involving a trusted adult is not yet explicit.',
    feedbackStem: 'This is one sensible action for the scenario. Add how someone would know they need more help and who should help them.',
    learnerAction: 'Name or choose one warning sign, one safe next action and one suitable trusted adult, service or emergency route.',
    caution: 'Keep examples fictional and age-appropriate; this is education, not diagnosis, counselling, emergency response or a request for private disclosure.',
  },
  {
    id: 'money-total-cost-comparison', subject: 'Money and life skills', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'During revision',
    observedEvidence: 'An option is chosen using one visible price or feature, but totals, units, dates, repeat costs or stated constraints are not compared consistently.',
    feedbackStem: 'Your choice uses one relevant number. To compare fairly, both options need the same units and the full stated cost.',
    learnerAction: 'Build a small side-by-side comparison with fictional values, show the total for the same period, then explain the trade-off.',
    caution: 'Use invented account data and age-appropriate examples; this is not personal financial advice and cheapest is not automatically best.',
  },
  {
    id: 'belief-account-scope', subject: 'Religion, belief and philosophy', stages: ['early-learning', 'ks1', 'ks2', 'ks3', 'ks4', 'post16-adult'], stageLabel: 'All-stage planning lens', moment: 'During revision',
    observedEvidence: 'A belief, practice or reason is described from one account, but the wording presents it as uniform across every person, place or tradition.',
    feedbackStem: 'This account supports what you wrote for this context. The wording currently makes the claim wider than that evidence.',
    learnerAction: 'Name the source or context, qualify the claim, then compare another accurate account only when suitable evidence is available.',
    caution: 'Do not demand or grade personal belief, imply a tradition is uniform, or treat respectful disagreement as permission to ignore evidence.',
  },
];
