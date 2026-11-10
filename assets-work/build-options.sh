#!/usr/bin/env bash
# Rebuilds every icon/splash option: SVGs, PNG previews and the animated GIFs.
set -euo pipefail
cd "$(dirname "$0")"
R="$HOME/.claude/skills/asset-craft/scripts/render.sh"
FF="$(ls /c/Users/Admin/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_*/ffmpeg-*/bin/ffmpeg.exe | head -1)"

node build-options.mjs

for id in a b c; do
  bash "$R" options/icon-$id-full.svg options/icon-$id-1024.png 1024x1024
  bash "$R" options/splash-sheet-$id.html options/sheet-$id.png 2160x2160

  rm -rf options/frames-$id && mkdir -p options/frames-$id
  for i in $(seq 0 35); do
    x=$(( (i % 6) * 360 )); y=$(( (i / 6) * 360 ))
    "$FF" -v error -y -i options/sheet-$id.png -vf "crop=360:360:$x:$y" -frames:v 1 options/frames-$id/f$(printf %02d $i).png
  done
  "$FF" -v error -y -framerate 20 -i options/frames-$id/f%02d.png \
    -vf "split[a][b];[a]palettegen=max_colors=48:stats_mode=full[p];[b][p]paletteuse=dither=none" \
    options/splash-$id.gif
  rm -rf options/frames-$id options/sheet-$id.png
done
ls -la options/*.gif
