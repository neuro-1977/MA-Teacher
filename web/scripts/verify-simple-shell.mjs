import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const shellPath = join(webRoot, 'src', 'TeacherShell.tsx');
const startPath = join(webRoot, 'src', 'GettingStartedPanel.tsx');
const distRoot = join(webRoot, 'dist');
const shell = readFileSync(shellPath, 'utf8');
const start = readFileSync(startPath, 'utf8');

const failures = [];
const forbiddenEagerImport = /^import\s+.+from ['"]\.\/(?:App|[^'"]*Panel|ViewModeControl)['"];?$/gm;
const eagerImports = [...shell.matchAll(forbiddenEagerImport)].map((match) => match[0]);
if (eagerImports.length > 0) failures.push(`teacher surfaces are eager again: ${eagerImports.join(' | ')}`);

for (const marker of [
  "lazy(() => import('./GettingStartedPanel')",
  "lazy(() => import('./LearningCheckPanel')",
  "lazy(() => import('./WorkspaceIndexPanel')",
  '<Suspense fallback={<WorkspaceLoading label={activeEntry.label} />}>',
  'role="status" aria-live="polite" aria-busy="true"',
  "const simpleIds = new Set(['workspace-start', 'workspace-lesson-reader', 'workspace-learning-checks', 'workspace-progress', 'workspace-subjects', 'workspace-feedback-hub'])",
  "'workspace-start': (view) => <GettingStartedPanel showTeacherHelp={view === 'teacher'} />",
  "'workspace-learning-checks': (view) => <LearningCheckPanel mode={view === 'teacher' ? 'teacher' : 'learner'} />",
  "view === 'simple' && effect === 'database-write' ? 'SAVES YOUR WORK'",
  'function isWorkspaceVisibleInView(id: string, view: AppView)',
  'isWorkspaceVisibleInView(initialHash, initialView)',
  'const next = isWorkspaceVisibleInView(requested, view) ? requested : \'teacher-home\';',
  'const restore = () => open(window.location.hash.slice(1), false);',
  '}, [view]);',
]) {
  if (!shell.includes(marker)) failures.push(`missing shell boundary marker: ${marker}`);
}

for (const marker of [
  'showTeacherHelp = false',
  '{showTeacherHelp && <section className="getting-started-preflight"',
  'These buttons do not change records',
]) {
  if (!start.includes(marker)) failures.push(`missing learner/teacher start boundary marker: ${marker}`);
}

const indexHtml = readFileSync(join(distRoot, 'index.html'), 'utf8');
const entryMatch = indexHtml.match(/<script[^>]+src="\/assets\/([^"]+\.js)"/);
if (!entryMatch) failures.push('Vite entry script was not found in dist/index.html');

let entryBytes = 0;
if (entryMatch) {
  const entryPath = join(distRoot, 'assets', entryMatch[1]);
  entryBytes = statSync(entryPath).size;
  if (entryBytes > 350_000) failures.push(`initial JavaScript is ${entryBytes} bytes; calm-shell ceiling is 350000`);
}

const jsChunks = readdirSync(join(distRoot, 'assets')).filter((name) => name.endsWith('.js'));
if (jsChunks.length < 40) failures.push(`expected lazy surface chunks; found only ${jsChunks.length} JavaScript files`);

if (failures.length > 0) {
  console.error('Simple shell contract failed:');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log(JSON.stringify({
  contract: 'simple-shell-lazy-boundary',
  status: 'passed',
  initialJavaScriptBytes: entryBytes,
  JavaScriptChunkCount: jsChunks.length,
  ceilingBytes: 350_000,
}));
