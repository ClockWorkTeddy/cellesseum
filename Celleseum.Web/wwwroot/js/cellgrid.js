/**
 * Canvas-based cell grid renderer.
 * Replaces thousands of DOM elements with a single <canvas>.
 */

const grids = new Map();

export function initCanvas(canvasId, gridSize, cellSize, gap) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const totalSize = gridSize * (cellSize + gap) + gap;
    canvas.width = totalSize;
    canvas.height = totalSize;

    const ctx = canvas.getContext("2d");
    const config = { gridSize, cellSize, gap, totalSize, ctx };
    grids.set(canvasId, config);

    drawEmpty(config);
}

export function drawFrame(canvasId, frameData) {
    const config = grids.get(canvasId);
    if (!config) return;

    const { gridSize, cellSize, gap, totalSize, ctx } = config;

    // Clear previous frame completely, then redraw separator background
    ctx.clearRect(0, 0, totalSize, totalSize);
    ctx.fillStyle = "rgba(255,255,255,0.1)";
    ctx.fillRect(0, 0, totalSize, totalSize);

    // Color palette: 0 = empty, 1 = plant (green), 2 = grazer (yellow)
    const colors = ["rgba(0,0,0,0.25)", "#00bb33", "#bbbb33"];
    const step = cellSize + gap;

    // First pass: draw empty and plant cells
    for (let i = 0; i < gridSize * gridSize; i++) {
        const cellType = frameData[i] || 0;
        if (cellType === 2) continue;
        const col = i % gridSize;
        const row = (i - col) / gridSize;
        ctx.fillStyle = colors[cellType];
        ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
    }

    // Second pass: find top-left corner of each 2x2 grazer block, draw as one solid square
    const drawn = new Uint8Array(gridSize * gridSize);
    const bigSize = cellSize * 2 + gap; // covers 2 cells + the internal gap
    ctx.fillStyle = colors[2];
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
                ctx.fillRect(gap + col * step, gap + row * step, bigSize, bigSize);
                drawn[i] = drawn[r] = drawn[b] = drawn[d] = 1;
            }
        }
    }
    // Draw any leftover grazer cells not part of a 2x2 block
    for (let i = 0; i < gridSize * gridSize; i++) {
        if (drawn[i] || (frameData[i] || 0) !== 2) continue;
        const col = i % gridSize;
        const row = (i - col) / gridSize;
        ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
    }
}

function drawEmpty(config) {
    const { gridSize, cellSize, gap, totalSize, ctx } = config;

    ctx.fillStyle = "rgba(255,255,255,0.1)";
    ctx.fillRect(0, 0, totalSize, totalSize);

    ctx.fillStyle = "rgba(0,0,0,0.25)";
    const step = cellSize + gap;
    for (let row = 0; row < gridSize; row++) {
        for (let col = 0; col < gridSize; col++) {
            ctx.fillRect(gap + col * step, gap + row * step, cellSize, cellSize);
        }
    }
}

export function dispose(canvasId) {
    grids.delete(canvasId);
}
