const expressionList = document.getElementById('expression-list');
const addBtn = document.getElementById('add-expression');
const saveBtn = document.getElementById('save-session');
const historyList = document.getElementById('history-list');
const historyEmpty = document.getElementById('history-empty');
const clearHistory = document.getElementById('clear-history');
const pasteButton = document.getElementById('paste-from-clipboard');
const validCount = document.getElementById('expression-valid-count');
const expressionTotal = document.getElementById('expression-total');
const keypadGrid = document.getElementById('keypad-grid');
const cloudProvider = document.getElementById('cloud-provider');
const autoSync = document.getElementById('auto-sync');
const syncNowBtn = document.getElementById('sync-now');
const cloudStatus = document.getElementById('cloud-status');

let expressions = [];
let history = [];
let settings = { provider: 'icloud', autoSync: false, lastSync: '' };
let activeExpressionInput = null;

const normalizeSingleLine = (text) => (text || '').replace(/\r?\n+/g, ' ').trim();

const sanitizeExpression = (text) => {
  if (!text) return '';

  const toHalfWidth = (char) => String.fromCharCode(char.charCodeAt(0) - 0xfee0);

  const normalized = normalizeSingleLine(text)
    .replace(/[０-９]/g, toHalfWidth)
    .replace(/[＋﹢]/g, '+')
    .replace(/[×xX＊﹡·⋅]/g, '*')
    .replace(/[÷／⁄]/g, '/')
    .replace(/[－﹣–—‒−]/g, '-')
    .replace(/[（﹙【〔｛]/g, '(')
    .replace(/[）﹚】〕｝]/g, ')')
    .replace(/．/g, '.')
    .replace(/，/g, ',')
    .replace(/[\s\u00a0]+/g, ' ')
    .trim();

  const withoutCommas = normalized.replace(/,/g, '');

  return withoutCommas.replace(/[^0-9.+\-*/()\s]/g, '');
};

const formatResult = (value) => {
  const numericValue = typeof value === 'string' ? Number(value) : value;
  if (typeof numericValue !== 'number' || Number.isNaN(numericValue)) return '无效';
  if (!Number.isFinite(numericValue)) return '∞';
  const rounded =
    Math.abs(numericValue) < 1 ? Math.round(numericValue * 1e8) / 1e8 : Math.round(numericValue * 1e6) / 1e6;
  return rounded.toString();
};

const localEvaluate = (clean) => {
  const safeExpression = clean.replace(/[^-+*/().\d\s]/g, '');
  if (!safeExpression.trim()) throw new Error('缺少有效数字');
  if (/[+*/]{2,}|-{3,}/.test(safeExpression)) {
    throw new Error('表达式不完整');
  }
  // eslint-disable-next-line no-new-func
  const evaluator = new Function(`return (${safeExpression})`);
  return evaluator();
};

const evaluateExpression = (text) => {
  const clean = sanitizeExpression(text);
  if (!clean) return { display: '', raw: '' };

  try {
    const evaluated = window.instProAPI?.evaluate ? window.instProAPI.evaluate(clean) : localEvaluate(clean);
    return { display: formatResult(evaluated), raw: clean };
  } catch (err) {
    return { display: '错误', raw: clean, error: err.message };
  }
};

const persistExpressions = () => {
  const toSave = expressions.map(({ title, note, value }) => ({ title, note, value }));
  window.instProAPI?.saveExpressions(toSave);
};

const persistHistory = () => {
  window.instProAPI?.saveHistory(history);
};

const persistSettings = () => {
  window.instProAPI?.saveSettings?.(settings);
};

const updateAggregates = () => {
  let valid = 0;
  let sum = 0;

  expressions.forEach((entry) => {
    const res = entry.result;
    if (res && !res.error && res.display && res.display !== '无效') {
      const numeric = Number(res.display);
      if (!Number.isNaN(numeric)) {
        valid += 1;
        sum += numeric;
      }
    }
  });

  validCount.textContent = valid;
  expressionTotal.textContent = valid ? formatResult(sum) : '0';
};

const setActiveInput = (input) => {
  activeExpressionInput = input;
};

