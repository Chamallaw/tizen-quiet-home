/*
 * Persistance locale : préférences utilisateur + cache de la liste d'apps.
 * Tout accès à localStorage est protégé : une erreur ne doit jamais empêcher l'accueil de s'afficher.
 */
(function (root) {
    'use strict';

    var PREFS_KEY = 'qh.prefs.v1';
    var APPS_CACHE_KEY = 'qh.apps.v1';

    function clone(obj) { return JSON.parse(JSON.stringify(obj)); }

    // Fusionne les préférences sauvegardées sur les défauts ; ignore les champs inconnus ou mal typés.
    function mergeConfig(defaults, saved) {
        var out = clone(defaults);
        if (!saved || typeof saved !== 'object') { return out; }
        Object.keys(out).forEach(function (key) {
            if (!(key in saved)) { return; }
            var def = out[key];
            var val = saved[key];
            if (Array.isArray(def) && Array.isArray(val)) { out[key] = clone(val); }
            else if (typeof def === typeof val && !Array.isArray(val)) { out[key] = val; }
        });
        return out;
    }

    function readJson(storage, key) {
        try {
            var raw = storage && storage.getItem(key);
            return raw ? JSON.parse(raw) : null;
        } catch (e) {
            return null;
        }
    }

    function writeJson(storage, key, value) {
        try {
            if (storage) { storage.setItem(key, JSON.stringify(value)); }
            return true;
        } catch (e) {
            return false;
        }
    }

    function createStore(storage, defaults) {
        return {
            loadPrefs: function () { return mergeConfig(defaults, readJson(storage, PREFS_KEY)); },
            savePrefs: function (prefs) { return writeJson(storage, PREFS_KEY, prefs); },
            resetPrefs: function () {
                try { storage.removeItem(PREFS_KEY); } catch (e) { /* ignoré */ }
                return clone(defaults);
            },
            loadAppsCache: function () {
                var cached = readJson(storage, APPS_CACHE_KEY);
                return cached && Array.isArray(cached.apps) ? cached.apps : null;
            },
            saveAppsCache: function (apps) {
                return writeJson(storage, APPS_CACHE_KEY, { savedAt: Date.now(), apps: apps });
            }
        };
    }

    var api = { mergeConfig: mergeConfig, createStore: createStore };
    root.TH = root.TH || {};
    root.TH.store = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof window !== 'undefined' ? window : globalThis);
