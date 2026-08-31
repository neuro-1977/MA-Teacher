export type InclusivePlanningCategory =
  | 'Language'
  | 'Reading'
  | 'Sensory access'
  | 'Attention and structure'
  | 'Response access'
  | 'Prior knowledge'
  | 'Challenge'
  | 'Safety';

export type InclusivePlanningLens = {
  id: string;
  category: InclusivePlanningCategory;
  title: string;
  askFirst: readonly string[];
  planningMoves: readonly string[];
  evidenceToCollect: readonly string[];
  neverInfer: string;
};

export const inclusivePlanningLenses: readonly InclusivePlanningLens[] = [
  {
    id: 'language-access',
    category: 'Language',
    title: 'Language and vocabulary access',
    askFirst: [
      'Which languages and communication forms does the learner use confidently?',
      'Is subject vocabulary, sentence structure, or the underlying concept causing the difficulty?',
    ],
    planningMoves: [
      'Pre-teach a small set of essential words with examples, images, and non-examples.',
      'Allow rehearsal, bilingual resources, glossaries, sentence frames, and additional processing time where useful.',
      'Keep the disciplinary idea intact while reducing avoidable language complexity.',
    ],
    evidenceToCollect: [
      'Explanation of the concept in the learner’s preferred available form.',
      'Use of new vocabulary across more than one example.',
    ],
    neverInfer: 'Accent, multilingualism, quietness, or limited English output does not establish low understanding.',
  },
  {
    id: 'reading-access',
    category: 'Reading',
    title: 'Reading, decoding, and text load',
    askFirst: [
      'Does the task assess reading itself, or is reading only the route into another subject?',
      'Which text features create load: decoding, vocabulary, density, layout, or background knowledge?',
    ],
    planningMoves: [
      'Separate the reading objective from the subject objective before changing the material.',
      'Offer chunked text, readable spacing, headings, audio, read-aloud, or a glossary when those do not invalidate the objective.',
      'Retain access to age-respectful ideas even when the reading route needs support.',
    ],
    evidenceToCollect: [
      'Independent understanding after an accessible presentation of the same content.',
      'Specific decoding or comprehension evidence when reading is the assessed objective.',
    ],
    neverInfer: 'A reading difficulty does not establish weak reasoning, limited subject knowledge, or a younger intellectual level.',
  },
  {
    id: 'sensory-format-access',
    category: 'Sensory access',
    title: 'Visual, auditory, and format access',
    askFirst: [
      'Can the learner perceive every instruction, representation, label, cue, and response from their working position?',
      'Which formats or assistive tools does the learner already use successfully?',
    ],
    planningMoves: [
      'Provide equivalent text, spoken, tactile, captioned, described, enlarged, or high-contrast routes where appropriate.',
      'Do not encode essential meaning in colour, sound, position, or gesture alone.',
      'Check diagrams, tables, equations, maps, and demonstrations independently rather than assuming one format fixes all access.',
    ],
    evidenceToCollect: [
      'Learner confirmation that the material and controls are perceivable.',
      'Task performance after format access is established, recorded separately from the access barrier.',
    ],
    neverInfer: 'A declared condition does not identify the exact format, contrast, volume, device, or assistance an individual needs.',
  },
  {
    id: 'attention-structure',
    category: 'Attention and structure',
    title: 'Attention, memory, and task structure',
    askFirst: [
      'How many instructions, representations, and decisions must be held at once?',
      'Is the learner losing the concept, the sequence, the current step, or the reason for the task?',
    ],
    planningMoves: [
      'Make the goal, current step, success evidence, and stopping point visible.',
      'Chunk long procedures without fragmenting the conceptual relationship between steps.',
      'Use worked examples, retrieval cues, checklists, and deliberate pauses instead of repeated verbal prompting.',
    ],
    evidenceToCollect: [
      'Independent completion with the structure still visible.',
      'Transfer to a similar task with fewer prompts after practice.',
    ],
    neverInfer: 'Inattention, movement, delay, or an unfinished task does not by itself establish motivation, diagnosis, or ability.',
  },
  {
    id: 'response-access',
    category: 'Response access',
    title: 'Motor, speech, writing, and response routes',
    askFirst: [
      'Which part of the response demonstrates the intended learning, and which part is merely the chosen output method?',
      'Can the learner reliably use the controls, tools, timing, and physical setup?',
    ],
    planningMoves: [
      'Permit an equivalent spoken, typed, selected, demonstrated, constructed, recorded, or assisted response when the output mode is not the objective.',
      'Preserve writing, pronunciation, practical, or motor requirements when those are explicitly being taught, while providing access around unrelated barriers.',
      'Record assistance and response mode so later evidence is interpretable.',
    ],
    evidenceToCollect: [
      'A response tied directly to the stated learning objective.',
      'Separate evidence for output fluency when the output method is itself assessed.',
    ],
    neverInfer: 'Speech, handwriting, motor speed, eye contact, or interface speed does not independently establish conceptual understanding.',
  },
  {
    id: 'prior-knowledge-context',
    category: 'Prior knowledge',
    title: 'Prior knowledge and context',
    askFirst: [
      'Which prerequisite concepts and experiences does this lesson assume?',
      'Which examples depend on local, cultural, household, technological, or historical familiarity?',
    ],
    planningMoves: [
      'Elicit prerequisite knowledge with low-stakes questions before introducing new content.',
      'Teach missing prerequisites explicitly rather than labelling the learner by the gap.',
      'Use more than one context and explain culturally specific references when they are not part of the objective.',
    ],
    evidenceToCollect: [
      'A prerequisite map showing known, uncertain, and newly taught ideas.',
      'Application of the new idea in both familiar and unfamiliar contexts.',
    ],
    neverInfer: 'Age, school year, nationality, household, or previous course title does not prove that a prerequisite is secure.',
  },
  {
    id: 'challenge-depth',
    category: 'Challenge',
    title: 'Depth, stretch, and high prior attainment',
    askFirst: [
      'Is the learner already fluent, or merely fast on a familiar surface task?',
      'Would greater depth, connection, proof, critique, or transfer be more valuable than more of the same work?',
    ],
    planningMoves: [
      'Increase conceptual depth, independence, uncertainty, comparison, or transfer before increasing workload.',
      'Invite justification, alternative methods, boundary cases, error analysis, and creation of examples.',
      'Do not use advanced content as a reward that leaves foundational gaps unexamined.',
    ],
    evidenceToCollect: [
      'Reasoning across unfamiliar examples or competing representations.',
      'Explanations of why a method works, fails, or needs a stated condition.',
    ],
    neverInfer: 'Speed, confidence, vocabulary, or one high score does not establish complete mastery or a fixed ability label.',
  },
  {
    id: 'emotional-content-safety',
    category: 'Safety',
    title: 'Emotional safety and sensitive content',
    askFirst: [
      'Could the topic, example, media, disclosure request, competition, or public response create avoidable distress or exposure?',
      'What choice, preview, private response route, pause, or alternative preserves the learning objective?',
    ],
    planningMoves: [
      'Preview sensitive material and explain why it is present before exposure.',
      'Avoid requiring personal disclosure as proof of learning.',
      'Provide a dignified pause or alternative route and escalate safeguarding concerns through the responsible human process.',
    ],
    evidenceToCollect: [
      'Evidence about the academic objective without storing unnecessary personal disclosure.',
      'A bounded record of any human safety decision, following the applicable policy.',
    ],
    neverInfer: 'A learner’s reaction, silence, humour, refusal, or disclosure must not be diagnosed or managed by MA-Teacher.',
  },
] as const;
