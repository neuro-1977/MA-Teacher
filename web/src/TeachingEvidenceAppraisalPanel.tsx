import { useMemo, useState } from 'react';
import {
  evidenceAppraisalDimensions,
  teachingClaimKinds,
  type TeachingClaimKind,
} from './teaching-evidence-appraisal-data';
import './teaching-evidence-appraisal.css';

export function TeachingEvidenceAppraisalPanel() {
  const [claimKind, setClaimKind] = useState<TeachingClaimKind>('Curriculum requirement');
  const dimensions = useMemo(
    () => evidenceAppraisalDimensions.filter((dimension) => dimension.appliesTo.includes(claimKind)),
    [claimKind],
  );

  return (
    <section id="workspace-evidence-appraisal" className="evidence-appraisal-panel" aria-labelledby="evidence-appraisal-title">
      <header>
        <p className="evidence-appraisal-kicker">Claim-specific evidence review</p>
        <h2 id="evidence-appraisal-title">Ask what the source can actually prove</h2>
        <p>A current, official, popular, or research-labelled source is not automatically suitable for every claim. Select the claim type before applying its evidence.</p>
      </header>

      <div className="evidence-appraisal-claims" role="group" aria-label="Select claim type">
        {teachingClaimKinds.map((kind) => (
          <button
            type="button"
            key={kind}
            aria-pressed={claimKind === kind}
            className={claimKind === kind ? 'is-active' : ''}
            onClick={() => setClaimKind(kind)}
          >
            {kind}
          </button>
        ))}
      </div>

      <p className="evidence-appraisal-selection" role="status">
        Showing {dimensions.length} review dimensions for <strong>{claimKind}</strong>. No quality score or acceptance decision is calculated.
      </p>

      <div className="evidence-appraisal-grid">
        {dimensions.map((dimension) => (
          <article key={dimension.id}>
            <h3>{dimension.title}</h3>
            <h4>Questions</h4>
            <ul>{dimension.questions.map((item) => <li key={item}>{item}</li>)}</ul>
            <h4>Record explicitly</h4>
            <ul>{dimension.record.map((item) => <li key={item}>{item}</li>)}</ul>
            <p><strong>Stop when:</strong> {dimension.stopWhen}</p>
          </article>
        ))}
      </div>

      <p className="evidence-appraisal-boundary" role="note">
        This is a review scaffold, not an evidence-ranking engine. Human reviewers must preserve contradictions, uncertainty, context, and unsupported transfer rather than averaging them into a confidence score.
      </p>
    </section>
  );
}
