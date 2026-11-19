// Video tile of OmniMediaTile.
//
// The application owns the MediaStream (a WebRTC peer, a screen share, a camera): .NET passes a reference to it and
// this module only sets it as the srcObject of the tile's <video>; it never stops, clones or adds a track. It also
// keeps the one thing CSS cannot: where the video picture really is inside the element. The <video> fills the stage
// and object-fit (contain or cover, data-omni-media-fit on the root) draws the picture inside it, so the picture is the
// element box minus the letterbox bands (contain) or larger than the box (cover). From the box and videoWidth x
// videoHeight (loadedmetadata, resize, a ResizeObserver on the video) this module computes that rectangle and writes it
// as the custom properties --omni-media-picture-left, -top, -width and -height on the picture element (the drawing
// overlay), through the CSSOM, the one path a strict style-src leaves open. Without a picture the properties are removed and the
// overlay has no size. The strokes of the overlay are 0..1 of that rectangle, so they mean the same on every tile size.
//
// Full screen is the tile root's, so the overlay and the chips go with the video; the state is told to .NET
// (OnFullscreenChanged) on every change, whoever caused it (the button, Escape, the browser). Export draws the current
// video frame at its native resolution, capped at 8192 on the longest side, then the strokes with the renderer the
// drawing pad uses (shared/ink-render.js).
import { unpack, paint, THEME_COLOR } from './shared/ink-render.js';
import { circuitDown } from './shared/notify.js';

const MAX_SIDE = 8192;
// A stroke in the theme colour is black in an export: there is no theme in an image.
const EXPORT_THEME_COLOR = '#000000';
const PICTURE_PROPERTIES = ['left', 'top', 'width', 'height'].map(name => `--omni-media-picture-${name}`);
const tiles = new WeakMap();

/**
 * The rectangle the video picture occupies in a box of boxWidth x boxHeight, relative to the box, for a video of
 * mediaWidth x mediaHeight: fit "cover" fills the box and overflows it on one axis, anything else ("contain") keeps the
 * whole picture and leaves bands on one axis. Centred, as object-position is by default. Null when a size is not above zero.
 */
export function pictureRect(boxWidth, boxHeight, mediaWidth, mediaHeight, fit) {
    if (!(boxWidth > 0 && boxHeight > 0 && mediaWidth > 0 && mediaHeight > 0)) {
        return null;
    }

    const horizontal = boxWidth / mediaWidth;
    const vertical = boxHeight / mediaHeight;
    const scale = fit === 'cover' ? Math.max(horizontal, vertical) : Math.min(horizontal, vertical);
    const width = mediaWidth * scale;
    const height = mediaHeight * scale;
    return { left: (boxWidth - width) / 2, top: (boxHeight - height) / 2, width, height };
}

/** The export size of a frame of width x height: itself, or scaled down with its ratio so the longest side is MAX_SIDE. */
export function exportSize(width, height) {
    const longest = Math.max(width, height);
    if (longest <= MAX_SIDE) {
        return { width, height };
    }

    const scale = MAX_SIDE / longest;
    return { width: Math.max(1, Math.round(width * scale)), height: Math.max(1, Math.round(height * scale)) };
}

const hundredth = value => Math.round(value * 100) / 100;

function layout(tile) {
    const picture = tile.root.querySelector('[data-omni-media-picture]');
    if (!picture) {
        return;
    }

    const box = tile.video.getBoundingClientRect();
    const rect = tile.video.srcObject
        ? pictureRect(box.width, box.height, tile.video.videoWidth, tile.video.videoHeight, tile.root.dataset.omniMediaFit)
        : null;
    const values = rect ? [rect.left, rect.top, rect.width, rect.height] : null;
    PICTURE_PROPERTIES.forEach((name, index) => {
        if (values) {
            picture.style.setProperty(name, `${hundredth(values[index])}px`);
        } else {
            picture.style.removeProperty(name);
        }
    });
}

function tell(tile, method, ...values) {
    // No call while Blazor Server shows its connection as lost: it could not arrive, and Blazor would report
    // its failed send as an uncaught rejection, out of reach of the catch below.
    if (circuitDown()) {
        return;
    }

    try {
        const pending = tile.dotnet.invokeMethodAsync(method, ...values);
        pending?.catch?.(() => { });
    } catch {
        // The component is gone; there is nobody left to tell.
    }
}

