import { jurisdictionStageGuides } from './jurisdiction-stage-guidance';
import './jurisdiction-stage-guidance.css';

export function JurisdictionStageGuidePanel() {
  return (
    <section className="stage-guidance" id="workspace-jurisdiction-stage-guidance" aria-labelledby="stage-guidance-title">
      <header className="stage-guidance__header">
        <div><p className="stage-guidance__eyebrow">Orientation / no equivalence</p><h2 id="stage-guidance-title">Four jurisdictions, four native structures</h2><p>Use the publisher's own stages before choosing teaching evidence. Similar labels do not guarantee the same years, duties or progression model.</p></div>
        <span>{jurisdictionStageGuides.length} research guides</span>
      </header>
      <div className="stage-guidance__boundary" role="note">These are unaccepted research summaries. Selecting or reading one does not classify a learner, map a curriculum, or approve teaching content.</div>
      <div className="stage-guidance__grid">
        {jurisdictionStageGuides.map((guide) => (
          <article className="stage-guide" key={guide.id}>
            <div className="stage-guide__title"><div><p>{guide.evidenceStatus}</p><h3>{guide.jurisdiction}</h3></div><span>{guide.researchedOn}</span></div>
            <p className="stage-guide__shape">{guide.frameworkShape}</p>
            <ol>{guide.nativeStages.map((stage) => <li key={stage.label}><strong>{stage.label}</strong><span>{stage.broadScope}</span></li>)}</ol>
            <dl>
              <div><dt>Internal lens guidance</dt><dd>{guide.internalLensGuidance}</dd></div>
              <div className="stage-guide__caution"><dt>Must not assume</dt><dd>{guide.mustNotAssume}</dd></div>
              <div><dt>Source candidates</dt><dd>{guide.sourceCandidateIds.map((id) => <code key={id}>{id}</code>)}</dd></div>
            </dl>
          </article>
        ))}
      </div>
    </section>
  );
}
