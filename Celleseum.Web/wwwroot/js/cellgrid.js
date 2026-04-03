/**
 * Canvas-based cell grid renderer.
 * Replaces thousands of DOM elements with a single <canvas>.
 *
 * Every cell has a uniform gap, but only every lineEvery-th gap
 * is drawn as a visible grid line. The rest match the background.
 */

const grids = new Map();

export function initCanvas(canvasId, gridSize, cellSize, gap, lineEvery) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const step = cellSize + gap;
    const totalSize = gridSize * step + gap;
    canvas.width = totalSize;
    canvas.height = totalSize;

    const ctx = canvas.getContext("2d");
    const config = { gridSize, cellSize, gap, lineEvery, step, totalSize, ctx };
    grids.set(canvasId, config);

    drawEmpty(config);
}

export function drawFrame(canvasId, frameData, saturationData) {
    const config = grids.get(canvasId);
    if (!config) return;

    const { gridSize, cellSize, gap, lineEvery, step, totalSize, ctx } = config;

    // 1. Fill entire canvas with empty-cell color (hides all gaps)
    ctx.clearRect(0, 0, totalSize, totalSize);
    ctx.fillStyle = "rgba(0,0,0,0.25)";
    ctx.fillRect(0, 0, totalSize, totalSize);

    // 2. Draw visible grid lines only at every lineEvery-th boundary
    ctx.fillStyle = "rgba(255,255,255,0.1)";
    for (let g = 0; g <= gridSize; g += lineEvery) {
        ctx.fillRect(g * step, 0, gap, totalSize);   // vertical
        ctx.fillRect(0, g * step, totalSize, gap);    // horizontal
    }

    // CellType enum: 0 = Empty, 1 = Plant, 2 = Grazer

    // 3. Draw empty and plant cells
    // Plants (type 1) use saturation (0–10) to control opacity
    for (let i = 0; i < gridSize * gridSize; i++) {
        const cellType = frameData[i] || 0;
        if (cellType === 2) continue; // grazers drawn in pass 4
        const col = i % gridSize;
        const row = (i - col) / gridSize;
        if (cellType === 1) {
            const alpha = (saturationData[i] || 0) / 10;
            ctx.fillStyle = `rgba(0,187,51,${alpha})`;
        } else {
            ctx.fillStyle = "rgba(0,0,0,0.25)";
        }
        ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
    }

    // 4. Draw solid 2x2 grazers (type 2) — fills internal gap between the 4 cells
    const drawn = new Uint8Array(gridSize * gridSize);
    const bigSize = cellSize * 2 + gap;

    for (let row = 0; row < gridSize - 1; row++) {
        for (let col = 0; col < gridSize - 1; col++) {
            const i = row * gridSize + col;
            if (drawn[i] || (frameData[i] || 0) !== 2) continue;
            const r = i + 1;
            const b = i + gridSize;
            const d = i + gridSize + 1;
            if ((frameData[r] || 0) === 2 &&
                (frameData[b] || 0) === 2 &&
                (frameData[d] || 0) === 2) {
                const alpha = ((saturationData[i] || 0) + (saturationData[r] || 0) + (saturationData[b] || 0) + (saturationData[d] || 0)) / 40;
                ctx.fillStyle = `rgba(255,255,51,${alpha})`;
                ctx.fillRect(gap + col * step, gap + row * step, bigSize, bigSize);
                drawn[i] = drawn[r] = drawn[b] = drawn[d] = 1;
            }
        }
    }
    // Leftover grazer cells not part of a full 2x2 block
    for (let i = 0; i < gridSize * gridSize; i++) {
        if (drawn[i] || (frameData[i] || 0) !== 2) continue;
        const col = i % gridSize;
        const row = (i - col) / gridSize;
        const alpha = (saturationData[i] || 0) / 10;
        ctx.fillStyle = `rgba(255,255,51,${alpha})`;
        ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
    }
}

function drawEmpty(config) {
    const { gridSize, cellSize, gap, lineEvery, step, totalSize, ctx } = config;

    // Fill with empty-cell color
    ctx.fillStyle = "rgba(0,0,0,0.25)";
    ctx.fillRect(0, 0, totalSize, totalSize);

    // Draw visible grid lines
    ctx.fillStyle = "rgba(255,255,255,0.1)";
    for (let g = 0; g <= gridSize; g += lineEvery) {
        ctx.fillRect(g * step, 0, gap, totalSize);
        ctx.fillRect(0, g * step, totalSize, gap);
    }
}

export function dispose(canvasId) {
    grids.delete(canvasId);
}
