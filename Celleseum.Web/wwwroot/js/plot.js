const plots = new Map();

export function initPlot(canvasId, color, label, totalSteps) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;

    const onFrame = (e) => {
        const state = plots.get(canvasId);
        if (!state) return;
        state.displayUpTo = e.detail.frame;
        redraw(canvasId);
    };

    window.addEventListener('celleseum:frame', onFrame);
    plots.set(canvasId, { data: [], color, label, totalSteps, displayUpTo: -1, onFrame });
    return true;
}

export function pushFrames(canvasId, counts) {
    const state = plots.get(canvasId);
    if (!state) return;
    for (let i = 0; i < counts.length; i++) {
        state.data.push(counts[i]);
    }
    // No immediate redraw — driven by celleseum:frame events
}

export function resetPlot(canvasId) {
    const state = plots.get(canvasId);
    if (!state) return;
    state.data = [];
    state.displayUpTo = -1;
    redraw(canvasId);
}

export function disposePlot(canvasId) {
    const state = plots.get(canvasId);
    if (state?.onFrame) {
        window.removeEventListener('celleseum:frame', state.onFrame);
    }
    plots.delete(canvasId);
}

function maxOf(arr, n) {
    let m = 0;
    for (let i = 0; i < n; i++) if (arr[i] > m) m = arr[i];
    return m;
}

function redraw(canvasId) {
    const canvas = document.getElementById(canvasId);
    const state = plots.get(canvasId);
    if (!canvas || !state) return;

    const dpr = window.devicePixelRatio || 1;
    const cssW = canvas.clientWidth;
    const cssH = canvas.clientHeight;
    if (cssW === 0 || cssH === 0) return;

    canvas.width = Math.floor(cssW * dpr);
    canvas.height = Math.floor(cssH * dpr);

    const ctx = canvas.getContext('2d');
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, cssW, cssH);

    const { data, color, label, totalSteps, displayUpTo } = state;
    const n = Math.min(displayUpTo + 1, data.length);
    if (n < 2) return;

    const pad = { top: 16, bottom: 8, left: 6, right: 6 };
    const pW = cssW - pad.left - pad.right;
    const pH = cssH - pad.top - pad.bottom;
    const max = maxOf(data, data.length);
    if (max === 0) return;

    ctx.beginPath();
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.lineJoin = 'round';
    ctx.globalAlpha = 0.85;
    for (let i = 0; i < n; i++) {
        const x = pad.left + (i / (totalSteps - 1)) * pW;
        const y = pad.top + pH - (data[i] / max) * pH;
        i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
    }
    ctx.stroke();

    ctx.globalAlpha = 0.9;
    ctx.font = '10px sans-serif';
    ctx.textBaseline = 'top';
    ctx.fillStyle = color;
    ctx.fillText(label, 8, 3);
    ctx.globalAlpha = 1;
}
