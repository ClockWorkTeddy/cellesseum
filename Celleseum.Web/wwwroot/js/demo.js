const RADIUS = 150;
const demos = new Map();

function drawBase(ctx, w, h) {
    // Keep canvas transparent so the guest pad background color is visible
    ctx.clearRect(0, 0, w, h);
}

function getImageRect(w, h, iw, ih) {
    // Center-crop without scaling: use image pixels 1:1.
    const sw = Math.min(w, iw);
    const sh = Math.min(h, ih);
    const sx = Math.max(0, (iw - sw) / 2);
    const sy = Math.max(0, (ih - sh) / 2);
    const dx = Math.max(0, (w - sw) / 2);
    const dy = Math.max(0, (h - sh) / 2);
    return { sx, sy, sw, sh, dx, dy };
}

function createOffscreen(w, h) {
    if (typeof OffscreenCanvas !== 'undefined') {
        return new OffscreenCanvas(w, h);
    }

    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    return canvas;
}

function draw(ctx, img, w, h, mx, my) {
    drawBase(ctx, w, h);

    if (mx === null) return;

    const { sx, sy, sw, sh, dx, dy } = getImageRect(w, h, img.naturalWidth, img.naturalHeight);

    const off = createOffscreen(w, h);
    const offCtx = off.getContext('2d');
    offCtx.drawImage(img, sx, sy, sw, sh, dx, dy, sw, sh);

    offCtx.globalCompositeOperation = 'destination-in';
    const grad = offCtx.createRadialGradient(mx, my, 0, mx, my, RADIUS);
    grad.addColorStop(0, 'rgba(0,0,0,1)');
    grad.addColorStop(0.25, 'rgba(0,0,0,0.75)');
    grad.addColorStop(1, 'rgba(0,0,0,0)');
    offCtx.fillStyle = grad;
    offCtx.fillRect(0, 0, w, h);

    ctx.drawImage(off, 0, 0);
}

function resizeCanvasToElement(canvas) {
    const rect = canvas.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    canvas.width = Math.max(1, Math.floor(rect.width * dpr));
    canvas.height = Math.max(1, Math.floor(rect.height * dpr));
    const ctx = canvas.getContext('2d');
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    return { width: rect.width, height: rect.height };
}

const FRAME_DURATION = 100; // ms — ~10 fps

export function initDemo(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;
    if (demos.has(canvasId)) return true;

    // Preload up to 31 frames; skip any that 404
    const frameSlots = new Array(31).fill(null);
    let pending = 31;

    const tryStart = () => {
        if (--pending > 0) return;

        const frames = frameSlots.filter(Boolean);
        if (frames.length === 0) return;

        const state = { mx: null, my: null, rafId: null, frameIndex: 0, lastFrameTime: 0 };

        const tick = (timestamp) => {
            if (timestamp - state.lastFrameTime >= FRAME_DURATION) {
                state.frameIndex = (state.frameIndex + 1) % frames.length;
                state.lastFrameTime = timestamp;
            }

            const img = frames[state.frameIndex];
            const { width, height } = resizeCanvasToElement(canvas);
            const ctx = canvas.getContext('2d');
            draw(ctx, img, width, height, state.mx, state.my);
            state.rafId = requestAnimationFrame(tick);
        };

        const onMouseMove = (e) => {
            const rect = canvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            const withinGlowReach =
                x >= -RADIUS &&
                x <= rect.width + RADIUS &&
                y >= -RADIUS &&
                y <= rect.height + RADIUS;

            state.mx = withinGlowReach ? x : null;
            state.my = withinGlowReach ? y : null;
        };

        const onMouseLeave = () => {
            state.mx = null;
            state.my = null;
        };

        window.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseleave', onMouseLeave);

        state.rafId = requestAnimationFrame(tick);
        demos.set(canvasId, { onMouseMove, onMouseLeave, state });
    };

    for (let i = 0; i < 31; i++) {
        const img = new Image();
        const idx = i;
        img.onload = () => { frameSlots[idx] = img; tryStart(); };
        img.onerror = () => tryStart(); // missing frame — just skip it
        img.src = `/images/wp${i + 1}.png`;
    }

    return true;
}

export function disposeDemo(canvasId) {
    const demo = demos.get(canvasId);
    if (demo) {
        window.removeEventListener('mousemove', demo.onMouseMove);
        document.removeEventListener('mouseleave', demo.onMouseLeave);
        if (demo.state.rafId !== null) {
            cancelAnimationFrame(demo.state.rafId);
        }
    }

    demos.delete(canvasId);
}
