import { useMemo, useState } from 'react';
import { feedbackPlanningEntries } from './feedback-planning-data';
import { questioningPlanningEntries } from './questioning-planning-data';
import { stageLenses } from './stage-lenses';
import { subjectLenses } from './subject-lenses';
import { teachingDataDraftContracts, type TeachingDataDraftContract } from './teaching-data-draft-contracts';
import type { TeachingBankId } from './teaching-bank-authoring-requirements';
import { vocabularyEntries } from './vocabulary-planning-data';
import { workedExamples } from './worked-examples';
import './teaching-data-draft-validator.css';

type ValidationResult = { state: 'empty' | 'invalid-json' | 'structural-errors' | 'structurally-complete'; errors: string[]; warnings: string[]; draftId: string | null };

const currentIds: Record<TeachingBankId, ReadonlySet<string>> = {
  vocabulary: new Set(vocabularyEntries.map((item) => item.id)),
  questioning: new Set(questioningPlanningEntries.map((item) => item.id)),
  feedback: new Set(feedbackPlanningEntries.map((item) => item.id)),
  'worked-example': new Set(workedExamples.map((item) => item.id)),
};
const allowedSubjects = new Set<string>(subjectLenses.map((item) => item.label));
const allowedStages = new Set<string>(stageLenses.map((item) => item.id));

