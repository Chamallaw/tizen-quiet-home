/*
 * Navigation au focus sur des rangées horizontales (logique pure).
 * Mémorise la colonne de chaque rangée, comme les lanceurs TV classiques.
 * État : { row, cols: { [rowIndex]: colIndex } }
 */
(function (root) {
    'use strict';

    function clamp(v, min, max) { return v < min ? min : v > max ? max : v; }

    function nonEmptyRows(rows) {
        var out = [];
        rows.forEach(function (r, i) { if (r.items.length) { out.push(i); } });
        return out;
    }

    function initial(rows) {
        var first = nonEmptyRows(rows)[0];
        return { row: first === undefined ? 0 : first, cols: {} };
    }

    // Recale l'état après une reconstruction des rangées (apps ajoutées/supprimées).
    function normalize(state, rows) {
        var valid = nonEmptyRows(rows);
        if (!valid.length) { return { row: 0, cols: {} }; }
        var row = valid.indexOf(state.row) !== -1 ? state.row : valid[0];
        var cols = {};
        valid.forEach(function (r) {
            cols[r] = clamp(state.cols[r] || 0, 0, rows[r].items.length - 1);
        });
        return { row: row, cols: cols };
    }

    function move(state, rows, dir) {
        var s = normalize(state, rows);
        var valid = nonEmptyRows(rows);
        if (!valid.length) { return s; }
        var col = s.cols[s.row] || 0;

        if (dir === 'left') { s.cols[s.row] = clamp(col - 1, 0, rows[s.row].items.length - 1); }
        else if (dir === 'right') { s.cols[s.row] = clamp(col + 1, 0, rows[s.row].items.length - 1); }
        else if (dir === 'up' || dir === 'down') {
            var pos = valid.indexOf(s.row) + (dir === 'up' ? -1 : 1);
            if (pos >= 0 && pos < valid.length) { s.row = valid[pos]; }
        }
        return s;
    }

    function current(state, rows) {
        var r = rows[state.row];
        if (!r || !r.items.length) { return null; }
        return r.items[state.cols[state.row] || 0] || null;
    }

    var api = { initial: initial, normalize: normalize, move: move, current: current };
    root.TH = root.TH || {};
    root.TH.focus = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof window !== 'undefined' ? window : globalThis);
