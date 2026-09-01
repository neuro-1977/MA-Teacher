import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(scriptDir, '..', '..');
const assetPath = path.join(root, 'web', 'src', 'assets', 'ma-teacher-logo.png');
const sourcePaths = [
  path.join(root, 'web', 'src', 'App.tsx'),
  path.join(root, 'web', 'src', 'ClassroomStudentShell.tsx'),
  path.join(root, 'web', 'src', 'TeacherShell.tsx'),
];

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

const png = fs.readFileSync(assetPath);
assert(png.length <= 160 * 1024, `Web logo exceeds 160 KiB: ${png.length} bytes`);
assert(png.subarray(1, 4).toString('ascii') === 'PNG', 'Web logo is not a PNG');
assert(png.readUInt32BE(16) === 256 && png.readUInt32BE(20) === 256,
  `Web logo must be 256x256, found ${png.readUInt32BE(16)}x${png.readUInt32BE(20)}`);

for (const sourcePath of sourcePaths) {
  const source = fs.readFileSync(sourcePath, 'utf8');
  assert(source.includes("./assets/ma-teacher-logo.png"), `${path.basename(sourcePath)} does not use the bounded web logo`);
  assert(!source.includes('icon-large.png'), `${path.basename(sourcePath)} still ships the 2.2 MB master logo`);
}

console.log(`[web-assets] PASS ${png.length} bytes, 256x256, three bounded runtime imports`);