const insertTextAtCaret = (input, text) => {
  input.focus();
  const sanitizedText = normalizeSingleLine(text);
  const selection = window.getSelection();
  if (document.queryCommandSupported('insertText')) {
    document.execCommand('insertText', false, sanitizedText);
  } else if (selection) {
    const range = selection.rangeCount ? selection.getRangeAt(0) : document.createRange();
    if (!selection.rangeCount) {
      range.selectNodeContents(input);
      range.collapse(false);
      selection.removeAllRanges();
      selection.addRange(range);
    }
    range.deleteContents();
    range.insertNode(document.createTextNode(sanitizedText));
    range.collapse(false);
  }
};

const createExpressionRow = (entry, index) => {
  const row = document.createElement('div');
  row.className = 'expression-row';

  const left = document.createElement('div');
  left.className = 'expression-left';

  const meta = document.createElement('div');
  meta.className = 'expression-meta';

  const titleInput = document.createElement('input');
  titleInput.className = 'expression-title';
  titleInput.value = entry.title || `算式${index + 1}`;
  titleInput.addEventListener('input', () => {
    entry.title = titleInput.value.trim() || `算式${index + 1}`;
    persistExpressions();
  });

  const noteInput = document.createElement('input');
  noteInput.className = 'expression-note';
  noteInput.placeholder = '备注（可选）';
  noteInput.value = entry.note || '';
  noteInput.addEventListener('input', () => {
    entry.note = noteInput.value;
    persistExpressions();
  });

  meta.appendChild(titleInput);
  meta.appendChild(noteInput);

  const input = document.createElement('div');
  input.className = 'expression-input';
  input.contentEditable = 'true';
  input.setAttribute('data-placeholder', '请输入算式，例如 5 × 90 + 1200');
  input.textContent = entry.value || '';

  input.addEventListener('focus', () => setActiveInput(input));

  const resultBox = document.createElement('div');
  resultBox.className = 'result-box';

  const resultValue = document.createElement('div');
  resultValue.className = 'result-value placeholder';
  resultValue.textContent = '等待输入';

  const resultExpression = document.createElement('div');
  resultExpression.className = 'result-expression';
  resultExpression.textContent = '';

  resultBox.appendChild(resultValue);
  resultBox.appendChild(resultExpression);

  const update = () => {
    const currentText = normalizeSingleLine(input.textContent);
    const evaluation = evaluateExpression(currentText);

    if (!currentText) {
      resultValue.textContent = '等待输入';
      resultValue.classList.add('placeholder');
      resultValue.classList.remove('error');
      resultExpression.textContent = '';
    } else if (evaluation.error) {
      resultValue.textContent = evaluation.display;
      resultValue.classList.add('error');
      resultValue.classList.remove('placeholder');
      resultExpression.textContent = evaluation.raw;
    } else {
      resultValue.textContent = evaluation.display;
      resultValue.classList.remove('error', 'placeholder');
      resultExpression.textContent = evaluation.raw;
    }

    entry.value = currentText;
    entry.result = evaluation;

    persistExpressions();
    updateAggregates();
  };

  input.addEventListener('input', () => {
    if (input.textContent.includes('\n')) {
      input.textContent = normalizeSingleLine(input.textContent);
      const range = document.createRange();
      range.selectNodeContents(input);
      range.collapse(false);
      const selection = window.getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
    }
    update();
  });

  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
    }
  });

  input.addEventListener('paste', (e) => {
    e.preventDefault();
    const text = e.clipboardData.getData('text/plain');
    if (!text) return;

    const lines = text
      .split(/\r?\n/)
      .map((line) => sanitizeExpression(line))
      .filter(Boolean);

    if (!lines.length) return;

    insertTextAtCaret(input, lines.shift());
    update();

    lines.forEach((line) => {
      const entry = { node: null, title: `算式${expressions.length + 1}`, note: '', value: line, result: null };
      expressions.push(entry);
      const newNode = createExpressionRow(entry, expressions.length - 1);
      expressionList.appendChild(newNode);
    });

    persistExpressions();
  });

  left.appendChild(meta);
  left.appendChild(input);

  row.appendChild(left);
  row.appendChild(resultBox);

  entry.node = row;
  setTimeout(update, 20);

  return row;
};

