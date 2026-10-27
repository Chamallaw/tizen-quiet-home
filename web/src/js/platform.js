/*
 * Seul module qui parle aux API Tizen. Hors TV (navigateur du PC), une
 * implémentation factice permet de développer et tester l'UI.
 */
(function (root) {
    'use strict';

    var KEYS = {
        LEFT: 37, UP: 38, RIGHT: 39, DOWN: 40, ENTER: 13,
        BACK: 10009, EXIT: 10182,
        RED: 403, GREEN: 404, YELLOW: 405, BLUE: 406,
        // Équivalents clavier pour le PC.
        BACKSPACE: 8, ESC: 27, F1: 112, F2: 113, F3: 114, F4: 115
    };

    var EXTRA_TV_KEYS = ['ColorF0Red', 'ColorF1Green', 'ColorF2Yellow', 'ColorF3Blue'];

    function log(level, msg, extra) {
        // console.* est visible dans le Web Inspector (sdb forward + Chrome DevTools).
        var line = '[QuietHome] ' + msg;
        if (extra !== undefined) { console[level](line, extra); } else { console[level](line); }
    }

    function tizenPlatform(tizen) {
        return {
            isTv: true,
            registerKeys: function () {
                if (!tizen.tvinputdevice) { return; }
                EXTRA_TV_KEYS.forEach(function (k) {
                    try { tizen.tvinputdevice.registerKey(k); } catch (e) { log('warn', 'registerKey ' + k + ' échoué', e); }
                });
            },
            listApps: function (cb) {
                try {
                    tizen.application.getAppsInfo(function (infos) {
                        cb(null, infos
                            .filter(function (i) { return i.show !== false; })
                            .map(function (i) { return { id: i.id, name: i.name, iconPath: i.iconPath }; }));
                    }, function (err) { cb(err); });
                } catch (e) { cb(e); }
            },
            launch: function (appId, cb) {
                try {
                    tizen.application.launch(appId, function () { cb(null); }, function (err) { cb(err); });
                } catch (e) { cb(e); }
            },
            hide: function () {
                try { tizen.application.getCurrentApplication().hide(); } catch (e) { log('warn', 'hide() échoué', e); }
            },
            exit: function () {
                try { tizen.application.getCurrentApplication().exit(); } catch (e) { log('warn', 'exit() échoué', e); }
            }
        };
    }

    function mockPlatform() {
        var apps = [
            { id: '111299001912', name: 'YouTube' },
            { id: '3201907018807', name: 'Netflix' },
            { id: '3201910019365', name: 'Prime Video' },
            { id: '3201606009684', name: 'Spotify' },
            { id: '3202203026841', name: 'Arte' },
            { id: '3201810017091', name: 'France.tv' },
            { id: 'org.tizen.browser', name: 'Internet' },
            { id: 'com.samsung.tv.store', name: 'Apps' }
        ];
        return {
            isTv: false,
            registerKeys: function () {},
            listApps: function (cb) { setTimeout(function () { cb(null, apps.slice()); }, 150); },
            launch: function (appId, cb) { log('info', '[mock] launch ' + appId); cb(null); },
            hide: function () { log('info', '[mock] hide'); },
            exit: function () { log('info', '[mock] exit'); }
        };
    }

    var platform = (typeof root.tizen !== 'undefined' && root.tizen.application) ? tizenPlatform(root.tizen) : mockPlatform();
    platform.KEYS = KEYS;
    platform.log = log;

    root.TH = root.TH || {};
    root.TH.platform = platform;
})(typeof window !== 'undefined' ? window : globalThis);
