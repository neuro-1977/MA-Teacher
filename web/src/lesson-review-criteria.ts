export const lessonReviewCategories = [
  {
    id: 'provenance',
    label: 'Curriculum provenance',
    purpose: 'Confirm every curriculum claim is linked to the intended accepted evidence and historical source version.',
    criteria: [
      { id: 'evidence-linked', question: 'Does each curriculum-dependent claim link to accepted evidence?', evidenceNeeded: 'Exact candidate, source version, subject, stage, and accepted disposition.', stopWhen: 'A claim is uncited, rejected, superseded without review, or linked to another stage.' },
      { id: 'source-context', question: 'Was the source read in enough context to preserve its meaning?', evidenceNeeded: 'Surrounding section, publication identity, retrieval version, and any qualification or scope note.', stopWhen: 'Extraction fragments remove conditions, exceptions, definitions, or intended audience.' },
      { id: 'coverage-honest', question: 'Is the curriculum lane genuinely configured for this lesson?', evidenceNeeded: 'Coverage state showing configured-partial or stronger support for the exact jurisdiction and stage.', stopWhen: 'A reference-only or unsupported lane is presented as supported.' },
    ],
  },
  {
    id: 'intention',
    label: 'Learning intention',
    purpose: 'Keep the lesson narrow enough to teach and inspect without hiding several unrelated goals.',
    criteria: [
      { id: 'goal-specific', question: 'Is the intended learning specific and observable?', evidenceNeeded: 'One bounded statement naming the knowledge, process, or communication the learner should demonstrate.', stopWhen: 'The goal is a topic label, activity description, vague improvement, or personality judgement.' },
      { id: 'prerequisites-explicit', question: 'Are necessary prerequisites identified without diagnosing the learner?', evidenceNeeded: 'A short list of knowledge or representations the lesson actually uses.', stopWhen: 'The plan assumes broad cultural knowledge or labels missing knowledge as low ability.' },
      { id: 'activity-aligned', question: 'Does each activity contribute to the stated intention?', evidenceNeeded: 'A visible connection between model, practice, response, and intended evidence.', stopWhen: 'Entertainment, decoration, reading load, or tool use replaces the intended subject thinking.' },
    ],
  },
  {
    id: 'discipline',
    label: 'Subject integrity',
    purpose: 'Preserve the concepts, methods, evidence, and language that make the subject distinct.',
    criteria: [
      { id: 'content-accurate', question: 'Is the content accurate within the lesson scope?', evidenceNeeded: 'Current authoritative or reviewed subject evidence and a human check of examples and explanations.', stopWhen: 'An example contradicts the explanation, uses obsolete facts without context, or invents certainty.' },
      { id: 'disciplinary-action', question: 'Does the learner perform the intended disciplinary action?', evidenceNeeded: 'A task requiring relevant reading, reasoning, calculation, investigation, interpretation, communication, or debugging.', stopWhen: 'The task only copies, decorates, guesses, or recalls when deeper action is claimed.' },
      { id: 'vocabulary-meaningful', question: 'Is subject vocabulary taught and used in context?', evidenceNeeded: 'Definitions, examples, contrasts, notation or pronunciation as relevant, and meaningful use.', stopWhen: 'Terminology is presented as an unexplained list or used inaccurately.' },
    ],
  },
  {
    id: 'sequence',
    label: 'Teaching sequence',
    purpose: 'Make explanation, modelling, guided practice, independence, and review coherent.',
    criteria: [
      { id: 'model-visible', question: 'Is unfamiliar thinking or process made visible before independence?', evidenceNeeded: 'A worked example, think-aloud, demonstration, trace, model, or source analysis appropriate to the subject.', stopWhen: 'The learner is asked to infer an unseen process from instructions alone.' },
      { id: 'practice-progresses', question: 'Does practice change support or complexity deliberately?', evidenceNeeded: 'A sequence showing what remains constant, what changes, and when support is removed.', stopWhen: 'Difficulty jumps through several new demands or repeats identical surface copying.' },
      { id: 'misconceptions-bounded', question: 'Are likely confusions addressed without scripting learner failure?', evidenceNeeded: 'Examples, non-examples, contrasts, or checks tied to the taught concept.', stopWhen: 'The plan predicts diagnosis, motivation, or fixed ability from an error.' },
    ],
  },
  {
    id: 'access',
    label: 'Access and dignity',
    purpose: 'Make the intended learning reachable while respecting age, identity, privacy, and agency.',
    criteria: [
      { id: 'age-respectful', question: 'Is presentation respectful for the learner regardless of content level?', evidenceNeeded: 'Language, examples, imagery, interaction, and pacing reviewed for the intended audience.', stopWhen: 'Foundational content is presented childishly to an older learner.' },
      { id: 'demand-separated', question: 'Are reading, writing, memory, motor, and technology demands distinguished from the goal?', evidenceNeeded: 'Alternative response or representation options that preserve intended subject evidence.', stopWhen: 'An incidental demand becomes the unacknowledged reason for failure.' },
      { id: 'support-removable', question: 'Can support be used and reduced without changing learner identity?', evidenceNeeded: 'Optional prompts, examples, frames, tools, or representations with a clear purpose.', stopWhen: 'Support is socially punitive, permanent by assumption, or recorded as diagnosis.' },
    ],
  },
  {
    id: 'assessment',
    label: 'Check and feedback',
    purpose: 'Collect inspectable evidence aligned with the lesson and provide bounded human feedback.',
    criteria: [
      { id: 'prompt-aligned', question: 'Does the check ask for the intended knowledge or action?', evidenceNeeded: 'One bounded prompt linked to lesson evidence and observable success criteria.', stopWhen: 'The prompt mainly tests unrelated reading, background knowledge, speed, or presentation.' },
      { id: 'criteria-observable', question: 'Can a reviewer point to evidence for each criterion?', evidenceNeeded: 'Specific features visible in the response, performance, method, or product.', stopWhen: 'Criteria rely on impressions such as clever, confident, talented, or trying hard.' },
      { id: 'feedback-bounded', question: 'Will feedback address this response and the next evidence needed?', evidenceNeeded: 'A human review tied to the attempt and saved criteria.', stopWhen: 'Feedback predicts attainment, assigns fixed ability, or claims broad mastery.' },
    ],
  },
  {
    id: 'safety',
    label: 'Safety and privacy',
    purpose: 'Prevent a teaching activity from soliciting unsafe action or unnecessary personal data.',
    criteria: [
      { id: 'activity-safe', question: 'Are practical, online, physical, and technical actions safe for the intended setting?', evidenceNeeded: 'Relevant adult supervision, equipment, environmental, account, data, and stop conditions.', stopWhen: 'The activity exposes credentials, personal data, unsafe materials, uncontrolled physical risk, or unauthorized systems.' },
      { id: 'data-minimal', question: 'Does the lesson avoid unnecessary learner-identifying or sensitive information?', evidenceNeeded: 'Prompt and expected response inspected for identity, health, family, location, finance, immigration, and safeguarding content.', stopWhen: 'The task asks for information not required to demonstrate the intended learning.' },
      { id: 'disclosure-route', question: 'Is the lesson clearly outside emergency and safeguarding reporting?', evidenceNeeded: 'Prompt wording that avoids soliciting urgent disclosure and a known responsible-human process outside MA-Teacher.', stopWhen: 'The activity could be mistaken for an emergency, counselling, or formal reporting channel.' },
    ],
  },
  {
    id: 'delivery',
    label: 'Delivery readiness',
    purpose: 'Check the actual lesson view and materials without confusing source completion with usable delivery.',
    criteria: [
      { id: 'reader-complete', question: 'Does the lesson reader show the complete intended content and provenance?', evidenceNeeded: 'Rendered lesson, section order, linked evidence, and absence of stale or missing content.', stopWhen: 'Source text exists but the delivered view is blank, truncated, duplicated, or stale.' },
      { id: 'interaction-usable', question: 'Can the intended operator and learner use the lesson controls?', evidenceNeeded: 'Keyboard, pointer, zoom, narrow-screen, error, and relevant assistive-technology observation.', stopWhen: 'A required action cannot be reached, understood, completed, or recovered.' },
      { id: 'derivative-reviewed', question: 'If printed or exported, was the derivative inspected separately?', evidenceNeeded: 'Current print/PDF preview checked for content, page order, privacy, and readability.', stopWhen: 'A source or screen success is used to claim paper/PDF success without inspection.' },
    ],
  },
] as const;

export type LessonReviewCategory = typeof lessonReviewCategories[number];
export type LessonReviewCategoryId = LessonReviewCategory['id'];
export type LessonReviewCriterion = LessonReviewCategory['criteria'][number];
