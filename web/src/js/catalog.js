/*
 * Logique pure : transforme la liste brute des apps installées + préférences
 * en rangées affichables. Aucun accès DOM ni Tizen ici (testable sous Node).
 *
 * Format d'une app : { id: string, name: string, iconPath?: string }
 * Format d'une tuile : { key, kind: 'app'|'action', appId?, action?, name, iconPath?, pinned?, missing? }
 */
(function (root) {
    'use strict';

    function lower(s) { return String(s || '').toLowerCase(); }

    function isHiddenId(id, prefs) {
        if (prefs.hidden.indexOf(id) !== -1) { return true; }
        for (var i = 0; i < prefs.hiddenPrefixes.length; i++) {
            if (id.indexOf(prefs.hiddenPrefixes[i]) === 0) { return true; }
        }
        return false;
    }

    // Trouve l'app installée correspondant à un raccourci épinglé (IDs candidats d'abord, puis nom).
    function resolvePinned(pin, apps) {
        var byId = {};
        apps.forEach(function (a) { byId[a.id] = a; });
        var ids = pin.appIds || [];
        for (var i = 0; i < ids.length; i++) {
            if (byId[ids[i]]) { return byId[ids[i]]; }
        }
        if (pin.match) {
            var m = lower(pin.match);
            for (var j = 0; j < apps.length; j++) {
                if (lower(apps[j].name).indexOf(m) !== -1) { return apps[j]; }
            }
        }
        return null;
    }

    function buildRows(apps, prefs) {
        apps = Array.isArray(apps) ? apps : [];
        var pinnedIds = {};
        var favorites = [];

        prefs.pinned.forEach(function (pin, pinIndex) {
            var app = resolvePinned(pin, apps);
            if (app) {
                if (pinnedIds[app.id]) { return; }
                pinnedIds[app.id] = true;
                favorites.push({ key: 'fav:' + app.id, kind: 'app', appId: app.id, name: pin.name || app.name, iconPath: app.iconPath, pinned: true, pinIndex: pinIndex });
            } else {
                // Si les apps ne sont pas encore connues, on garde la tuile (grisée) pour une mise en page stable.
                favorites.push({ key: 'fav:' + pin.name, kind: 'app', appId: (pin.appIds || [])[0] || null, name: pin.name, pinned: true, missing: true, pinIndex: pinIndex });
            }
        });

        var rows = [{ id: 'favorites', title: 'Favoris', items: favorites }];

        if (prefs.showLibrary) {
            var library = apps
                .filter(function (a) { return a && a.id && !pinnedIds[a.id] && !isHiddenId(a.id, prefs); })
                .sort(function (a, b) { return lower(a.name) < lower(b.name) ? -1 : lower(a.name) > lower(b.name) ? 1 : 0; })
                .map(function (a) { return { key: 'lib:' + a.id, kind: 'app', appId: a.id, name: a.name, iconPath: a.iconPath }; });
            rows.push({ id: 'library', title: 'Applications', items: library });
        }

        rows.push({ id: 'system', title: 'Système', items: [
            { key: 'act:reload', kind: 'action', action: 'reload', name: 'Rafraîchir les apps' },
            { key: 'act:reset', kind: 'action', action: 'reset', name: 'Réinitialiser l’accueil' },
            { key: 'act:hide', kind: 'action', action: 'hide', name: 'Retour à la TV' }
        ] });

        return rows;
    }

    // Index du raccourci épinglé correspondant à une tuile (-1 si non épinglée).
    // pinIndex est prioritaire : un favori résolu par son nom n'a pas forcément son ID dans appIds.
    function findPin(prefs, app) {
        if (typeof app.pinIndex === 'number' && prefs.pinned[app.pinIndex]) { return app.pinIndex; }
        for (var i = 0; i < prefs.pinned.length; i++) {
            if ((prefs.pinned[i].appIds || []).indexOf(app.appId) !== -1) { return i; }
        }
        return -1;
    }

    // Épingle/désépingle une app. Retourne de nouvelles préférences (immutables).
    function togglePin(prefs, app) {
        var next = JSON.parse(JSON.stringify(prefs));
        var idx = findPin(next, app);
        if (idx === -1) {
            next.pinned.push({ name: app.name, appIds: [app.appId] });
        } else {
            next.pinned.splice(idx, 1);
        }
        return next;
    }

    function hideApp(prefs, app) {
        var next = JSON.parse(JSON.stringify(prefs));
        if (next.hidden.indexOf(app.appId) === -1) { next.hidden.push(app.appId); }
        var idx = findPin(next, app);
        if (idx !== -1) { next.pinned.splice(idx, 1); }
        return next;
    }

    // Déplace une app épinglée d'un cran (delta = -1 ou +1).
    function movePin(prefs, app, delta) {
        var next = JSON.parse(JSON.stringify(prefs));
        var idx = findPin(next, app);
        var target = idx + delta;
        if (idx === -1 || target < 0 || target >= next.pinned.length) { return next; }
        var tmp = next.pinned[idx];
        next.pinned[idx] = next.pinned[target];
        next.pinned[target] = tmp;
        return next;
    }

    var api = { buildRows: buildRows, resolvePinned: resolvePinned, togglePin: togglePin, hideApp: hideApp, movePin: movePin };
    root.TH = root.TH || {};
    root.TH.catalog = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof window !== 'undefined' ? window : globalThis);