export function TeachingDataDraftValidatorPanel() {
  const [bank, setBank] = useState<TeachingBankId>('vocabulary');
  const [draftText, setDraftText] = useState('');
  const [copyState, setCopyState] = useState<'idle' | 'copied' | 'failed'>('idle');
  const contract = teachingDataDraftContracts.find((item) => item.id === bank)!;
  const result = useMemo(() => validateDraft(draftText, contract), [contract, draftText]);

  function loadSkeleton() {
    const skeleton: Record<string, unknown> = Object.fromEntries(contract.requiredStringFields.map((field) => [field, '']));
    skeleton[contract.stageField] = contract.stageField === 'stages' ? [] : '';
    for (const field of contract.requiredListFields) skeleton[field] = [];
    setDraftText(JSON.stringify(skeleton, null, 2));
    setCopyState('idle');
  }

  async function copyFindings() {
    if (!navigator.clipboard) return setCopyState('failed');
    const receipt = [
      'MA-TEACHER / TEACHING-DATA DRAFT STRUCTURAL PREFLIGHT',
      `Bank: ${contract.label}`,
      `Draft id: ${result.draftId ?? '[unavailable]'}`,
      `State: ${result.state}`,
      '',
      'Errors:',
      ...(result.errors.length ? result.errors.map((item) => `- ${item}`) : ['- none']),
      '',
      'Warnings:',
      ...(result.warnings.length ? result.warnings.map((item) => `- ${item}`) : ['- none']),
      '',
      'Boundary: structural preflight only; no source write, review, approval, correctness, suitability, rights, curriculum, accessibility, learner or runtime evidence.',
    ].join('\n');
    try { await navigator.clipboard.writeText(receipt); setCopyState('copied'); }
    catch { setCopyState('failed'); }
  }

  return (
    <section className="draft-validator" id="workspace-draft-validator" aria-labelledby="draft-validator-title">
      <header><div><p>Local structural preflight</p><h2 id="draft-validator-title">Teaching-data draft validator</h2><span>Catch malformed contribution records before a deliberate source edit; a passing structure is not approved teaching data.</span></div><strong data-state={result.state}>{result.state.replaceAll('-', ' ').toUpperCase()}</strong></header>
      <aside className="draft-validator__boundary" role="note"><b>Paste no learner or personal data.</b> The draft remains in browser memory. MA-Teacher does not submit it, repair it, add it to source, reconcile counts, or judge its educational quality.</aside>
      <div className="draft-validator__controls"><label>Draft bank<select value={bank} onChange={(event) => { setBank(event.target.value as TeachingBankId); setDraftText(''); setCopyState('idle'); }}>{teachingDataDraftContracts.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label><button type="button" onClick={loadSkeleton}>Load empty schema</button><button type="button" className="draft-validator__clear" onClick={() => { setDraftText(''); setCopyState('idle'); }}>Clear local draft</button></div>
      <label className="draft-validator__editor">One JSON object / maximum 12,000 characters<textarea value={draftText} maxLength={12000} rows={18} spellCheck={false} onChange={(event) => { setDraftText(event.target.value); setCopyState('idle'); }} placeholder={`Paste one ${contract.label} draft object here.`} /></label>
      <div className="draft-validator__results" aria-live="polite">
        <article><h3>Structural errors / {result.errors.length}</h3>{result.errors.length === 0 ? <p>None detected by this bounded structural check.</p> : <ul>{result.errors.map((item) => <li key={item}>{item}</li>)}</ul>}</article>
        <article><h3>Warnings / {result.warnings.length}</h3>{result.warnings.length === 0 ? <p>None detected. Human review is still required.</p> : <ul>{result.warnings.map((item) => <li key={item}>{item}</li>)}</ul>}</article>
      </div>
      {result.state === 'structurally-complete' && <p className="draft-validator__pass"><b>Structurally complete only.</b> Continue with subject, stage, pedagogy, rights, accessibility, dignity, source-count, build and human review before any source contribution.</p>}
      <footer><button type="button" onClick={copyFindings} disabled={result.state === 'empty'}>Copy findings only</button><span>{copyState === 'copied' ? 'Findings copied without draft content.' : copyState === 'failed' ? 'Clipboard refused; nothing was saved.' : 'No source or database mutation is available here.'}</span></footer>
    </section>
  );
}

function validateDraft(text: string, contract: TeachingDataDraftContract): ValidationResult {
  if (!text.trim()) return { state: 'empty', errors: [], warnings: [], draftId: null };
  let value: unknown;
  try { value = JSON.parse(text); }
  catch (error) { return { state: 'invalid-json', errors: [`JSON parse failed: ${error instanceof Error ? error.message : 'unknown syntax error'}`], warnings: [], draftId: null }; }
  if (!isPlainObject(value)) return { state: 'structural-errors', errors: ['Draft must be one JSON object, not an array, string, number, boolean or null.'], warnings: [], draftId: null };

  const errors: string[] = [];
  const warnings: string[] = [];
  const knownFields = new Set([...contract.requiredStringFields, ...contract.requiredListFields, contract.stageField]);
  for (const field of contract.requiredStringFields) {
    const fieldValue = value[field];
    if (typeof fieldValue !== 'string' || !fieldValue.trim()) errors.push(`${field} must be a non-empty string.`);
    else {
      if (fieldValue !== fieldValue.trim()) warnings.push(`${field} has leading or trailing whitespace.`);
      if (fieldValue.length > 2000) warnings.push(`${field} exceeds 2,000 characters and needs a bounded human review.`);
    }
  }
  for (const field of contract.requiredListFields) validateStringList(value[field], field, errors, warnings);

  const draftId = typeof value.id === 'string' && value.id.trim() ? value.id.trim() : null;
  if (draftId && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(draftId)) errors.push('id must use lowercase kebab-case letters and numbers.');
  if (draftId && currentIds[contract.id].has(draftId)) errors.push(`id ${draftId} already exists in the current ${contract.label} source array.`);
  if (typeof value.subject === 'string' && value.subject.trim() && !allowedSubjects.has(value.subject)) errors.push(`subject must exactly match one current subject lens: ${[...allowedSubjects].join(', ')}.`);

  if (contract.stageField === 'stages') {
    validateStringList(value.stages, 'stages', errors, warnings);
    if (Array.isArray(value.stages)) for (const stage of value.stages) if (typeof stage === 'string' && !allowedStages.has(stage)) errors.push(`stages contains unknown stage id ${stage}.`);
  } else if (typeof value.stage !== 'string' || !value.stage.trim()) errors.push('stage must be one non-empty string.');
  else if (!allowedStages.has(value.stage)) errors.push(`stage must exactly match one current stage id: ${[...allowedStages].join(', ')}.`);

  const unknown = Object.keys(value).filter((field) => !knownFields.has(field));
  if (unknown.length) warnings.push(`Unknown fields will not be consumed by the current source shape: ${unknown.join(', ')}.`);
  return { state: errors.length ? 'structural-errors' : 'structurally-complete', errors: [...new Set(errors)], warnings: [...new Set(warnings)], draftId };
}

function validateStringList(value: unknown, field: string, errors: string[], warnings: string[]) {
  if (!Array.isArray(value) || value.length === 0) { errors.push(`${field} must be a non-empty array of strings.`); return; }
  if (value.some((item) => typeof item !== 'string' || !item.trim())) errors.push(`${field} must contain only non-empty strings.`);
  const strings = value.filter((item): item is string => typeof item === 'string').map((item) => item.trim());
  if (new Set(strings).size !== strings.length) warnings.push(`${field} contains duplicate values.`);
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
