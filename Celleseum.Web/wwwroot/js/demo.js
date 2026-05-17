const RADIUS = 75;
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
    grad.addColorStop(0.5, 'rgba(0,0,0,1)');
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

export function initDemo(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;
    if (demos.has(canvasId)) return true;

    const img = new Image();
    img.src = '/images/demo.png';
    img.onerror = (e) => console.error('[demo.js] Failed to load /images/demo.png', e);

    img.onload = () => {
        const render = (mx = null, my = null) => {
            const { width, height } = resizeCanvasToElement(canvas);
            const ctx = canvas.getContext('2d');
            draw(ctx, img, width, height, mx, my);
        };

        render();

        const surface = canvas.parentElement ?? canvas;

        const onMouseMove = (e) => {
            const rect = canvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            render(x, y);
        };

        const onMouseLeave = () => render();
        const onResize = () => render();

        surface.addEventListener('mousemove', onMouseMove);
        surface.addEventListener('mouseleave', onMouseLeave);
        window.addEventListener('resize', onResize);

        demos.set(canvasId, { onMouseMove, onMouseLeave, onResize, surface });
    };

    return true;
}

export function disposeDemo(canvasId) {
    const canvas = document.getElementById(canvasId);
    const demo = demos.get(canvasId);
    if (canvas && demo) {
        const surface = demo.surface ?? canvas;
        surface.removeEventListener('mousemove', demo.onMouseMove);
        surface.removeEventListener('mouseleave', demo.onMouseLeave);
        window.removeEventListener('resize', demo.onResize);
    }

    demos.delete(canvasId);
}
