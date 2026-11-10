// Generates the icon + splash options for the owner to choose from.
//   node build-options.mjs  ->  options/*.svg, options/splash-sheet-*.html (frames laid out in a grid)
// Then render.sh turns each sheet into a PNG and ffmpeg slices/encodes the GIFs (see build-options.sh).
import { writeFileSync, mkdirSync } from 'node:fs';

const OUT = new URL('./options/', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1');
mkdirSync(OUT, { recursive: true });

const C = { teal: '#1B6E66', mint: '#5CC4B4', ink: '#1F201D', paper: '#F6F5F1', orange: '#EB6834' };
const f = (n) => +n.toFixed(2);
const ease = (u) => (u < 0.5 ? 2 * u * u : 1 - Math.pow(-2 * u + 2, 2) / 2);
const clamp = (v, a = 0, b = 1) => Math.min(b, Math.max(a, v));

/* ---------- The three marks, each a function of animation time t (0..1; t=0 is the resting mark) ---------- */

// A: "Check-in ring": eight slot segments around a clock face (five answered, three to come) and an orange "now" hub.
function markA(t = 0) {
  const parts = [];
  for (let k = 0; k < 8; k++) {
    const u = clamp((t - k * 0.06) / 0.5);
    const pop = Math.sin(Math.PI * u);               // 0 at rest, 1 at peak
    const r = 236 + 22 * pop;
    const a0 = (-90 + k * 45 + 3.5) * Math.PI / 180, a1 = (-90 + (k + 1) * 45 - 3.5) * Math.PI / 180;
    const x0 = 512 + r * Math.cos(a0), y0 = 512 + r * Math.sin(a0), x1 = 512 + r * Math.cos(a1), y1 = 512 + r * Math.sin(a1);
    const solid = k < 5;
    const op = solid ? 1 : 0.34 + 0.66 * pop;       // the faint slots flash on as the wave passes
    parts.push(`<path d="M${f(x0)} ${f(y0)} A${f(r)} ${f(r)} 0 0 1 ${f(x1)} ${f(y1)}" fill="none" stroke="${C.paper}" stroke-width="92" stroke-linecap="butt" opacity="${f(op)}"/>`);
  }
  const hub = 50 + 10 * Math.sin(Math.PI * clamp(t / 0.9));
  parts.push(`<circle cx="512" cy="512" r="${f(hub)}" fill="${C.orange}"/>`);
  return parts.join('');
}

// B: "Stacked day": one day as a column of time chunks, the same shape as the History chart's bars.
function markB(t = 0) {
  const segs = [                                    // bottom to top
    { h: 230, c: C.mint, o: 1 },
    { h: 150, c: C.orange, o: 1 },
    { h: 90, c: C.paper, o: 1 },
    { h: 48, c: C.paper, o: 0.38 },
  ];
  const gap = 16, w = 300, x = 512 - w / 2, total = segs.reduce((a, g) => a + g.h, 0) + gap * (segs.length - 1);
  let y = 512 + total / 2;
  return segs.map((g, i) => {
    const u = clamp((t - i * 0.11) / 0.6);
    const k = 1 - Math.sin(Math.PI * u);             // collapses to nothing, regrows from the bottom
    const h = Math.max(0, g.h * k);
    const top = y - h;
    const out = h < 1 ? '' : `<rect x="${x}" y="${f(top)}" width="${w}" height="${f(h)}" rx="${Math.min(46, h / 2)}" fill="${g.c}" opacity="${g.o}"/>`;
    y = top - gap * (h < 1 ? 0 : 1) - (h < 1 ? 0 : 0);
    return out;
  }).join('');
}

// C: "H for hour": two tall slots joined by a warm crossbar.
function markC(t = 0) {
  const u = clamp(t / 0.9);
  const s = Math.sin(Math.PI * u);
  const squeeze = 1 - 0.07 * s;
  const ph = 500 * squeeze, py = 512 - ph / 2;
  const barY = 452 - 230 * s * s;                   // crossbar lifts off and drops back
  return (
    `<rect x="322" y="${f(py)}" width="120" height="${f(ph)}" rx="60" fill="${C.teal}"/>` +
    `<rect x="582" y="${f(py)}" width="120" height="${f(ph)}" rx="60" fill="${C.teal}"/>` +
    `<rect x="382" y="${f(barY)}" width="260" height="120" rx="60" fill="${C.orange}"/>`
  );
}

const options = [
  { id: 'a', name: 'Check-in ring', bg: C.teal, mark: markA, note: 'Eight slots around a clock face with an orange hub. Five answered, three still to come.' },
  { id: 'b', name: 'Stacked day', bg: C.ink, mark: markB, note: 'One day as a column of time chunks, like the History chart.' },
  { id: 'c', name: 'H for hour', bg: C.paper, mark: markC, note: 'Two tall slots joined by a warm crossbar.' },
];

const svg = (inner, w = 1024, h = 1024, bg = null) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024" width="${w}" height="${h}">${bg ? `<rect width="1024" height="1024" fill="${bg}"/>` : ''}${inner}</svg>\n`;

for (const o of options) {
  writeFileSync(OUT + `icon-${o.id}-background.svg`, svg('', 1024, 1024, o.bg));
  writeFileSync(OUT + `icon-${o.id}-foreground.svg`, svg(o.mark(0)));
  writeFileSync(OUT + `icon-${o.id}-full.svg`, svg(o.mark(0), 1024, 1024, o.bg));
  // Splash resting frame (square, same background).
  writeFileSync(OUT + `splash-${o.id}-static.svg`, svg(o.mark(0), 1024, 1024, o.bg));

  // Animation frames laid out on one sheet: 6 x 6 tiles of 360 px; one render, then sliced by ffmpeg.
  const N = 36, COLS = 6, T = 360;
  let tiles = '';
  for (let i = 0; i < N; i++) {
    const t = i / (N - 1) * 0.999;                // last frame is the (almost) resting mark; loops cleanly
    const x = (i % COLS) * T, y = Math.floor(i / COLS) * T;
    tiles += `<svg x="${x}" y="${y}" width="${T}" height="${T}" viewBox="0 0 1024 1024"><rect width="1024" height="1024" fill="${o.bg}"/>${o.mark(i === N - 1 ? 0 : t)}</svg>`;
  }
  writeFileSync(
    OUT + `splash-sheet-${o.id}.html`,
    `<!doctype html><meta charset="utf-8"><style>html,body{margin:0;background:${o.bg}}svg{display:block}</style>` +
      `<svg xmlns="http://www.w3.org/2000/svg" width="${COLS * T}" height="${(N / COLS) * T}">${tiles}</svg>`
  );
}
writeFileSync(OUT + 'options.json', JSON.stringify(options.map(({ id, name, bg, note }) => ({ id, name, bg, note })), null, 2));
console.log('wrote', options.length, 'options to', OUT);
