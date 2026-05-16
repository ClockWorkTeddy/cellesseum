const RADIUS = 50;
const demos = new Map();

function draw(ctx, img, w, h, mx, my) {
    // 1. Full image in grayscale as base layer
    ctx.filter = 'grayscale(1)';
    ctx.drawImage(img, 0, 0, w, h);
    ctx.filter = 'none';

    if (mx === null) return;

    // 2. Draw color image onto offscreen canvas
    const off = new OffscreenCanvas(w, h);
    const offCtx = off.getContext('2d');
    offCtx.drawImage(img, 0, 0, w, h);

    // 3. Mask the color layer with a radial gradient:
    //    full opacity at cursor → 50% halfway → transparent at edge
    offCtx.globalCompositeOperation = 'destination-in';
    const grad = offCtx.createRadialGradient(mx, my, 0, mx, my, RADIUS);
    grad.addColorStop(0,   'rgba(0,0,0,1)');
    grad.addColorStop(0.5, 'rgba(0,0,0,0.5)');
    grad.addColorStop(1,   'rgba(0,0,0,0)');
    offCtx.fillStyle = grad;
    offCtx.fillRect(0, 0, w, h);

    // 4. Paint the masked color layer on top of the grayscale base
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
