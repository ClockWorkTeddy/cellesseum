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

function getPlotPoint(i, totalSteps, pW, padLeft, seriesTop, seriesHeight, value, max) {
    const x = padLeft + (i / (totalSteps - 1)) * pW;
    const y = seriesTop + seriesHeight - (value / max) * seriesHeight;
    return { x, y };
}

function drawVerticalGrid(ctx, label, totalSteps, pad, pW, axisY) {
    const gridStep = 125;
    ctx.beginPath();
    if (label == 'Plants') {
        ctx.strokeStyle = '#00bb0022';
    } else {
        ctx.strokeStyle = '#77bb0022';
    }
    ctx.lineWidth = 1;
    ctx.globalAlpha = 1;
    for (let tick = gridStep; tick < totalSteps; tick += gridStep) {
        const x = pad.left + (tick / (totalSteps - 1)) * pW + 0.5;
        ctx.moveTo(x, pad.top + 0.5);
        ctx.lineTo(x, axisY);
    }
    ctx.stroke();
}

function drawHorizontalGrid(ctx, axisX, pad, pW, pH) {
    const horizontalGridSegmentCount = 5;
    ctx.beginPath();
    ctx.lineWidth = 1;
    for (let segment = 1; segment < horizontalGridSegmentCount; segment++) {
        const y = pad.top + (segment / horizontalGridSegmentCount) * pH + 0.5;
        ctx.moveTo(axisX, y);
        ctx.lineTo(pad.left + pW + 0.5, y);
    }
    ctx.stroke();
}

function drawPlotBorder(ctx, axisX, axisY, pad, pW, color) {
    ctx.beginPath();
    ctx.strokeStyle = color;
    ctx.lineWidth = 1;
    ctx.globalAlpha = 0.25;
    ctx.moveTo(axisX, pad.top + 0.5);
    ctx.lineTo(axisX, axisY);
    ctx.lineTo(pad.left + pW + 0.5, axisY);
    ctx.lineTo(pad.left + pW + 0.5, pad.top + 0.5);
    ctx.stroke();
}

function drawAreaFill(ctx, points, baselineY, color) {
    if (points.length < 2) return;

    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.lineTo(points[points.length - 1].x, baselineY);
    ctx.lineTo(points[0].x, baselineY);
    ctx.closePath();
    ctx.fillStyle = color;
    ctx.globalAlpha = 0.1;
    ctx.fill();
}

function drawSeriesLine(ctx, points, color) {
    if (points.length < 2) return;

    ctx.beginPath();
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.lineJoin = 'round';
    ctx.globalAlpha = 0.85;
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.stroke();
    ctx.globalAlpha = 1;
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

    const pad = { top: 8, bottom: 8, left: 6, right: 6 };
    const pW = cssW - pad.left - pad.right;
    const pH = cssH - pad.top - pad.bottom;

    const axisX = pad.left + 0.5;
    const axisY = pad.top + pH + 0.5;
    const seriesTopInset = 10;
    const seriesTop = pad.top + seriesTopInset;
    const seriesHeight = pH - seriesTopInset;

    drawVerticalGrid(ctx, label, totalSteps, pad, pW, axisY);
    drawHorizontalGrid(ctx, axisX, pad, pW, pH);
    if (label === 'Plants') {
        drawPlotBorder(ctx, axisX, axisY, pad, pW, "#00bb00");
    } else {
        drawPlotBorder(ctx, axisX, axisY, pad, pW, "#77bb00");
    }
    

    if (n < 2) return;

    const max = maxOf(data, data.length);
    if (max === 0) return;

    const points = [];
    for (let i = 0; i < n; i++) {
        points.push(getPlotPoint(i, totalSteps, pW, pad.left, seriesTop, seriesHeight, data[i], max));
    }

    const baselineY = seriesTop + seriesHeight;
    drawAreaFill(ctx, points, baselineY, color);
    drawSeriesLine(ctx, points, color);
}
