/**
 * Canvas-based cell grid renderer.
 * Replaces thousands of DOM elements with a single <canvas>.
 *
 * Every cell has a uniform gap, but only every lineEvery-th gap
 * is drawn as a visible grid line. The rest match the background.
 */

const grids = new Map();
const grazerColorPalette = (() => {
    const anchors = [
        [0x77, 0xFF, 0x00],
        [0xFF, 0xFF, 0x00],
        [0xFF, 0x00, 0x00],
        [0xFF, 0x00, 0xFF],
        [0x00, 0x00, 0xFF],
        [0x00, 0xFF, 0x77]
    ];

    const palette = new Array(8);
    const segmentCount = anchors.length - 1;

    for (let i = 0; i < 8; i++) {
        const t = i / 7;
        const segmentFloat = t * segmentCount;
        const segment = Math.min(segmentCount - 1, Math.floor(segmentFloat));
        const localT = segmentFloat - segment;

        const from = anchors[segment];
        const to = anchors[segment + 1];

        const r = Math.round(from[0] + (to[0] - from[0]) * localT);
        const g = Math.round(from[1] + (to[1] - from[1]) * localT);
        const b = Math.round(from[2] + (to[2] - from[2]) * localT);

        palette[i] = `rgb(${r}, ${g}, ${b})`;
    }

    return palette;
})();

function grazerColorFromVariant(variant) {
    const value = variant & 0xFF;
    const index = Math.min(7, value);
    return grazerColorPalette[index];
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

export function drawFrame(canvasId, frameData, saturationData) {
    const config = grids.get(canvasId);
    if (!config) return;

    const { gridWidth, gridHeight, cellSize, gap, lineEvery, step, totalWidth, totalHeight, ctx } = config;

    // 1. Fill entire canvas with empty-cell color (hides all gaps)
    ctx.clearRect(0, 0, totalWidth, totalHeight);
    ctx.fillStyle = "rgba(0,0,0,0.25)";
    ctx.fillRect(0, 0, totalWidth, totalHeight);

    // 2. Draw visible grid lines only at every lineEvery-th boundary
    ctx.fillStyle = "rgba(255,255,255,0.1)";
    for (let g = 0; g <= gridWidth; g += lineEvery) {
        ctx.fillRect(g * step, 0, gap, totalHeight);   // vertical
    }
    for (let g = 0; g <= gridHeight; g += lineEvery) {
        ctx.fillRect(0, g * step, totalWidth, gap);    // horizontal
    }

    // CellType enum: 0 = Empty, 1 = Plant, 2 = Grazer

    // 3. Draw empty and plant cells
    // Plants (type 1) use saturation (0–10) to control opacity
    for (let i = 0; i < gridWidth * gridHeight; i++) {
        const cellType = frameData[i] || 0;
        if (cellType === 2) continue; // grazers drawn in pass 4
        const col = i % gridWidth;
        const row = (i - col) / gridWidth;
        if (cellType === 1) {
            const alpha = (saturationData[i] || 0) / 10;
            ctx.fillStyle = `rgba(0,187,51,${alpha})`;
        } else {
            ctx.fillStyle = "rgba(0,0,0,0.25)";
        }
        ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
    }

    // 4. Draw solid 2x2 grazers (type 2) — fills internal gap between the 4 cells
    const drawn = new Uint8Array(gridWidth * gridHeight);
    const bigSize = cellSize * 2 + gap;

    for (let row = 0; row < gridHeight - 1; row++) {
        for (let col = 0; col < gridWidth - 1; col++) {
            const i = row * gridWidth + col;
            if (drawn[i] || (frameData[i] || 0) !== 2) continue;
            const r = i + 1;
            const b = i + gridWidth;
            const d = i + gridWidth + 1;
            if ((frameData[r] || 0) === 2 &&
                (frameData[b] || 0) === 2 &&
                (frameData[d] || 0) === 2) {
                
                ctx.fillStyle = grazerColorFromVariant(saturationData[i] || 0);
                ctx.fillRect(gap + col * step, gap + row * step, bigSize, bigSize);
                drawn[i] = drawn[r] = drawn[b] = drawn[d] = 1;
            }
        }
    }
    // Leftover grazer cells not part of a full 2x2 block
    for (let i = 0; i < gridWidth * gridHeight; i++) {
        if (drawn[i] || (frameData[i] || 0) !== 2) continue;
        const col = i % gridWidth;
        const row = (i - col) / gridWidth;
        ctx.fillStyle = grazerColorFromVariant(saturationData[i] || 0);
        ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
    }
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

export function enqueueFrames(canvasId, allTypes, allSaturation, plantCounts, grazerCounts, score, startFrame) {
    const config = grids.get(canvasId);
    const player = players.get(canvasId);
    if (!config || !player) return;

    player.queue.push({
        allTypes,
        allSaturation,
        plantCounts,
        grazerCounts,
        score,
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

    const stepEl = document.getElementById("stat-step");
    const plantEl = document.getElementById("stat-plants");
    const grazerEl = document.getElementById("stat-grazers");
    const scoreEl = document.getElementById("stat-score");

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
