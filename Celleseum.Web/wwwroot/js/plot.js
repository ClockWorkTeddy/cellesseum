const plots = new Map();

export function initPlot(canvasId, label, totalSteps, windowSize = null, verticalGridSize = 100) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;

    const { color, gridColor, seriesColors } = resolvePlotColors(canvas);
    const normalizedWindowSize = Number.isFinite(windowSize) && windowSize > 0 ? Math.floor(windowSize) : null;

    const onFrame = (e) => {
        const state = plots.get(canvasId);
        if (!state) return;
        state.displayUpTo = e.detail.frame;
        redraw(canvasId);
    };

    window.addEventListener('celleseum:frame', onFrame);
    plots.set(canvasId, { seriesData: [], color, gridColor, seriesColors, label, totalSteps, windowSize: normalizedWindowSize, displayUpTo: -1, onFrame, verticalGridSize });
    return true;
}

export function pushFrames(canvasId, frames) {
    const state = plots.get(canvasId);
    if (!state || !Array.isArray(frames)) return;

    if (!Array.isArray(state.seriesData) || state.seriesData.length !== frames.length) {
        state.seriesData = new Array(frames.length);
        for (let s = 0; s < frames.length; s++) {
            state.seriesData[s] = [];
        }
    }

    for (let s = 0; s < frames.length; s++) {
        const source = frames[s];
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

function maxOfSeries(seriesList, start, end) {
    let m = 0;
    for (let s = 0; s < seriesList.length; s++) {
        const series = seriesList[s];
        if (!Array.isArray(series)) continue;

        const limit = Math.min(end, series.length);
        for (let i = start; i < limit; i++) {
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
    return { x, y, value };
}

function drawVerticalGrid(ctx, gridColor, visibleStart, plotStepCount, pad, gridWidth, stepSpanWidth, axisY) {
    const gridStep = 50;
    if (plotStepCount < 2 || stepSpanWidth <= 0 || gridWidth <= 0) {
        return;
    }

    const denominator = plotStepCount - 1;
    const maxLocalTick = (gridWidth / stepSpanWidth) * denominator;
    const firstTick = Math.max(gridStep, Math.ceil(visibleStart / gridStep) * gridStep);
    const lastTick = Math.floor((visibleStart + maxLocalTick) / gridStep) * gridStep;
    if (firstTick > lastTick) {
        return;
    }

    ctx.beginPath();
    ctx.strokeStyle = gridColor;
    ctx.lineWidth = 1;
    ctx.globalAlpha = 1;

    for (let tick = firstTick; tick <= lastTick; tick += gridStep) {
        const localTick = tick - visibleStart;
        const x = pad.left + (localTick / denominator) * stepSpanWidth + 0.5;
        ctx.moveTo(x, pad.top + 0.5);
        ctx.lineTo(x, axisY);
    }

    ctx.stroke();
}

function drawHorizontalGrid(ctx, gridColor, axisX, pad, pW, seriesTop, seriesHeight, max, verticalGridSize) {
    if (max <= 0) {
        return;
    }

    const horizontalGridLineCount = Math.floor(max / verticalGridSize);
    if (horizontalGridLineCount < 1) {
        return;
    }

    ctx.beginPath();
    ctx.strokeStyle = gridColor;
    ctx.lineWidth = 1;
    ctx.globalAlpha = 1;
    for (let line = 1; line <= horizontalGridLineCount; line++) {
        const value = line * verticalGridSize;
        const y = seriesTop + seriesHeight - (value / max) * seriesHeight + 0.5;
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

function getSeriesColor(seriesIndex, color, seriesColors) {
    return seriesIndex === 0 ? color : (seriesColors[seriesIndex - 1] || color);
}

function drawCurrentPositionMarker(ctx, markerX, topY, bottomY, markerColor) {
    ctx.beginPath();
    ctx.strokeStyle = markerColor;
    ctx.lineWidth = 1;
    ctx.globalAlpha = 1;
    ctx.moveTo(markerX, topY + 0.5);
    ctx.lineTo(markerX, bottomY);
    ctx.stroke();
}

function drawSeriesSegments(ctx, points, color, lineWeight, baselineY, fillColor) {
    let segment = [];

    const drawSegment = () => {
        if (segment.length < 2) {
            segment = [];
            return;
        }

        if (fillColor) {
            ctx.beginPath();
            ctx.moveTo(segment[0].x, segment[0].y);
            for (let i = 1; i < segment.length; i++) {
                ctx.lineTo(segment[i].x, segment[i].y);
            }
            ctx.lineTo(segment[segment.length - 1].x, baselineY);
            ctx.lineTo(segment[0].x, baselineY);
            ctx.closePath();
            ctx.fillStyle = fillColor;
            ctx.globalAlpha = 0.1;
            ctx.fill();
        }

        ctx.beginPath();
        ctx.strokeStyle = color;
        ctx.lineWidth = lineWeight;
        ctx.lineJoin = 'round';
        ctx.globalAlpha = 0.85;
        ctx.moveTo(segment[0].x, segment[0].y);
        for (let i = 1; i < segment.length; i++) {
            ctx.lineTo(segment[i].x, segment[i].y);
        }
        ctx.stroke();
        ctx.globalAlpha = 1;
        segment = [];
    };

    for (let i = 0; i < points.length; i++) {
        const point = points[i];
        if (!point || point.value === 0) {
            drawSegment();
            continue;
        }

        segment.push(point);
    }

    drawSegment();
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

    const { seriesData, color, gridColor, seriesColors, totalSteps, windowSize, displayUpTo, verticalGridSize } = state;
    const hasSeriesData = Array.isArray(seriesData) && seriesData.length > 0;
    const allSeries = hasSeriesData ? [...seriesData] : [];

    let availablePointCount = 0;
    for (let i = 0; i < allSeries.length; i++) {
        const series = allSeries[i];
        if (Array.isArray(series) && series.length > availablePointCount) {
            availablePointCount = series.length;
        }
    }

    const visibleEnd = Math.min(displayUpTo + 1, availablePointCount);
    const visibleStart = (() => {
        if (!windowSize) {
            return 0;
        }

        const slidingStart = Math.max(0, visibleEnd - windowSize);
        const terminalFreezeStart = Math.max(0, totalSteps - (windowSize * 2));
        return Math.min(slidingStart, terminalFreezeStart);
    })();
    const visibleCount = Math.max(0, visibleEnd - visibleStart);

    const pad = { top: 8, bottom: 8, left: 6, right: 6 };
    const pW = cssW - pad.left - pad.right;
    const pH = cssH - pad.top - pad.bottom;

    const axisX = pad.left + 0.5;
    const axisY = pad.top + pH + 0.5;
    const seriesTopInset = 10;
    const seriesTop = pad.top + seriesTopInset;
    const seriesHeight = pH - seriesTopInset;
    const plotStepCount = windowSize ? Math.max(windowSize, 2) : totalSteps;
    const plotSpanWidth = windowSize ? pW * 0.5 : pW;
    let lineWeight = 1;
    drawVerticalGrid(ctx, gridColor, visibleStart, plotStepCount, pad, pW, plotSpanWidth, axisY);
    drawPlotBorder(ctx, axisX, axisY, pad, pW, color);

    if (visibleCount < 2 || !hasSeriesData) return;

    const max = maxOfSeries(allSeries, visibleStart, visibleEnd);
    if (max === 0) return;

    drawHorizontalGrid(ctx, gridColor, axisX, pad, pW, seriesTop, seriesHeight, max, verticalGridSize);

    for (let seriesIndex = 0; seriesIndex < allSeries.length; seriesIndex++) {
        const series = allSeries[seriesIndex];
        if (!Array.isArray(series)) continue;

        const seriesLength = Math.min(visibleEnd, series.length);
        if (seriesLength < 2) continue;

        const points = [];
        for (let i = visibleStart; i < seriesLength; i++) {
            points.push(getPlotPoint(i - visibleStart, plotStepCount, plotSpanWidth, pad.left, seriesTop, seriesHeight, series[i], max));
        }

        const seriesColor = getSeriesColor(seriesIndex, color, seriesColors);
        if (seriesIndex === 0) {
            lineWeight = 2;
        }

        const baselineY = seriesTop + seriesHeight;
        drawSeriesSegments(ctx, points, seriesColor, lineWeight, baselineY, seriesIndex === 0 ? color : null);
    }

    const latestIndex = visibleEnd - 1;
    let markerColor = null;
    let markerValue = Number.NEGATIVE_INFINITY;

    for (let seriesIndex = 0; seriesIndex < allSeries.length; seriesIndex++) {
        const series = allSeries[seriesIndex];
        if (!Array.isArray(series) || latestIndex >= series.length) continue;

        const value = series[latestIndex];
        if (!Number.isFinite(value)) continue;

        if (value > markerValue) {
            markerValue = value;
            markerColor = getSeriesColor(seriesIndex, color, seriesColors);
        }
    }

    if (markerColor) {
        const markerRatio = (visibleCount - 1) / (plotStepCount - 1);
        const markerX = pad.left + markerRatio * plotSpanWidth + 0.5;
        drawCurrentPositionMarker(ctx, markerX, pad.top, axisY, markerColor);
    }
}
