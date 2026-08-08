/**
 * Canvas-based cell grid renderer.
 * Replaces thousands of DOM elements with a single <canvas>.
 *
 * Every cell has a uniform gap, but only every lineEvery-th gap
 * is drawn as a visible grid line. The rest match the background.
 */

const grids = new Map();
// Grazer palette pre-packed as Uint32 RGBA words (little-endian: 0xAABBGGRR)
// One word per palette entry — no string allocation or CSS parsing per cell.
const grazerPaletteU32 = (() => {
    const anchors = [
        [0x80, 0xFF, 0x00],
        [0xFF, 0xFF, 0x00],
        [0xFF, 0x00, 0x00],
        [0xFF, 0x00, 0xFF],
        [0x00, 0x00, 0xFF],
        [0x00, 0xFF, 0x80]
    ];

    const palette = new Uint32Array(8);
    const segmentCount = anchors.length - 1;

    for (let i = 0; i < 8; i++) {
        const t = i / 7;
        const segmentFloat = t * segmentCount;
        const segment = Math.min(segmentCount - 1, Math.floor(segmentFloat));
        const localT = segmentFloat - segment;

        const from = anchors[segment];
        const to   = anchors[segment + 1];

        const r = Math.round(from[0] + (to[0] - from[0]) * localT);
        const g = Math.round(from[1] + (to[1] - from[1]) * localT);
        const b = Math.round(from[2] + (to[2] - from[2]) * localT);

        // little-endian RGBA: byte order R,G,B,A → Uint32 = A<<24 | B<<16 | G<<8 | R
        palette[i] = ((255 << 24) | (b << 16) | (g << 8) | r) >>> 0;
    }

    return palette;
})();

export function initCanvas(canvasId, gridWidth, gridHeight, cellSize, gap, lineEvery) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;

    const step = cellSize + gap;
    const totalWidth  = gridWidth  * step + gap;
    const totalHeight = gridHeight * step + gap;
    canvas.width  = totalWidth;
    canvas.height = totalHeight;

    const ctx = canvas.getContext("2d");
    const config = { gridWidth, gridHeight, cellSize, gap, lineEvery, step, totalWidth, totalHeight, ctx };
    grids.set(canvasId, config);

    drawEmpty(config);
    return true;
}

export function drawFrame(canvasId, frameData, saturationData) {
    const config = grids.get(canvasId);
    if (!config) return;

    const { gridWidth, gridHeight, cellSize, gap, step, totalWidth, totalHeight, ctx } = config;
    const cellCount = gridWidth * gridHeight;

    // Allocate once, reuse every frame
    if (!config.imageData) {
        config.imageData = ctx.createImageData(totalWidth, totalHeight);
        config.pixels32  = new Uint32Array(config.imageData.data.buffer);
        config.drawn     = new Uint8Array(cellCount); // tracks grazer cells already painted
    }
    const pixels32 = config.pixels32;
    const drawn    = config.drawn;

    // little-endian RGBA words
    const sepColor32 = 0x1AFFFFFF; // rgba(255,255,255,26) — separator lines
    const bgColor32  = 0x40000000; // rgba(0,0,0,64)       — empty cells

    // Fill entire canvas with separator colour; cell pixels are painted over below.
    pixels32.fill(gap > 0 ? sepColor32 : bgColor32);

    // Reset per-frame drawn flags
    drawn.fill(0);

    for (let i = 0; i < cellCount; i++) {
        const cellType = frameData[i] || 0;

        // Skip grazer cells already covered by a 2×2 block painted from its top-left
        if (cellType === 2 && drawn[i]) continue;

        const col = i % gridWidth;
        const row = (i - col) / gridWidth;
        const px  = gap + col * step;
        const py  = gap + row * step;

        if (cellType === 2) {
            const color32 = grazerPaletteU32[Math.min(7, (saturationData[i] || 0) & 0xFF)];

            // Detect top-left corner of a 2×2 grazer block.
            // Paint the whole block solid (covers internal gap) so the grazer
            // looks like one piece. Borders toward other grazers are untouched.
            const rIdx = i + 1;
            const bIdx = i + gridWidth;
            const dIdx = i + gridWidth + 1;

            if (col + 1 < gridWidth && row + 1 < gridHeight &&
                (frameData[rIdx] || 0) === 2 &&
                (frameData[bIdx] || 0) === 2 &&
                (frameData[dIdx] || 0) === 2) {

                const blockSize = 2 * cellSize + gap; // covers 2 cells + the gap between them
                for (let dy = 0; dy < blockSize; dy++) {
                    const rowBase = (py + dy) * totalWidth + px;
                    for (let dx = 0; dx < blockSize; dx++) {
                        pixels32[rowBase + dx] = color32;
                    }
                }
                drawn[i] = drawn[rIdx] = drawn[bIdx] = drawn[dIdx] = 1;

            } else {
                // Orphan / edge grazer cell — paint as single cell
                for (let dy = 0; dy < cellSize; dy++) {
                    const rowBase = (py + dy) * totalWidth + px;
                    for (let dx = 0; dx < cellSize; dx++) {
                        pixels32[rowBase + dx] = color32;
                    }
                }
            }

        } else if (cellType === 1) {
            // Plant: rgba(0,187,51,alpha) — saturation 0-8 → alpha 0-255
            const sat    = saturationData[i] || 0;
            const alpha  = sat >= 8 ? 255 : Math.round((sat / 8) * 255);
            const color32 = ((alpha << 24) | (51 << 16) | (187 << 8)) >>> 0;
            for (let dy = 0; dy < cellSize; dy++) {
                const rowBase = (py + dy) * totalWidth + px;
                for (let dx = 0; dx < cellSize; dx++) {
                    pixels32[rowBase + dx] = color32;
                }
            }

        } else {
            // Empty cell: overwrite separator colour at this cell's position
            for (let dy = 0; dy < cellSize; dy++) {
                const rowBase = (py + dy) * totalWidth + px;
                for (let dx = 0; dx < cellSize; dx++) {
                    pixels32[rowBase + dx] = bgColor32;
                }
            }
        }
    }

    // One GPU upload per frame
    ctx.putImageData(config.imageData, 0, 0);
}

