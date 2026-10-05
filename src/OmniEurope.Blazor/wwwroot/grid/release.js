// What every grid module does when .NET detaches it: the attachment of that element is disposed, and
// so is the attachment of any element that has left the page. A grid being disposed has already been
// removed when its detach runs, and its reference then arrives as null: without the sweep, the
// listeners a module set on the document or the window would stay for good, one more set per visit.
export function release(attachments, element, dispose = attachment => attachment.dispose()) {
    for (const [attached, attachment] of attachments) {
        if (attached === element || !attached.isConnected) {
            dispose(attachment);
            attachments.delete(attached);
        }
    }
}
