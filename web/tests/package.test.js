// Vérifications statiques du paquet avant installation sur la TV.
'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const SRC = path.join(__dirname, '..', 'src');
const configXml = fs.readFileSync(path.join(SRC, 'config.xml'), 'utf8');

test('config.xml : package id de 10 caractères alphanumériques', () => {
  const pkg = configXml.match(/package="([^"]+)"/)[1];
  assert.match(pkg, /^[0-9A-Za-z]{10}$/);
  const appId = configXml.match(/<tizen:application id="([^"]+)"/)[1];
  assert.ok(appId.startsWith(pkg + '.'), 'l\'app id doit commencer par le package id');
});

test('config.xml : uniquement des privilèges publics connus', () => {
  const privs = [...configXml.matchAll(/privilege name="([^"]+)"/g)].map(m => m[1]);
  assert.deepEqual(privs.sort(), [
    'http://tizen.org/privilege/application.launch',
    'http://tizen.org/privilege/tv.inputdevice',
  ]);
});

test('config.xml : aucun accès réseau déclaré', () => {
  assert.doesNotMatch(configXml.replace(/<!--[\s\S]*?-->/g, ''), /<access\b/);
});

test('fichiers référencés présents', () => {
  const html = fs.readFileSync(path.join(SRC, 'index.html'), 'utf8');
  const refs = [...html.matchAll(/(?:src|href)="([^"]+)"/g)].map(m => m[1]);
  refs.push('icon.png');
  for (const ref of refs) {
    assert.ok(fs.existsSync(path.join(SRC, ref)), 'manquant : ' + ref);
  }
});

test('JS compatible vieux moteurs : pas de syntaxe ES2015+ dans src/', () => {
  const dir = path.join(SRC, 'js');
  for (const f of fs.readdirSync(dir)) {
    const code = fs.readFileSync(path.join(dir, f), 'utf8').replace(/\/\*[\s\S]*?\*\/|\/\/.*$/gm, '');
    assert.doesNotMatch(code, /=>|\blet\s|\bconst\s|`|\bclass\s/, f);
  }
});
