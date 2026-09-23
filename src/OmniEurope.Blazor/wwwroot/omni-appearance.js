// The text scale is attached to the document root because component sizes are expressed in rem.
export function setTextSizeLevel(level) {
    const normalized = Number.isInteger(level) ? Math.min(10, Math.max(1, level)) : 5;
    document.documentElement.dataset.oeTextSize = String(normalized);
}

export function clearTextSizeLevel() {
    delete document.documentElement.dataset.oeTextSize;
}
