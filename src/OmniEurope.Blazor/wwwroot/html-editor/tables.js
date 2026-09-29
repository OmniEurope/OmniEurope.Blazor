// Part of omni-html-editor.js: table editing (rows, columns, merged and split cells), the table a
// toolbar command inserts, and Tab moving between cells.
import { elementOf } from './model.js';

// The table as a grid of slots, each holding the cell that covers it, so row and column spans are
// honoured: places maps a cell to its anchor row and column and to its spans.
function tableGrid(table) {
    const rows = [...table.rows];
    const grid = rows.map(() => []);
    const places = new Map();
    rows.forEach((row, r) => {
        let c = 0;
        for (const cell of row.cells) {
            while (grid[r][c]) {
                c++;
            }

            const spanRows = Math.max(1, cell.rowSpan || 1);
            const spanCols = Math.max(1, cell.colSpan || 1);
            places.set(cell, { row: r, col: c, rows: spanRows, cols: spanCols });
            for (let i = 0; i < spanRows && r + i < rows.length; i++) {
                for (let j = 0; j < spanCols; j++) {
                    grid[r + i][c + j] = cell;
                }
            }

            c += spanCols;
        }
    });
    return { rows, grid, places, width: Math.max(0, ...grid.map(line => line.length)) };
}

function newCell(like) {
    const cell = document.createElement(like?.tagName === 'TH' ? 'th' : 'td');
    cell.appendChild(document.createElement('br'));
    return cell;
}

function setSpan(cell, name, value) {
    if (value > 1) {
        cell.setAttribute(name, String(value));
    }
    else {
        cell.removeAttribute(name);
    }
}

// Puts a cell in row r at column col: before the first cell of that row anchored at or after it.
function placeCell(info, r, cell, col) {
    const row = info.rows[r];
    const next = [...row.cells].find(other => other !== cell && (info.places.get(other)?.col ?? Infinity) >= col);
    row.insertBefore(cell, next ?? null);
}

function caretInto(cell) {
    const range = document.createRange();
    range.selectNodeContents(cell);
    range.collapse(true);
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
}

function hasContent(cell) {
    return cell.textContent.trim() !== '' || cell.querySelector('img,hr,table,aside') !== null;
}

export function editTable(cell, action, argument) {
    const table = cell.closest('table');
    if (!table) {
        return;
    }

    switch (action) {
        case 'addrowabove':
        case 'addrowbelow':
            addRow(table, cell, action === 'addrowbelow');
            break;
        case 'deleterow':
            deleteRow(table, cell);
            break;
        case 'addcolumnbefore':
        case 'addcolumnafter':
            addColumn(table, cell, action === 'addcolumnafter');
            break;
        case 'deletecolumn':
            deleteColumn(table, cell);
            break;
        case 'mergecellright': {
            const info = tableGrid(table);
            const place = info.places.get(cell);
            const right = info.grid[place.row][place.col + place.cols];
            if (right) {
                setCellSpan(table, cell, place.rows, place.cols + info.places.get(right).cols);
            }
            break;
        }
        case 'mergecelldown': {
            const info = tableGrid(table);
            const place = info.places.get(cell);
            const below = info.grid[place.row + place.rows]?.[place.col];
            if (below) {
                setCellSpan(table, cell, place.rows + info.places.get(below).rows, place.cols);
            }
            break;
        }
        case 'splitcell':
            splitCell(table, cell);
            break;
        case 'setcellspan': {
            // "rowsxcolumns", as for inserttable; without an argument the cell is split back to 1x1.
            const match = /^(\d{1,2})x(\d{1,2})$/.exec(argument ?? '');
            setCellSpan(table, cell, match ? Number(match[1]) : 1, match ? Number(match[2]) : 1);
            break;
        }
        default:
            return;
    }

    if (cell.isConnected) {
        caretInto(cell);
    }
}

function addRow(table, cell, below) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const r = below ? place.row + place.rows - 1 : place.row;
    const row = document.createElement('tr');
    for (let c = 0; c < info.width; c++) {
        const over = info.grid[r][c];
        if (!over) {
            row.appendChild(newCell(null));
            continue;
        }

        const at = info.places.get(over);
        c = at.col + at.cols - 1;
        // A cell crossing the new row's edge grows over it instead of getting a neighbour.
        if (below ? at.row + at.rows - 1 > r : at.row < r) {
            setSpan(over, 'rowspan', at.rows + 1);
            continue;
        }

        const added = newCell(over);
        setSpan(added, 'colspan', at.cols);
        row.appendChild(added);
    }

    if (below) {
        info.rows[r].after(row);
    }
    else {
        info.rows[r].before(row);
    }
}

function deleteRow(table, cell) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const r = place.row;
    if (info.rows.length === 1) {
        removeTable(table);
        return;
    }

    const seen = new Set();
    for (let c = 0; c < info.width; c++) {
        const over = info.grid[r][c];
        if (!over || seen.has(over)) {
            continue;
        }

        seen.add(over);
        const at = info.places.get(over);
        if (at.rows > 1) {
            setSpan(over, 'rowspan', at.rows - 1);
            if (at.row === r) {
                // Anchored in the row that goes: it moves down, keeping its column.
                placeCell(info, r + 1, over, at.col);
            }
        }
    }

    info.rows[r].remove();
    const landing = info.grid[r + 1]?.[place.col] ?? info.grid[r - 1]?.[place.col];
    if (landing?.isConnected) {
        caretInto(landing);
    }
}

