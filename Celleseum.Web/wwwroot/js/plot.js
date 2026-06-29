const plots = new Map();

export function initPlot(canvasId, label, totalSteps) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;

    const { color, gridColor, seriesColors } = resolvePlotColors(canvas);

    const onFrame = (e) => {
        const state = plots.get(canvasId);
        if (!state) return;
        state.displayUpTo = e.detail.frame;
        redraw(canvasId);
    };

    window.addEventListener('celleseum:frame', onFrame);
    plots.set(canvasId, { data: [], seriesData: [], color, gridColor, seriesColors, label, totalSteps, displayUpTo: -1, onFrame });
    return true;
}

export function pushFrames(canvasId, counts) {
    const state = plots.get(canvasId);
    if (!state) return;
    for (let i = 1; i < counts.length; i++) {
        state.data.push(counts[i]);
    }
}

export function pushSeriesFrames(canvasId, seriesCounts) {
    const state = plots.get(canvasId);
    if (!state || !Array.isArray(seriesCounts)) return;

    if (!Array.isArray(state.seriesData) || state.seriesData.length !== seriesCounts.length) {
        state.seriesData = new Array(seriesCounts.length);
        for (let s = 0; s < seriesCounts.length; s++) {
            state.seriesData[s] = [];
        }
    }

    for (let s = 0; s < seriesCounts.length; s++) {
        const source = seriesCounts[s];
        if (!Array.isArray(source)) continue;

        const target = state.seriesData[s];
        for (let i = 0; i < source.length; i++) {
            target.push(source[i]);
        }
    }
}

export function resetPlot(canvasId) {
    const state = plots.get(canvasId);
    if (!state) return;
    state.data = [];
    state.seriesData = [];
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

function maxOfSeries(seriesList, n) {
    let m = 0;
    for (let s = 0; s < seriesList.length; s++) {
        const series = seriesList[s];
        if (!Array.isArray(series)) continue;

        const limit = Math.min(n, series.length);
        for (let i = 0; i < limit; i++) {
            if (series[i] > m) m = series[i];
        }
    }

    return m;
}

function resolvePlotColors(canvas) {
    const styles = getComputedStyle(canvas);
    const color = styles.getPropertyValue('--plot-series-color').trim() || '#00bb00';
    const gridColor = styles.getPropertyValue('--plot-grid-color').trim() || `${color}22`;
    const rawSeriesColors = styles.getPropertyValue('--plot-series-colors').trim();
    const seriesColors = rawSeriesColors
        ? rawSeriesColors.split('|').map((c) => c.trim()).filter((c) => c.length > 0)
        : [];

    return { color, gridColor, seriesColors };
}

function getPlotPoint(i, totalSteps, pW, padLeft, seriesTop, seriesHeight, value, max) {
    const x = padLeft + (i / (totalSteps - 1)) * pW;
    const y = seriesTop + seriesHeight - (value / max) * seriesHeight;
    return { x, y };
}

function drawVerticalGrid(ctx, gridColor, totalSteps, pad, pW, axisY) {
    const gridStep = 125;
    ctx.beginPath();
    ctx.strokeStyle = gridColor;
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

function drawSeriesLine(ctx, points, color, lineWeight) {
    if (points.length < 2) return;

    ctx.beginPath();
    ctx.strokeStyle = color;
    ctx.lineWidth = lineWeight;
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

    const { data, seriesData, color, gridColor, seriesColors, totalSteps, displayUpTo } = state;
    const hasPrimarySeries = Array.isArray(data) && data.length > 0;
    const hasExtraSeries = Array.isArray(seriesData) && seriesData.length > 0;
    const allSeries = hasExtraSeries
        ? (hasPrimarySeries ? [data, ...seriesData] : [...seriesData])
        : (hasPrimarySeries ? [data] : []);

    let availablePointCount = 0;
    for (let i = 0; i < allSeries.length; i++) {
        const series = allSeries[i];
        if (Array.isArray(series) && series.length > availablePointCount) {
            availablePointCount = series.length;
        }
    }
    const n = Math.min(displayUpTo + 1, availablePointCount);

    const pad = { top: 8, bottom: 8, left: 6, right: 6 };
    const pW = cssW - pad.left - pad.right;
    const pH = cssH - pad.top - pad.bottom;

    const axisX = pad.left + 0.5;
    const axisY = pad.top + pH + 0.5;
    const seriesTopInset = 10;
    const seriesTop = pad.top + seriesTopInset;
    const seriesHeight = pH - seriesTopInset;
    let  lineWeight = 1;
    drawVerticalGrid(ctx, gridColor, totalSteps, pad, pW, axisY);
    drawHorizontalGrid(ctx, axisX, pad, pW, pH);
    drawPlotBorder(ctx, axisX, axisY, pad, pW, color);

    if (n < 2) return;

    const max = maxOfSeries(allSeries, n);
    if (max === 0) return;

    if (!hasExtraSeries) {
        const points = [];
        for (let i = 0; i < n; i++) {
            points.push(getPlotPoint(i, totalSteps, pW, pad.left, seriesTop, seriesHeight, data[i], max));
        }

        const baselineY = seriesTop + seriesHeight;
        lineWeight = 2;
        drawAreaFill(ctx, points, baselineY, color);
        drawSeriesLine(ctx, points, color, lineWeight);
        return;
    }

    for (let seriesIndex = 0; seriesIndex < allSeries.length; seriesIndex++) {
        const series = allSeries[seriesIndex];
        if (!Array.isArray(series)) continue;

        const seriesLength = Math.min(n, series.length);
        if (seriesLength < 2) continue;

        const points = [];
        for (let i = 0; i < seriesLength; i++) {
            points.push(getPlotPoint(i, totalSteps, pW, pad.left, seriesTop, seriesHeight, series[i], max));
        }

        const seriesColor = hasPrimarySeries
            ? (seriesIndex === 0 ? color : (seriesColors[seriesIndex - 1] || color))
            : (seriesColors[seriesIndex] || color);
        drawSeriesLine(ctx, points, seriesColor, lineWeight);
    }
}
