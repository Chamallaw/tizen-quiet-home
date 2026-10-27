/*
 * Configuration par défaut. L'utilisateur la surcharge depuis l'UI (épingler/masquer),
 * les modifications sont stockées dans localStorage (voir store.js).
 *
 * Les IDs d'apps Samsung varient selon l'année/la région : chaque raccourci liste
 * plusieurs IDs candidats et un motif de nom en secours. Le premier installé gagne.
 */
(function (root) {
    'use strict';

    var DEFAULTS = {
        version: 1,
        pinned: [
            { name: 'YouTube', appIds: ['111299001912'], match: 'youtube' },
            { name: 'Netflix', appIds: ['3201907018807', '11101200001'], match: 'netflix' },
            { name: 'Prime Video', appIds: ['3201910019365', '3201512006785'], match: 'prime video' },
            { name: 'Disney+', appIds: ['3201901017640'], match: 'disney' },
            { name: 'Plex', appIds: ['3201512006963'], match: 'plex' }
        ],
        // IDs à ne jamais afficher dans la bibliothèque (services système, pubs...).
        hidden: [],
        // Préfixes d'IDs internes Samsung masqués par défaut dans la bibliothèque.
        hiddenPrefixes: ['org.tizen.', 'com.samsung.tv.', 'com.samsung.', 'QuietHome1.'],
        showLibrary: true,
        clock24h: true
    };

    root.TH = root.TH || {};
    root.TH.DEFAULTS = DEFAULTS;
    if (typeof module !== 'undefined' && module.exports) { module.exports = DEFAULTS; }
})(typeof window !== 'undefined' ? window : globalThis);
