# MA-Teacher Product Contract

## Status

Version 0.1.0 is an installable desktop foundation. It proves that MA-Teacher can
be built, installed, launched, and rendered as an independent local application.
It does not prove teaching quality, curriculum coverage, safeguarding, learner
records, assessment, or AI tutoring.

## Purpose

MA-Teacher should help people learn and help adults support learning without
pretending confidence is correctness. The product is intended for all age groups,
with age-appropriate language and presentation rather than one generic answer for
every learner.

Initial subject breadth: science, English, maths, history and histories,
languages, and information technology and computing.

## Product roles

- **Learner:** understandable goals, explanations, practice, feedback, and safe
  next actions suited to the learner's stage.
- **Teacher or supporting adult:** curriculum intent, source provenance, planning
  context, misconceptions, progress evidence, and control over learner-facing use.

Role boundaries must remain explicit. A learner surface may not silently expose
adult controls or infer authority from shared-device access.

## Curriculum evidence model

1. Identify country, curriculum, stage, subject, and effective date.
2. Prefer the current official government curriculum publication.
3. Use official exam-board or awarding-body material only for its actual scope.
4. Record provenance and retrieval date for every mapped objective or lesson.
5. Distinguish quoted requirements, teaching interpretation, and generated practice material.
6. Reconcile contradictions before presenting content as authoritative.
7. Mark unavailable or uncertain coverage honestly rather than filling gaps by invention.

The English National Curriculum is the first intended lane, not a permanent
geographic limit. Adding another curriculum requires a separately identified and
versioned evidence map.

## Safety and data boundaries

- No pupil account, personal record, classroom roster, assessment history, or
  safeguarding data exists in 0.1.0.
- Future learner data remains local-first by default and needs explicit retention,
  export, deletion, consent, and adult-control contracts.
- Generated teaching material is not automatically correct because a model wrote
  it. Source retrieval, reasoning, checking, and evidence-backed tests are part of
  the future workflow.
- MA-Teacher must never claim official endorsement merely because it maps official
  curriculum material.

## Architecture boundary

MA-Teacher is a separate repository and runnable product. It may integrate with
Mostly Armless later through explicit, versioned interfaces, but 0.1.0 does not
require MA-Dev, Serenity, a browser agent, a model server, or a cloud account.

The application serves its packaged interface on loopback port 5201 and renders it
inside WebView2. Runtime WebView2 data stays beneath the chosen install root. The
installer creates no service, scheduler, startup entry, firewall rule, or remote listener.

## Next implementation gates

1. Select the first age/stage and lesson workflow to implement end to end.
2. Establish the canonical curriculum-source ingestion and revision model.
3. Define learner/adult roles, safeguarding, and local data ownership.
4. Build source-grounded retrieval and answer verification before AI tutoring.
5. Prove one subject workflow with real users before broadening the surface.
