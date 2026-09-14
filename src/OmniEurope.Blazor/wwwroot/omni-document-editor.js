// File export of OmniDocumentEditor: the one thing it needs the browser for, handing a file to the
// user. The content is built in .NET; this only wraps it in a Blob and follows a download link,
// which navigates nowhere and needs no permission from the content security policy.
export function download(fileName, mimeType, content) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.hidden = true;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 0);
}
