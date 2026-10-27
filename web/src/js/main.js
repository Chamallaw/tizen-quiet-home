/*
 * Démarrage et gestion des touches.
 * Stratégie de chargement : affichage immédiat depuis le cache, puis
 * rafraîchissement asynchrone de la liste réelle des apps (stale-while-revalidate).
 */
(function (root) {
    'use strict';

    var TH = root.TH;
    var P = TH.platform;
    var K = P.KEYS;
    var store = TH.store.createStore(root.localStorage, TH.DEFAULTS);
    var container = root.document.getElementById('rows');

    var prefs = store.loadPrefs();
    var apps = store.loadAppsCache() || [];
    var rows = [];
    var focusState = { row: 0, cols: {} };
    var editMode = false;
    var launching = false;

    function rebuild() {
        rows = TH.catalog.buildRows(apps, prefs);
        focusState = TH.focus.normalize(focusState, rows);
        TH.ui.renderRows(container, rows);
        TH.ui.applyFocus(container, rows, focusState);
    }

    function updatePrefs(next, message) {
        prefs = next;
        if (!store.savePrefs(prefs)) { TH.ui.toast('Sauvegarde impossible'); }
        else if (message) { TH.ui.toast(message); }
        rebuild();
    }

    function refreshApps(manual) {
        P.listApps(function (err, list) {
            if (err) {
                P.log('error', 'getAppsInfo échoué', err);
                if (manual) { TH.ui.toast('Liste des apps indisponible'); }
                return;
            }
            apps = list;
            store.saveAppsCache(apps);
            rebuild();
            if (manual) { TH.ui.toast(list.length + ' applications'); }
        });
    }

    function activate(item) {
        if (!item) { return; }
        if (item.kind === 'action') {
            if (item.action === 'reload') { refreshApps(true); }
            else if (item.action === 'reset') { updatePrefs(store.resetPrefs(), 'Accueil réinitialisé'); }
            else if (item.action === 'hide') { P.hide(); }
            return;
        }
        if (item.missing || !item.appId) { TH.ui.toast(item.name + ' n’est pas installée'); return; }
        if (launching) { return; }
        launching = true;
        P.launch(item.appId, function (err) {
            launching = false;
            if (err) {
                P.log('error', 'launch ' + item.appId + ' échoué', err);
                TH.ui.toast('Impossible de lancer ' + item.name);
            }
        });
    }

    function moveFocus(dir) {
        var item = TH.focus.current(focusState, rows);
        // En mode édition, gauche/droite sur un favori le déplace au lieu de bouger le focus.
        if (editMode && item && item.pinned && !item.missing && (dir === 'left' || dir === 'right')) {
            var delta = dir === 'left' ? -1 : 1;
            focusState.cols[focusState.row] = Math.max(0, (focusState.cols[focusState.row] || 0) + delta);
            updatePrefs(TH.catalog.movePin(prefs, item, delta));
            return;
        }
        focusState = TH.focus.move(focusState, rows, dir);
        TH.ui.applyFocus(container, rows, focusState);
    }

    function onKey(e) {
        var item = TH.focus.current(focusState, rows);
        switch (e.keyCode) {
            case K.LEFT: moveFocus('left'); break;
            case K.RIGHT: moveFocus('right'); break;
            case K.UP: moveFocus('up'); break;
            case K.DOWN: moveFocus('down'); break;
            case K.ENTER: activate(item); break;
            case K.YELLOW: case K.F3:
                // Autorisé aussi sur un favori absent (grisé), pour pouvoir le retirer.
                if (item && item.kind === 'app') {
                    var wasPinned = !!item.pinned;
                    updatePrefs(TH.catalog.togglePin(prefs, item), wasPinned ? 'Désépinglée' : 'Épinglée');
                }
                break;
            case K.BLUE: case K.F4:
                if (item && item.kind === 'app' && !item.missing) {
                    updatePrefs(TH.catalog.hideApp(prefs, item), item.name + ' masquée');
                }
                break;
            case K.GREEN: case K.F2:
                editMode = !editMode;
                TH.ui.setEditMode(editMode);
                break;
            case K.RED: case K.F1: refreshApps(true); break;
            case K.BACK: case K.BACKSPACE: case K.ESC:
                if (editMode) { editMode = false; TH.ui.setEditMode(false); }
                else { P.hide(); }
                break;
            default: return;
        }
        e.preventDefault();
    }

    // Revenir sur l'accueil (depuis une app) : recharger la liste, des apps ont pu être installées.
    root.document.addEventListener('visibilitychange', function () {
        if (!root.document.hidden) { refreshApps(false); }
    });

    root.addEventListener('error', function (e) { P.log('error', 'Erreur JS non gérée : ' + e.message); });
    root.document.addEventListener('keydown', onKey);

    P.registerKeys();
    TH.ui.startClock(prefs.clock24h);
    focusState = TH.focus.initial(TH.catalog.buildRows(apps, prefs));
    rebuild();
    refreshApps(false);
    P.log('info', 'démarré (' + (P.isTv ? 'TV' : 'navigateur/mock') + ')');
})(window);
