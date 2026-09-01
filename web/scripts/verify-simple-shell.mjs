import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const shellPath = join(webRoot, 'src', 'TeacherShell.tsx');
const distRoot = join(webRoot, 'dist');
const shell = readFileSync(shellPath, 'utf8');

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
]) {
  if (!shell.includes(marker)) failures.push(`missing shell boundary marker: ${marker}`);
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
