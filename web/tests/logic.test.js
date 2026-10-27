// Tests de la logique pure (aucune TV requise) : node --test tests/
'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');

const DEFAULTS = require('../src/js/defaults.js');
const store = require('../src/js/store.js');
const catalog = require('../src/js/catalog.js');
const focus = require('../src/js/focus.js');

const APPS = [
  { id: '111299001912', name: 'YouTube' },
  { id: '9999', name: 'Netflix (nouvel ID)' },
  { id: 'abc', name: 'arte' },
  { id: 'org.tizen.browser', name: 'Internet' },
  { id: 'com.samsung.tv.store', name: 'Apps' },
];

function memoryStorage() {
  const m = new Map();
  return { getItem: k => (m.has(k) ? m.get(k) : null), setItem: (k, v) => m.set(k, String(v)), removeItem: k => m.delete(k) };
}

test('mergeConfig ignore les champs inconnus et mal typés', () => {
  const merged = store.mergeConfig(DEFAULTS, { hidden: ['x'], showLibrary: 'oui', evil: 1 });
  assert.deepEqual(merged.hidden, ['x']);
  assert.equal(merged.showLibrary, true);
  assert.equal(merged.evil, undefined);
});

test('store survit à un JSON corrompu et à un stockage qui lève', () => {
  const s = memoryStorage();
  s.setItem('qh.prefs.v1', '{pas du json');
  assert.deepEqual(store.createStore(s, DEFAULTS).loadPrefs(), DEFAULTS);
  const broken = { getItem() { throw new Error('quota'); }, setItem() { throw new Error('quota'); }, removeItem() {} };
  const st = store.createStore(broken, DEFAULTS);
  assert.deepEqual(st.loadPrefs(), DEFAULTS);
  assert.equal(st.savePrefs(DEFAULTS), false);
});

test('les favoris se résolvent par ID puis par nom', () => {
  const rows = catalog.buildRows(APPS, DEFAULTS);
  const fav = rows[0].items;
  assert.equal(fav[0].appId, '111299001912');
  assert.equal(fav[1].appId, '9999'); // Netflix trouvé par le nom
  assert.ok(fav[2].missing); // Prime Video non installé
});

test('la bibliothèque exclut favoris et apps système, triée par nom', () => {
  const lib = catalog.buildRows(APPS, DEFAULTS).find(r => r.id === 'library').items;
  assert.deepEqual(lib.map(i => i.appId), ['abc']);
});

test('togglePin retire un favori résolu par son nom sans créer de doublon', () => {
  const fav = catalog.buildRows(APPS, DEFAULTS)[0].items[1];
  const next = catalog.togglePin(DEFAULTS, fav);
  assert.equal(next.pinned.length, DEFAULTS.pinned.length - 1);
  assert.ok(!next.pinned.some(p => p.name === 'Netflix'));
  assert.equal(DEFAULTS.pinned.length, 5, 'les défauts ne sont pas mutés');
});

test('togglePin puis hideApp sur une app de la bibliothèque', () => {
  const arte = catalog.buildRows(APPS, DEFAULTS).find(r => r.id === 'library').items[0];
  const pinned = catalog.togglePin(DEFAULTS, arte);
  assert.equal(pinned.pinned.at(-1).appIds[0], 'abc');
  const hidden = catalog.hideApp(pinned, { appId: 'abc' });
  assert.ok(hidden.hidden.includes('abc'));
  assert.ok(!hidden.pinned.some(p => p.appIds.includes('abc')));
});

test('movePin échange avec le voisin et reste borné', () => {
  const first = catalog.buildRows(APPS, DEFAULTS)[0].items[0];
  assert.deepEqual(catalog.movePin(DEFAULTS, first, -1).pinned, DEFAULTS.pinned);
  assert.equal(catalog.movePin(DEFAULTS, first, 1).pinned[1].name, 'YouTube');
});

test('focus : mémorise la colonne par rangée et saute les rangées vides', () => {
  const rows = [
    { items: [1, 2, 3] },
    { items: [] },
    { items: ['a', 'b'] },
  ];
  let s = focus.initial(rows);
  s = focus.move(s, rows, 'right');
  s = focus.move(s, rows, 'right');
  s = focus.move(s, rows, 'right'); // borné
  assert.equal(focus.current(s, rows), 3);
  s = focus.move(s, rows, 'down');
  assert.equal(s.row, 2);
  assert.equal(focus.current(s, rows), 'a');
  s = focus.move(s, rows, 'up');
  assert.equal(focus.current(s, rows), 3);
});

test('focus.normalize recadre après suppression d\'éléments', () => {
  const s = focus.normalize({ row: 0, cols: { 0: 9 } }, [{ items: [1, 2] }]);
  assert.equal(s.cols[0], 1);
});