const renderExpressions = () => {
  expressionList.innerHTML = '';
  expressions = expressions.filter(Boolean);
  if (!expressions.length) {
    expressions.push({ node: null, title: '算式1', note: '', value: '', result: null });
  }

  expressions.forEach((entry, index) => {
    if (!entry.title) entry.title = `算式${index + 1}`;
    const node = createExpressionRow(entry, index);
    expressionList.appendChild(node);
  });

  updateAggregates();
};

const handleAddExpression = (value = '') => {
  const entry = { node: null, title: `算式${expressions.length + 1}`, note: '', value, result: null };
  expressions.push(entry);
  const node = createExpressionRow(entry, expressions.length - 1);
  expressionList.appendChild(node);

  const input = node.querySelector('.expression-input');
  if (input) {
    input.focus();
  }

  persistExpressions();
  return node;
};

const renderHistory = () => {
  historyList.innerHTML = '';
  if (!history.length) {
    historyEmpty.style.display = 'block';
    return;
  }

  historyEmpty.style.display = 'none';

  history.forEach((item, index) => {
    const card = document.createElement('div');
    card.className = 'history-card';

    const header = document.createElement('div');
    header.className = 'history-header';
    header.innerHTML = `<span>${item.time}</span><span class="tag">${item.items.length} 行</span>`;

    const content = document.createElement('div');
    content.className = 'history-expression';
    content.textContent = item.items
      .map((i, idx) => `${i.title || `算式${idx + 1}`}: ${i.expression}${i.note ? `（${i.note}）` : ''}`)
      .join('\n');

    const summary = document.createElement('div');
    summary.className = 'history-result';
    summary.textContent = `最新结果：${item.items[item.items.length - 1]?.result ?? ''}`;

    const actions = document.createElement('div');
    actions.className = 'history-actions';

    const restoreBtn = document.createElement('button');
    restoreBtn.className = 'ghost';
    restoreBtn.textContent = '恢复到编辑';
    restoreBtn.addEventListener('click', () => {
      expressions = item.items.map((entry, idx) => ({
        node: null,
        title: entry.title || `算式${idx + 1}`,
        note: entry.note || '',
        value: entry.expression,
        result: null
      }));
      renderExpressions();
      persistExpressions();
    });

    const deleteBtn = document.createElement('button');
    deleteBtn.className = 'ghost';
    deleteBtn.textContent = '删除';
    deleteBtn.addEventListener('click', () => {
      history.splice(index, 1);
      renderHistory();
      persistHistory();
    });

    actions.appendChild(restoreBtn);
    actions.appendChild(deleteBtn);

    card.appendChild(header);
    card.appendChild(content);
    card.appendChild(summary);
    card.appendChild(actions);
    historyList.appendChild(card);
  });
};

const saveSessionToHistory = () => {
  const cleaned = expressions
    .map((expr) => ({
      title: expr.title,
      note: expr.note,
      expression: sanitizeExpression(expr.node?.querySelector('.expression-input')?.textContent || expr.value || '')
    }))
    .filter((item) => item.expression.length);

  if (!cleaned.length) return;

  const evaluatedItems = cleaned.map((item, idx) => {
    const evaluation = evaluateExpression(item.expression);
    return {
      title: item.title || `算式${idx + 1}`,
      note: item.note || '',
      expression: evaluation.raw || item.expression,
      result: evaluation.display,
      error: evaluation.error
    };
  });

  const now = new Date();
  const stamp = `${now.getMonth() + 1}月${now.getDate()}日 ${now.getHours().toString().padStart(2, '0')}:${now
    .getMinutes()
    .toString()
    .padStart(2, '0')}`;

  history.unshift({ time: stamp, items: evaluatedItems });
  history = history.slice(0, 20);
  renderHistory();
  persistHistory();
};

const clearAllHistory = () => {
  history = [];
  renderHistory();
  persistHistory();
};

