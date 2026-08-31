import { useMemo, useState } from 'react';
import { lessonReviewCategories, type LessonReviewCategory, type LessonReviewCategoryId } from './lesson-review-criteria';
import './lesson-review-gate.css';

type ReviewFilter = 'all' | LessonReviewCategoryId;

export function LessonReviewGatePanel() {
  const [filter, setFilter] = useState<ReviewFilter>('all');
  const visible = useMemo<readonly LessonReviewCategory[]>(() => filter === 'all' ? lessonReviewCategories : lessonReviewCategories.filter((category) => category.id === filter), [filter]);
  const criterionCount = visible.reduce<number>((count, category) => count + category.criteria.length, 0);

  return <section id="workspace-lesson-review" className="lesson-review-shell" aria-labelledby="lesson-review-title">
    <header>
      <div>
        <p>HUMAN REVIEW GATE · NO APPROVAL BUTTON</p>
        <h2 id="lesson-review-title">Inspect the lesson before anyone relies on it.</h2>
        <span>These criteria identify evidence and stop conditions. They do not score, approve, publish, or mutate a lesson.</span>
      </div>
      <label>Review category<select value={filter} onChange={(event) => setFilter(event.target.value as ReviewFilter)}><option value="all">All categories</option>{lessonReviewCategories.map((category) => <option key={category.id} value={category.id}>{category.label}</option>)}</select></label>
    </header>

    <p className="lesson-review-count" role="status">Showing {criterionCount} criteria across {visible.length} {visible.length === 1 ? 'category' : 'categories'}.</p>

    <div className="lesson-review-categories">
      {visible.map((category) => <article key={category.id}>
        <div className="lesson-review-category-heading"><div><p>{category.id.toUpperCase()}</p><h3>{category.label}</h3></div><span>{category.purpose}</span></div>
        <div className="lesson-review-criteria">
          {category.criteria.map((criterion) => <section key={criterion.id}>
            <div><code>{criterion.id}</code><h4>{criterion.question}</h4></div>
            <p><b>Evidence to inspect</b><span>{criterion.evidenceNeeded}</span></p>
            <p className="lesson-review-stop"><b>Stop use when</b><span>{criterion.stopWhen}</span></p>
          </section>)}
        </div>
      </article>)}
    </div>
  </section>;
}
