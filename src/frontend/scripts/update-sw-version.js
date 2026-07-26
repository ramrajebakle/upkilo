/**
 * Postbuild script: injects the Next.js BUILD_ID into public/sw.js CACHE_NAME.
 * Run automatically via "postbuild" in package.json after every `next build`.
 * This busts the service worker cache on each deployment so users get fresh assets.
 */
const fs = require('fs');
const path = require('path');

const buildIdFile = path.join(__dirname, '..', '.next', 'BUILD_ID');
const swFile = path.join(__dirname, '..', 'public', 'sw.js');

if (!fs.existsSync(buildIdFile)) {
  console.warn('[update-sw-version] .next/BUILD_ID not found — skipping SW version update');
  process.exit(0);
}

const buildId = fs.readFileSync(buildIdFile, 'utf8').trim();
let sw = fs.readFileSync(swFile, 'utf8');

const updated = sw.replace(
  /const CACHE_NAME = 'upkilo-[^']+';/,
  `const CACHE_NAME = 'upkilo-${buildId}';`
);

if (updated === sw) {
  console.warn('[update-sw-version] Pattern not found in sw.js — version not updated');
} else {
  fs.writeFileSync(swFile, updated, 'utf8');
  console.log(`[update-sw-version] SW cache version → upkilo-${buildId}`);
}
