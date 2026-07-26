#!/usr/bin/env node
/**
 * Generates PWA icons for all required sizes from an inline SVG source.
 * Uses the 'sharp' npm package. Run: node scripts/generate-icons.js
 * Install sharp first: npm install --save-dev sharp
 */

const path = require('path');
const fs = require('fs');

const SIZES = [72, 96, 128, 144, 152, 192, 384, 512];
const OUTPUT_DIR = path.join(__dirname, '../public/icons');

// Brand violet SVG icon with "U" lettermark
function makeSvg(size) {
  const fontSize = Math.round(size * 0.5);
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">
  <rect width="${size}" height="${size}" rx="${Math.round(size * 0.22)}" fill="#7C3AED"/>
  <text x="50%" y="54%" dominant-baseline="middle" text-anchor="middle"
        font-family="-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
        font-size="${fontSize}" font-weight="700" fill="white">U</text>
</svg>`;
}

async function generateIcons() {
  let sharp;
  try {
    sharp = require('sharp');
  } catch {
    console.error('sharp is not installed. Run: npm install --save-dev sharp');
    process.exit(1);
  }

  if (!fs.existsSync(OUTPUT_DIR)) {
    fs.mkdirSync(OUTPUT_DIR, { recursive: true });
  }

  for (const size of SIZES) {
    const svgBuffer = Buffer.from(makeSvg(size));
    const outputPath = path.join(OUTPUT_DIR, `icon-${size}x${size}.png`);
    await sharp(svgBuffer).resize(size, size).png().toFile(outputPath);
    console.log(`Generated: icon-${size}x${size}.png`);
  }

  console.log(`\nAll ${SIZES.length} icons generated in ${OUTPUT_DIR}`);
}

generateIcons().catch(console.error);