// Playing may be refused (a sound that was not asked for by a gesture): the next press or key retries, once.
function play(tile) {
    tile.release?.();
    const attempt = tile.video.play();
    attempt?.catch?.(error => {
        if (error?.name !== 'NotAllowedError') {
            return;
        }

        const retry = () => {
            tile.release?.();
            tile.video.play()?.catch?.(() => { });
        };
        document.addEventListener('pointerdown', retry);
        document.addEventListener('keydown', retry);
        tile.release = () => {
            document.removeEventListener('pointerdown', retry);
            document.removeEventListener('keydown', retry);
            tile.release = null;
        };
    });
}

/** Starts following the video, its box and the full-screen state of the tile root. */
export function attach(root, dotnet) {
    const video = root?.querySelector('video');
    if (!video || tiles.has(root)) {
        return;
    }

    const tile = { root, video, dotnet, fullscreen: false, release: null };
    const relayout = () => layout(tile);
    const onFullscreen = () => {
        const now = document.fullscreenElement === root;
        if (now !== tile.fullscreen) {
            tile.fullscreen = now;
            tell(tile, 'OnFullscreenChanged', now);
        }
    };

    video.addEventListener('loadedmetadata', relayout);
    video.addEventListener('resize', relayout);
    video.addEventListener('emptied', relayout);
    document.addEventListener('fullscreenchange', onFullscreen);
    const observer = new ResizeObserver(relayout);
    observer.observe(video);
    tile.stop = () => {
        video.removeEventListener('loadedmetadata', relayout);
        video.removeEventListener('resize', relayout);
        video.removeEventListener('emptied', relayout);
        document.removeEventListener('fullscreenchange', onFullscreen);
        observer.disconnect();
        tile.release?.();
    };
    tiles.set(root, tile);
}

/** Stops following and lets go of the stream: srcObject is cleared, the tracks are left running (they are the application's). */
export function detach(root) {
    const tile = root ? tiles.get(root) : undefined;
    if (!tile) {
        return;
    }

    tile.stop();
    tile.video.srcObject = null;
    tiles.delete(root);
}

/** Shows the stream (a MediaStream the application made), or no stream when it is null. */
export function setStream(root, stream) {
    const tile = root ? tiles.get(root) : undefined;
    if (!tile) {
        return;
    }

    tile.video.srcObject = stream ?? null;
    if (stream) {
        play(tile);
    }

    layout(tile);
}

/** The sound of the video: muted, or not. */
export function setMuted(root, muted) {
    const tile = root ? tiles.get(root) : undefined;
    if (tile) {
        tile.video.muted = muted;
    }
}

/** Places the drawing overlay again: the fit or the overlay itself changed. */
export function refresh(root) {
    const tile = root ? tiles.get(root) : undefined;
    if (tile) {
        layout(tile);
    }
}

/**
 * Enters or leaves full screen for the tile root. A refusal (no user gesture, no support) is not an error here: the
 * result says whether the tile is full screen afterwards, and the change event keeps .NET informed.
 */
export async function toggleFullscreen(root) {
    try {
        if (document.fullscreenElement === root) {
            await document.exitFullscreen();
        } else if (root?.requestFullscreen) {
            await root.requestFullscreen();
        }
    } catch {
        // Refused by the browser: reported by the returned state.
    }

    return document.fullscreenElement === root;
}

/**
 * The current video frame at its native size, the strokes over it, as the bytes of a PNG. Null when there is no
 * frame to draw yet (no stream, or no picture decoded).
 */
export async function exportPng(root, colors, widths, points) {
    const video = root ? tiles.get(root)?.video : undefined;
    if (!video || !video.srcObject || video.readyState < 2 || !(video.videoWidth > 0 && video.videoHeight > 0)) {
        return null;
    }

    const { width, height } = exportSize(video.videoWidth, video.videoHeight);
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d');
    context.drawImage(video, 0, 0, width, height);
    paint(context, unpack(colors, widths, points), width, height, color => (color === THEME_COLOR ? EXPORT_THEME_COLOR : color));
    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
    if (!blob) {
        throw new Error('The browser could not encode the video frame as a PNG image.');
    }

    return new Uint8Array(await blob.arrayBuffer());
}
