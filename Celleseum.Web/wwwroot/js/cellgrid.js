/**
 * Canvas-based cell grid renderer.
 * Replaces thousands of DOM elements with a single <canvas>.
 *
 * Every cell has a uniform gap, but only every lineEvery-th gap
 * is drawn as a visible grid line. The rest match the background.
 */

const grids = new Map();

function hueToRgbWord(hueDegrees) {
    const h = ((hueDegrees % 360) + 360) % 360;
    const c = 1;
    const x = c * (1 - Math.abs(((h / 60) % 2) - 1));

    let r1 = 0;
    let g1 = 0;
    let b1 = 0;

    if (h < 60) {
        r1 = c; g1 = x;
    } else if (h < 120) {
        r1 = x; g1 = c;
    } else if (h < 180) {
        g1 = c; b1 = x;
    } else if (h < 240) {
        g1 = x; b1 = c;
    } else if (h < 300) {
        r1 = x; b1 = c;
    } else {
        r1 = c; b1 = x;
    }

    const r = Math.round(r1 * 255);
    const g = Math.round(g1 * 255);
    const b = Math.round(b1 * 255);

    // little-endian RGBA: byte order R,G,B,A → Uint32 = A<<24 | B<<16 | G<<8 | R
    return ((255 << 24) | (b << 16) | (g << 8) | r) >>> 0;
}

function buildMutationPalette(maxGeneration) {
    const generationCount = Math.max(1, Number(maxGeneration) | 0) + 1;
    const sections = generationCount + 1; // generations + 2 slots including plant anchor
    const sectionAngle = 360 / sections;
    const palette = new Uint32Array(generationCount);

    for (let generation = 0; generation < generationCount; generation++) {
        const hue = 120 - sectionAngle * (generation + 1);
        palette[generation] = hueToRgbWord(hue);
    }

    return palette;
}

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

export function drawFrame(canvasId, frameData, saturationData, mode = "simple", mutationPalette = null) {
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
            const saturation = (saturationData[i] || 0) & 0xFF;
            const color32 = mode === "mutation"
                ? mutationPalette[Math.min(mutationPalette.length - 1, saturation)]
                : 0xFFCC44FF; // rgba(255,68,204,255) complementary to plant green


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

export function startPlayback(canvasId, delay, dynamicDelay, mode, generations, dotNetRef) {
    players.set(canvasId, {
        baseDelay: delay,
        dynamicDelay,
        mode,
        mutationPalette: buildMutationPalette(generations),
        dotNetRef,
        queue: [],
        currentBatch: null,
        frameInBatch: 0,
        isPlaying: false,
        isPaused: false,
        isCompleted: false,
        isBuffering: true,
        hasNotifiedStart: false,
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

    if (!player.isBuffering && !player.isPaused && !player.isPlaying) {
        player.isPlaying = true;
        tickPlayer(canvasId);
    }
}

export function completePlayback(canvasId) {
    const player = players.get(canvasId);
    if (!player) return;

    player.isCompleted = true;
    player.isBuffering = false;

    if (!player.isPaused && !player.isPlaying && (player.currentBatch || player.queue.length > 0)) {
        player.isPlaying = true;
        tickPlayer(canvasId);
        return;
    }

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
        const saturationEls = Array.from(document.querySelectorAll('[id^="stat-grazer-"]'))
            .sort((a, b) => {
                const aIndex = Number.parseInt(a.id.replace('stat-grazer-', ''), 10);
                const bIndex = Number.parseInt(b.id.replace('stat-grazer-', ''), 10);
                return aIndex - bIndex;
            });

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

    drawFrame(canvasId, types, saturation, player.mode, player.mutationPalette);

    if (!player.hasNotifiedStart) {
        player.hasNotifiedStart = true;
        player.dotNetRef.invokeMethodAsync("OnPlaybackStarted");
    }

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

    const grazerCount = batch.grazerCounts[frame] || 1;
    let frameDelay;
    if (player.dynamicDelay) {
        const maxGrazers = config.gridWidth * config.gridHeight / 100;
        const t = Math.min(1, Math.max(0, (grazerCount - 1) / Math.max(1, maxGrazers - 1)));
        frameDelay = Math.round(player.baseDelay + t * (15 - player.baseDelay));
    } else {
        frameDelay = player.baseDelay;
    }
    player.timerId = setTimeout(() => tickPlayer(canvasId), frameDelay);
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

export async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: "application/x-msgpack" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
}

export function dispose(canvasId) {
    const player = players.get(canvasId);
    if (player?.timerId) {
        clearTimeout(player.timerId);
    }

    players.delete(canvasId);
    grids.delete(canvasId);
}