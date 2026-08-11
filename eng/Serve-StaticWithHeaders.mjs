import { createReadStream } from 'node:fs';
import { readFile, stat } from 'node:fs/promises';
import { createServer } from 'node:http';
import { extname, resolve, sep } from 'node:path';

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const root = resolve(options.get('--root') ?? '');
const port = Number(options.get('--port') ?? '5190');
if (!root || !Number.isInteger(port) || port < 1 || port > 65535) {
  throw new Error('Usage: node Serve-StaticWithHeaders.mjs --root <directory> --port <port>');
}

const headerText = await readFile(resolve(root, '_headers'), 'utf8');
const headerRules = [];
let currentRule;
for (const rawLine of headerText.split(/\r?\n/)) {
  const line = rawLine.trim();
  if (!line) continue;

  if (!/^\s/.test(rawLine)) {
    currentRule = { pattern: line, headers: {} };
    headerRules.push(currentRule);
    continue;
  }

  if (!currentRule) throw new Error(`Header without deployment path: ${line}`);
  const separator = line.indexOf(':');
  if (separator < 1) throw new Error(`Invalid deployment header: ${line}`);
  currentRule.headers[line.slice(0, separator).trim()] = line.slice(separator + 1).trim();
}

function matchesPattern(pathname, pattern) {
  const expression = `^${pattern
    .replace(/[.+?^${}()|[\]\\]/g, '\\$&')
    .replaceAll('*', '.*')}$`;
  return new RegExp(expression).test(pathname);
}

function headersFor(pathname) {
  return Object.assign(
    {},
    ...headerRules.filter(rule => matchesPattern(pathname, rule.pattern)).map(rule => rule.headers));
}

const contentTypes = new Map([
  ['.css', 'text/css; charset=utf-8'],
  ['.dat', 'application/octet-stream'],
  ['.dll', 'application/octet-stream'],
  ['.html', 'text/html; charset=utf-8'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.pdb', 'application/octet-stream'],
  ['.wasm', 'application/wasm']
]);

const server = createServer(async (request, response) => {
  try {
    const pathname = decodeURIComponent(new URL(request.url ?? '/', 'http://127.0.0.1').pathname);
    const relative = pathname === '/' ? 'index.html' : pathname.replace(/^\/+/, '');
    const deploymentPath = `/${relative}`;
    const deploymentHeaders = headersFor(deploymentPath);
    const file = resolve(root, relative);
    if (file !== root && !file.startsWith(`${root}${sep}`)) {
      response.writeHead(403).end();
      return;
    }

    const details = await stat(file);
    if (!details.isFile()) throw new Error('Not a file');
    response.writeHead(200, {
      ...deploymentHeaders,
      'Content-Type': contentTypes.get(extname(file).toLowerCase()) ?? 'application/octet-stream',
      'Content-Length': details.size
    });
    createReadStream(file).pipe(response);
  } catch {
    response.writeHead(404, headersFor(decodeURIComponent(new URL(request.url ?? '/', 'http://127.0.0.1').pathname))).end('Not found');
  }
});

server.listen(port, '127.0.0.1', () => {
  console.log(`Static deployment host ready on http://127.0.0.1:${port}`);
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => {
    server.closeAllConnections();
    server.close(() => process.exit(0));
  });
}
