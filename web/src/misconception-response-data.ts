export type LearningErrorHypothesis = {
  id: string;
  label: string;
  description: string;
  evidenceQuestions: readonly string[];
  usefulResponses: readonly string[];
  disconfirmingEvidence: readonly string[];
};

export const learningErrorHypotheses: readonly LearningErrorHypothesis[] = [
  {
    id: 'slip-or-lapse',
    label: 'Slip or lapse',
    description: 'The learner may understand the idea but have made an execution, attention, transcription, or memory slip.',
    evidenceQuestions: [
      'Can the learner notice and correct the response without being told the answer?',
      'Does the same error recur across equivalent examples and representations?',
      'Can the learner explain the correct principle despite the incorrect output?',
    ],
    usefulResponses: [
      'Invite a deliberate check against the success condition.',
      'Reduce avoidable execution load, then repeat one comparable item.',
      'Teach a self-check routine if the pattern recurs.',
    ],
    disconfirmingEvidence: [
      'The learner gives the same underlying explanation across varied examples.',
      'The learner cannot distinguish correct and incorrect worked cases.',
    ],
  },
  {
    id: 'retrieval-gap',
    label: 'Retrieval gap',
    description: 'Relevant knowledge may have been learned but is not currently retrievable with sufficient fluency.',
    evidenceQuestions: [
      'Does a small cue restore accurate recall without teaching the answer again?',
      'Was the knowledge previously demonstrated after a delay?',
      'Can recognition succeed while unaided recall fails?',
    ],
    usefulResponses: [
      'Use brief successful retrieval with gradually reduced cues.',
      'Revisit after spacing rather than massing many identical items.',
      'Connect the retrieved fact to its meaning and use, not only its wording.',
    ],
    disconfirmingEvidence: [
      'The learner recalls the fact but applies an incorrect relationship.',
      'Additional cues reproduce the same conceptual error.',
    ],
  },
  {
    id: 'missing-prerequisite',
    label: 'Missing prerequisite',
    description: 'A required earlier concept, representation, vocabulary item, or procedure may not be secure.',
    evidenceQuestions: [
      'Which exact prerequisite does this step depend on?',
      'Can the learner complete a simpler task that isolates that prerequisite?',
      'Is the assumed prerequisite part of this learner’s actual prior experience?',
    ],
    usefulResponses: [
      'Teach the smallest missing prerequisite explicitly.',
      'Reconnect it to the current objective immediately after success.',
      'Record the gap as lesson-planning evidence, not a learner identity.',
    ],
    disconfirmingEvidence: [
      'The prerequisite is accurate and fluent in an equivalent context.',
      'The error persists when the prerequisite is supplied correctly.',
    ],
  },
  {
    id: 'conceptual-model',
    label: 'Competing conceptual model',
    description: 'The learner may be reasoning consistently from an incorrect or overgeneralised model.',
    evidenceQuestions: [
      'Can the learner predict what should happen and explain why?',
      'Where does the proposed rule work, and where does it fail?',
      'Does the same explanation transfer across changed surface features?',
    ],
    usefulResponses: [
      'Compare carefully chosen cases that differ in one important feature.',
      'Use a counterexample the current model cannot explain, then rebuild the relationship.',
      'Ask the learner to contrast the old and revised explanation before reapplying it.',
    ],
    disconfirmingEvidence: [
      'The error disappears when language, notation, or task wording changes.',
      'The learner’s explanation is correct and the failure is limited to execution.',
    ],
  },
  {
    id: 'language-or-notation',
    label: 'Language or notation barrier',
    description: 'Vocabulary, syntax, symbols, representation conventions, or task wording may obscure an understood idea.',
    evidenceQuestions: [
      'Can the learner show the idea through another language, diagram, object, action, or example?',
      'Which specific word, symbol, or sentence changes the interpretation?',
      'Is the notation itself part of the intended learning?',
    ],
    usefulResponses: [
      'Clarify essential vocabulary and notation with examples and non-examples.',
      'Offer an equivalent response route when language or notation is not the objective.',
      'Return to the original form after meaning is secure when that form must be learned.',
    ],
    disconfirmingEvidence: [
      'The same reasoning error appears in a low-language or alternative representation.',
      'The learner understands every term but predicts an incorrect relationship.',
    ],
  },
  {
    id: 'task-interpretation',
    label: 'Task interpretation mismatch',
    description: 'The learner may be answering a different reasonable question or following a misunderstood instruction.',
    evidenceQuestions: [
      'What does the learner think the task is asking them to produce?',
      'Can they restate the goal and success condition in their own words?',
      'Does an example of the expected response clarify the task without revealing its answer?',
    ],
    usefulResponses: [
      'Repair the instruction or example, then obtain fresh evidence.',
      'Separate task literacy from subject understanding in the record.',
      'Revise ambiguous wording for future learners.',
    ],
    disconfirmingEvidence: [
      'The learner states the intended task accurately but retains the same reasoning.',
      'The error recurs in a differently worded equivalent task.',
    ],
  },
  {
    id: 'access-barrier',
    label: 'Access barrier',
    description: 'Perception, layout, timing, interaction, sensory, motor, attention, or response demands may block valid evidence.',
    evidenceQuestions: [
      'Can the learner perceive and operate every essential part of the task?',
      'Does an equivalent accessible route change the observed result?',
      'Is the blocked access route itself the objective being assessed?',
    ],
    usefulResponses: [
      'Remove the unrelated access barrier while preserving the objective.',
      'Record the response route and assistance needed to interpret the evidence.',
      'Consult the learner and responsible humans rather than inferring a support need.',
    ],
    disconfirmingEvidence: [
      'The same explanation and error remain after access is established.',
      'The learner confirms the original route was fully accessible.',
    ],
  },
] as const;

export const misconceptionInvestigationSequence = [
  'Capture the exact response, prompt, representation, assistance, and context without correcting it.',
  'Ask the learner to explain or demonstrate the reasoning in an available form.',
  'Test at least one plausible competing explanation with a small contrasting example.',
  'Choose the smallest instructional response supported by the evidence.',
  'Recheck a near example, then a changed representation or context.',
  'Record what the evidence supports and what remains unknown; do not attach a fixed label to the learner.',
] as const;
