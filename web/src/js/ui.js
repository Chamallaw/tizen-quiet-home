/*
 * Rendu DOM. Reconstruit les rangées quand les données changent ; un simple
 * déplacement de focus ne touche qu'aux classes CSS et au décalage des rangées.
 */
(function (root) {
    'use strict';

    var doc = root.document;
    var PALETTE = ['#e53935', '#8e24aa', '#3949ab', '#039be5', '#00897b', '#7cb342', '#fb8c00', '#6d4c41'];
    var TILE_STRIDE = 260 + 28; // largeur tuile + gap (cf. style.css)
    var VISIBLE_TILES = 5;
    var toastTimer = null;

    function colorFor(name) {
        var h = 0;
        for (var i = 0; i < name.length; i++) { h = (h * 31 + name.charCodeAt(i)) >>> 0; }
        return PALETTE[h % PALETTE.length];
    }

    function initials(name) {
        var words = String(name).replace(/[^\w\s+]/g, ' ').trim().split(/\s+/);
        return ((words[0] || '?').charAt(0) + (words[1] ? words[1].charAt(0) : '')).toUpperCase();
    }

    function fallbackIcon(item) {
        var el = doc.createElement('div');
        el.className = 'initials';
        el.style.background = colorFor(item.name);
        el.textContent = initials(item.name);
        return el;
    }

    function tileEl(item) {
        var el = doc.createElement('div');
        el.className = 'tile' + (item.missing ? ' missing' : '');
        el.setAttribute('data-key', item.key);

        // Les icônes d'autres apps peuvent être inaccessibles (sandbox) : repli sur des initiales.
        if (item.iconPath) {
            var img = doc.createElement('img');
            img.alt = '';
            img.onerror = function () { el.replaceChild(fallbackIcon(item), img); };
            img.src = item.iconPath;
            el.appendChild(img);
        } else {
            el.appendChild(fallbackIcon(item));
        }

        var label = doc.createElement('div');
        label.className = 'label';
        label.textContent = item.name;
        el.appendChild(label);

        if (item.pinned && !item.missing) {
            var badge = doc.createElement('span');
            badge.className = 'badge';
            badge.textContent = '★';
            el.appendChild(badge);
        }
        return el;
    }

    function renderRows(container, rows) {
        var frag = doc.createDocumentFragment();
        rows.forEach(function (row, rIdx) {
            var section = doc.createElement('section');
            section.className = 'row';
            section.setAttribute('data-row', rIdx);

            var title = doc.createElement('h2');
            title.className = 'row-title';
            title.textContent = row.title;
            section.appendChild(title);

            if (!row.items.length) {
                var empty = doc.createElement('div');
                empty.className = 'row-empty';
                empty.textContent = row.id === 'library' ? 'Chargement des applications…' : 'Aucun élément';
                section.appendChild(empty);
            } else {
                var track = doc.createElement('div');
                track.className = 'row-track';
                row.items.forEach(function (item) { track.appendChild(tileEl(item)); });
                section.appendChild(track);
            }
            frag.appendChild(section);
        });
        container.innerHTML = '';
        container.appendChild(frag);
    }

    function applyFocus(container, rows, state) {
        var prev = container.querySelector('.tile.focused');
        if (prev) { prev.classList.remove('focused'); }

        var sections = container.querySelectorAll('.row');
        for (var r = 0; r < sections.length; r++) {
            var track = sections[r].querySelector('.row-track');
            if (!track) { continue; }
            var col = state.cols[r] || 0;
            // Fait défiler la rangée pour garder la tuile focalisée visible.
            var offset = Math.max(0, col - (VISIBLE_TILES - 2));
            track.style.transform = 'translateX(' + (-offset * TILE_STRIDE) + 'px)';
            if (r === state.row) {
                var tile = track.children[col];
                if (tile) { tile.classList.add('focused'); }
            }
        }

        // Défilement vertical : garde la rangée active dans la zone visible.
        var active = sections[state.row];
        if (active) {
            var top = active.offsetTop - container.offsetTop;
            container.scrollTop = Math.max(0, top - 40);
        }
    }

    function setEditMode(on) {
        doc.getElementById('mode-label').hidden = !on;
    }

    function toast(msg) {
        var el = doc.getElementById('toast');
        el.textContent = msg;
        el.classList.add('visible');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { el.classList.remove('visible'); }, 2500);
    }

    function startClock(use24h) {
        var el = doc.getElementById('clock');
        function tick() {
            var d = new Date();
            var h = d.getHours();
            var m = ('0' + d.getMinutes()).slice(-2);
            el.textContent = use24h ? ('0' + h).slice(-2) + ':' + m : ((h % 12) || 12) + ':' + m + (h < 12 ? ' AM' : ' PM');
        }
        tick();
        setInterval(tick, 10000);
    }

    root.TH = root.TH || {};
    root.TH.ui = { renderRows: renderRows, applyFocus: applyFocus, setEditMode: setEditMode, toast: toast, startClock: startClock };
})(window);
