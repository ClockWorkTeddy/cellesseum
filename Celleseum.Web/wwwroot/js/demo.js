const RADIUS = 50;
// Grid geometry — must match the demo.png generation parameters
const GRID = 120;
const CELL = 2;
const GAP  = 1;
const STEP = CELL + GAP; // 3px per cell

const demos = new Map();

function drawGridBase(ctx, w, h) {
    // Dark cell background
    ctx.fillStyle = 'rgba(11,18,32,1)';
    ctx.fillRect(0, 0, w, h);

    // Subtle grid lines
    ctx.fillStyle = 'rgba(255,255,255,0.08)';
    for (let g = 0; g <= GRID; g++) {
        ctx.fillRect(g * STEP, 0, GAP, h);
        ctx.fillRect(0, g * STEP, w, GAP);
    }
}

function draw(ctx, img, w, h, mx, my) {
    // 1. Base layer: dark background + grid lines only (no cell content)
    drawGridBase(ctx, w, h);

    if (mx === null) return;

    // 2. Draw full color image onto offscreen canvas
    const off = new OffscreenCanvas(w, h);
    const offCtx = off.getContext('2d');
    offCtx.drawImage(img, 0, 0, w, h);

    // 3. Mask with radial gradient: full color at cursor → fade to transparent at edge
    offCtx.globalCompositeOperation = 'destination-in';
    const grad = offCtx.createRadialGradient(mx, my, 0, mx, my, RADIUS);
    grad.addColorStop(0,   'rgba(0,0,0,1)');
    grad.addColorStop(0.5, 'rgba(0,0,0,0.5)');
    grad.addColorStop(1,   'rgba(0,0,0,0)');
    offCtx.fillStyle = grad;
    offCtx.fillRect(0, 0, w, h);

    // 4. Paint masked color layer over the grid base
    ctx.drawImage(off, 0, 0);
}

export function initDemo(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;

    const img = new Image();
    img.src = '/images/demo.png';
    img.onerror = (e) => console.error('[demo.js] Failed to load /images/demo.png', e);
    img.onload = () => {
        canvas.width = img.naturalWidth;
        canvas.height = img.naturalHeight;
        const ctx = canvas.getContext('2d');

        draw(ctx, img, canvas.width, canvas.height, null, null);

        const onMouseMove = (e) => {
            const rect = canvas.getBoundingClientRect();
            const scaleX = canvas.width / rect.width;
            const scaleY = canvas.height / rect.height;
            const x = (e.clientX - rect.left) * scaleX;
            const y = (e.clientY - rect.top) * scaleY;
            draw(ctx, img, canvas.width, canvas.height, x, y);
        };

        const onMouseLeave = () => {
            draw(ctx, img, canvas.width, canvas.height, null, null);
        };

        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('mouseleave', onMouseLeave);

        demos.set(canvasId, { onMouseMove, onMouseLeave });
    };

    return true;
}

export function disposeDemo(canvasId) {
    const canvas = document.getElementById(canvasId);
    const demo = demos.get(canvasId);
    if (canvas && demo) {
        canvas.removeEventListener('mousemove', demo.onMouseMove);
        canvas.removeEventListener('mouseleave', demo.onMouseLeave);
    }
    demos.delete(canvasId);
}