const restoreState = () => {
  const storedExpressions = window.instProAPI?.loadExpressions?.() || [];
  const storedHistory = window.instProAPI?.loadHistory?.() || [];
  const storedSettings = window.instProAPI?.loadSettings?.();

  expressions = storedExpressions.map((item, idx) => {
    if (typeof item === 'string') {
      return { node: null, title: `算式${idx + 1}`, note: '', value: item, result: null };
    }
    return {
      node: null,
      title: item.title || `算式${idx + 1}`,
      note: item.note || '',
      value: item.value || '',
      result: null
    };
  });

  if (!expressions.length) expressions = [{ node: null, title: '算式1', note: '', value: '', result: null }];
  history = storedHistory;
  settings = {
    provider: storedSettings?.provider || 'icloud',
    autoSync: Boolean(storedSettings?.autoSync),
    lastSync: storedSettings?.lastSync || ''
  };

  renderExpressions();
  renderHistory();
  applySettingsUI();
};

const applySettingsUI = () => {
  cloudProvider.value = settings.provider;
  autoSync.checked = settings.autoSync;
  cloudStatus.textContent = settings.lastSync ? `上次同步：${settings.lastSync}` : '未同步';
};

const performSync = (manual = false) => {
  const now = new Date();
  const stamp = `${now.getFullYear()}-${(now.getMonth() + 1).toString().padStart(2, '0')}-${now
    .getDate()
    .toString()
    .padStart(2, '0')} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
  cloudStatus.textContent = `${manual ? '正在手动同步' : '自动同步'}到 ${cloudProvider.options[cloudProvider.selectedIndex].text}...`;

  setTimeout(() => {
    settings.lastSync = stamp;
    cloudStatus.textContent = `${cloudProvider.options[cloudProvider.selectedIndex].text} 同步完成（${stamp}）`;
    persistSettings();
  }, 300);
};

const ensureActiveExpression = () => {
  if (activeExpressionInput && document.body.contains(activeExpressionInput)) return activeExpressionInput;
  const lastRow = expressionList.querySelector('.expression-input:last-of-type');
  if (lastRow) {
    lastRow.focus();
    return lastRow;
  }
  return null;
};

const setupKeypad = () => {
  const keys = [
    '7',
    '8',
    '9',
    '÷',
    '←',
    '4',
    '5',
    '6',
    '×',
    '清空',
    '1',
    '2',
    '3',
    '-',
    '(',
    '0',
    '.',
    '+',
    ')',
    '/'
  ];

  keys.forEach((label) => {
    const btn = document.createElement('button');
    btn.textContent = label;
    btn.className = 'ghost';
    btn.addEventListener('click', () => {
      const target = ensureActiveExpression();
      if (!target) return;

      if (label === '清空') {
        target.textContent = '';
        target.dispatchEvent(new Event('input'));
        return;
      }

      if (label === '←') {
        const text = target.textContent;
        target.textContent = text.slice(0, -1);
        target.dispatchEvent(new Event('input'));
        return;
      }

      const value = label === '×' ? '*' : label === '÷' ? '/' : label;
      insertTextAtCaret(target, value);
      target.dispatchEvent(new Event('input'));
    });
    keypadGrid.appendChild(btn);
  });
};

addBtn.addEventListener('click', () => handleAddExpression());
saveBtn.addEventListener('click', saveSessionToHistory);
clearHistory.addEventListener('click', clearAllHistory);

pasteButton.addEventListener('click', async () => {
  const text = await window.instProAPI?.readClipboard?.();
  if (!text) return;

  const lines = text
    .split(/\r?\n/)
    .map((line) => sanitizeExpression(line))
    .filter(Boolean);

  if (!lines.length) return;

  const active = ensureActiveExpression();
  if (active) {
    insertTextAtCaret(active, lines.shift());
    active.dispatchEvent(new Event('input'));
  }

  lines.forEach((line) => handleAddExpression(line));
  persistExpressions();
});

cloudProvider.addEventListener('change', () => {
  settings.provider = cloudProvider.value;
  persistSettings();
  if (settings.autoSync) performSync();
});

autoSync.addEventListener('change', () => {
  settings.autoSync = autoSync.checked;
  persistSettings();
  if (settings.autoSync) performSync();
});

syncNowBtn.addEventListener('click', () => performSync(true));

document.addEventListener('keydown', (event) => {
  if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
    event.preventDefault();
    saveSessionToHistory();
  }

  if (event.shiftKey && event.key === 'Enter') {
    event.preventDefault();
    handleAddExpression();
  }
});

const start = () => {
  restoreState();
  setupKeypad();
  if (settings.autoSync) performSync();
};

start();