function drawEmpty(config) {
    const { totalWidth, totalHeight, ctx } = config;

    // Fill with empty-cell color
    ctx.fillStyle = "rgba(0,0,0,0.25)";
    ctx.fillRect(0, 0, totalWidth, totalHeight);
}

const players = new Map();

export function startPlayback(canvasId, delay, dotNetRef) {
    players.set(canvasId, {
        delay,
        dotNetRef,
        queue: [],
        currentBatch: null,
        frameInBatch: 0,
        isPlaying: false,
        isPaused: false,
        isCompleted: false,
        timerId: null
    });
}

export function enqueueFrames(canvasId, allTypes, allSaturation, plantCounts, grazerCounts, score, grazerSaturationCounts, startFrame) {
    const config = grids.get(canvasId);
    const player = players.get(canvasId);
    if (!config || !player) return;

    player.queue.push({
        allTypes,
        allSaturation,
        plantCounts,
        grazerCounts,
        score,
        grazerSaturationCounts,
        startFrame,
        frameCount: plantCounts.length,
        cellCount: config.gridWidth * config.gridHeight
    });

    if (!player.isPaused && !player.isPlaying) {
        player.isPlaying = true;
        tickPlayer(canvasId);
    }
}

export function completePlayback(canvasId) {
    const player = players.get(canvasId);
    if (!player) return;

    player.isCompleted = true;
    if (!player.isPlaying && player.queue.length === 0 && !player.currentBatch) {
        player.dotNetRef.invokeMethodAsync("OnPlaybackComplete");
    }
}

function tickPlayer(canvasId) {
    const config = grids.get(canvasId);
    const player = players.get(canvasId);
    if (!config || !player) return;

    // Resolve stat elements once and cache them on the player object
    if (!player.statEls) {
        const saturationEls = new Array(8);
        for (let s = 0; s < 8; s++) {
            saturationEls[s] = document.getElementById(`stat-grazer-${s}`);
        }
        player.statEls = {
            step:       document.getElementById('stat-step'),
            plant:      document.getElementById('stat-plants'),
            grazer:     document.getElementById('stat-grazers'),
            score:      document.getElementById('stat-score'),
            saturation: saturationEls,
        };
    }
    const { step: stepEl, plant: plantEl, grazer: grazerEl, score: scoreEl, saturation: saturationEls } = player.statEls;

    if (!player.currentBatch) {
        player.currentBatch = player.queue.shift() || null;
        player.frameInBatch = 0;
    }

    if (!player.currentBatch) {
        player.isPlaying = false;
        if (player.isCompleted) {
            player.dotNetRef.invokeMethodAsync("OnPlaybackComplete");
        }
        return;
    }

    const batch = player.currentBatch;
    const frame = player.frameInBatch;
    const offset = frame * batch.cellCount;
    const types = batch.allTypes.subarray(offset, offset + batch.cellCount);
    const saturation = batch.allSaturation.subarray(offset, offset + batch.cellCount);

    drawFrame(canvasId, types, saturation);

    const absoluteFrame = batch.startFrame + frame;
    window.dispatchEvent(new CustomEvent('celleseum:frame', { detail: { frame: absoluteFrame } }));

    if (stepEl) stepEl.textContent = absoluteFrame;
    if (plantEl) plantEl.textContent = batch.plantCounts[frame];
    if (grazerEl) grazerEl.textContent = batch.grazerCounts[frame];
    if (scoreEl) scoreEl.textContent = batch.score[frame];

    for (let s = 0; s < saturationEls.length; s++) {
        const saturationEl = saturationEls[s];
        if (!saturationEl) continue;

        const saturationSeries = batch.grazerSaturationCounts?.[s];
        const saturationCount = Array.isArray(saturationSeries) && frame < saturationSeries.length
            ? saturationSeries[frame]
            : 0;
        saturationEl.textContent = saturationCount;
    }

    player.frameInBatch++;
    if (player.frameInBatch >= batch.frameCount) {
        player.currentBatch = null;
        player.frameInBatch = 0;
    }

    player.timerId = setTimeout(() => tickPlayer(canvasId), player.delay);
}

export function pausePlayback(canvasId) {
    const player = players.get(canvasId);
    if (!player) return;
    if (player.timerId) {
        clearTimeout(player.timerId);
        player.timerId = null;
    }
    player.isPaused = true;
    player.isPlaying = false;
}

export function resumePlayback(canvasId) {
    const player = players.get(canvasId);
    if (!player || !player.isPaused) return;
    player.isPaused = false;
    player.isPlaying = true;
    tickPlayer(canvasId);
}

export function stepPlayback(canvasId) {
    const player = players.get(canvasId);
    if (!player || !player.isPaused) return;
    tickPlayer(canvasId);
    // tickPlayer scheduled the next tick — cancel it so only one frame advances
    if (player.timerId) {
        clearTimeout(player.timerId);
        player.timerId = null;
    }
}

export function dispose(canvasId) {
    const player = players.get(canvasId);
    if (player?.timerId) {
        clearTimeout(player.timerId);
    }

    players.delete(canvasId);
    grids.delete(canvasId);
}