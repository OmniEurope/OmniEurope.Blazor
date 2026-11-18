// The streams of the OmniMediaTile demonstration, made in the page's own module as an application would make them: the
// tile only receives a reference to the MediaStream and shows it.
//
// The demo stream is a canvas that is drawn on a timer and captured with canvas.captureStream(30): moving shapes, a
// clock and a frame around the picture, so that a viewer sees at once that it plays, how the picture sits in the tile and
// where its edges are. It needs no permission. The camera needs one (getUserMedia) and may be refused or missing.
const timers = new WeakMap();

function paint(context, width, height, label, now) {
    const seconds = now / 1000;
    const gradient = context.createLinearGradient(0, 0, width, height);
    gradient.addColorStop(0, '#1d3557');
    gradient.addColorStop(1, '#457b9d');
    context.fillStyle = gradient;
    context.fillRect(0, 0, width, height);

    for (let index = 0; index < 4; index++) {
        const angle = seconds * (0.6 + index * 0.35) + index * 1.7;
        context.fillStyle = ['#2a9d8f', '#f1faee', '#ffb703', '#8ecae6'][index];
        context.beginPath();
        context.arc(width / 2 + Math.cos(angle) * width * 0.3, height / 2 + Math.sin(angle * 1.3) * height * 0.28, 18 + index * 6, 0, Math.PI * 2);
        context.fill();
    }

    context.fillStyle = '#ffffff';
    context.font = `bold ${Math.round(height * 0.12)}px sans-serif`;
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    const date = new Date();
    const pad = value => String(value).padStart(2, '0');
    context.fillText(`${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`, width / 2, height / 2);
    context.font = `${Math.round(height * 0.05)}px sans-serif`;
    context.fillText(label, width / 2, height * 0.2);

    // The frame of the picture: its edges are where a drawing layer starts and stops.
    context.strokeStyle = '#ffd60a';
    context.lineWidth = 16;
    context.strokeRect(8, 8, width - 16, height - 16);
}

/** A MediaStream of an animated canvas, width x height, at 30 frames a second. */
export function createDemoStream(width, height, label) {
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d');
    const draw = () => paint(context, width, height, label, performance.now());
    draw();
    const stream = canvas.captureStream(30);
    timers.set(stream, setInterval(draw, 33));
    return stream;
}

/** The camera, once the visitor agrees. Throws when it is refused or there is none. */
export async function useCamera() {
    if (!navigator.mediaDevices?.getUserMedia) {
        throw new Error('This browser or this page cannot reach a camera.');
    }

    return await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
}

/** Stops a stream this module made: its timer and its tracks. The application owns its streams, the tile never stops them. */
export function stopStream(stream) {
    const timer = timers.get(stream);
    if (timer !== undefined) {
        clearInterval(timer);
        timers.delete(stream);
    }

    stream.getTracks().forEach(track => track.stop());
}

/** The size of the first video track of the stream, for the page to show. */
export function describeStream(stream) {
    const settings = stream.getVideoTracks()[0]?.getSettings?.() ?? {};
    return settings.width && settings.height ? `${settings.width} by ${settings.height}` : 'unknown size';
}
