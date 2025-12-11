const { contextBridge, clipboard } = require('electron');
const { create, all } = require('mathjs');
const Store = require('electron-store');

const store = new Store({ name: 'instpro-calculator' });
const math = create(all, {
  number: 'number',
  precision: 14
});

contextBridge.exposeInMainWorld('instProAPI', {
  saveHistory: (history) => store.set('history', history),
  loadHistory: () => store.get('history', []),
  saveExpressions: (expressions) => store.set('expressions', expressions),
  loadExpressions: () => store.get('expressions', []),
  readClipboard: () => clipboard.readText(),
  evaluate: (expression) => math.evaluate(expression)
});