function addColumn(table, cell, after) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const c = after ? place.col + place.cols - 1 : place.col;
    const grown = new Set();
    info.rows.forEach((row, r) => {
        const over = info.grid[r][c];
        if (!over) {
            row.appendChild(newCell(null));
            return;
        }

        const at = info.places.get(over);
        if (after ? at.col + at.cols - 1 > c : at.col < c) {
            if (!grown.has(over)) {
                grown.add(over);
                setSpan(over, 'colspan', at.cols + 1);
            }

            return;
        }

        // A cell spanning several rows gets one neighbour spanning the same rows, added once.
        if (at.row !== r) {
            return;
        }

        const added = newCell(over);
        setSpan(added, 'rowspan', at.rows);
        if (after) {
            over.after(added);
        }
        else {
            over.before(added);
        }
    });
}

function deleteColumn(table, cell) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const seen = new Set();
    let landing = null;
    for (let r = 0; r < info.rows.length; r++) {
        const over = info.grid[r][place.col];
        if (!over || seen.has(over)) {
            continue;
        }

        seen.add(over);
        const at = info.places.get(over);
        if (at.cols > 1) {
            setSpan(over, 'colspan', at.cols - 1);
        }
        else {
            over.remove();
        }
    }

    if (!table.querySelector('td,th')) {
        removeTable(table);
        return;
    }

    landing = info.grid[place.row][place.col + place.cols] ?? info.grid[place.row][place.col - 1];
    if (landing?.isConnected) {
        caretInto(landing);
    }
}

function splitCell(table, cell) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    if (place.rows === 1 && place.cols === 1) {
        return;
    }

    setSpan(cell, 'rowspan', 1);
    setSpan(cell, 'colspan', 1);
    for (let j = 1; j < place.cols; j++) {
        cell.after(newCell(cell));
    }

    for (let i = 1; i < place.rows && place.row + i < info.rows.length; i++) {
        for (let j = 0; j < place.cols; j++) {
            placeCell(info, place.row + i, newCell(cell), place.col);
        }
    }
}

// Makes the cell span rows x columns from where it is anchored, absorbing the content of the cells
// it now covers. Refused (nothing changes) when a covered cell reaches outside that area, or the
// area leaves the table: the table stays a rectangle.
function setCellSpan(table, cell, rows, cols) {
    let info = tableGrid(table);
    let place = info.places.get(cell);
    const spanRows = Math.max(1, Math.min(rows, info.rows.length - place.row));
    const spanCols = Math.max(1, Math.min(cols, info.width - place.col));
    for (let r = place.row; r < place.row + spanRows; r++) {
        for (let c = place.col; c < place.col + spanCols; c++) {
            const over = info.grid[r][c];
            if (!over) {
                return;
            }

            const at = info.places.get(over);
            if (over !== cell && (at.row < place.row || at.col < place.col
                || at.row + at.rows > place.row + spanRows || at.col + at.cols > place.col + spanCols)) {
                return;
            }
        }
    }

    splitCell(table, cell);
    info = tableGrid(table);
    place = info.places.get(cell);
    const absorbed = new Set();
    for (let r = place.row; r < place.row + spanRows; r++) {
        for (let c = place.col; c < place.col + spanCols; c++) {
            const over = info.grid[r][c];
            if (over && over !== cell) {
                absorbed.add(over);
            }
        }
    }

    for (const other of absorbed) {
        if (hasContent(other)) {
            if (!hasContent(cell)) {
                cell.replaceChildren();
            }
            else {
                cell.appendChild(document.createElement('br'));
            }

            cell.append(...other.childNodes);
        }

        other.remove();
    }

    setSpan(cell, 'rowspan', spanRows);
    setSpan(cell, 'colspan', spanCols);
}

function removeTable(table) {
    const paragraph = document.createElement('p');
    paragraph.appendChild(document.createElement('br'));
    table.replaceWith(paragraph);
    caretInto(paragraph);
}

export function tableHtml(argument) {
    const match = /^(\d{1,2})x(\d{1,2})$/.exec(argument);
    const rows = match ? Math.max(1, Number(match[1])) : 3;
    const columns = match ? Math.max(1, Number(match[2])) : 3;
    const row = `<tr>${'<td><br></td>'.repeat(columns)}</tr>`;
    return `<table><tbody>${row.repeat(rows)}</tbody></table><p><br></p>`;
}

export function moveBetweenCells(surface, backwards) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return false;
    }

    const cell = elementOf(selection.getRangeAt(0).startContainer)?.closest('td,th');
    const table = cell?.closest('table');
    if (!cell || !table || !surface.contains(table)) {
        return false;
    }

    const cells = [...table.querySelectorAll('td,th')];
    const next = cells[cells.indexOf(cell) + (backwards ? -1 : 1)];
    if (!next) {
        return false;
    }

    const range = document.createRange();
    range.selectNodeContents(next);
    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
    return true;
}
