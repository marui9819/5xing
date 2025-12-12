const expressionList = document.getElementById('expression-list');
const addBtn = document.getElementById('add-expression');
const saveBtn = document.getElementById('save-session');
const historyList = document.getElementById('history-list');
const historyEmpty = document.getElementById('history-empty');
const clearHistory = document.getElementById('clear-history');
const pasteButton = document.getElementById('paste-from-clipboard');

let expressions = [];
let history = [];

const sanitizeExpression = (text) => {
  if (!text) return '';
  return text
    .replace(/×/g, '*')
    .replace(/÷/g, '/')
    .replace(/，/g, ',')
    .replace(/：/g, ':')
    .replace(/[；;]/g, ';')
    .replace(/[−–—]/g, '-')
    .replace(/＝/g, '=')
    .replace(/[\s\u00a0]+/g, ' ')
    .trim();
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
  // Basic, Math-only evaluation fallback when preload bridge is unavailable
  const safeExpression = clean.replace(/[^-+*/().,\d\s]/g, '');
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
  const toSave = expressions.map(({ value }) => value);
  window.instProAPI?.saveExpressions(toSave);
};

const persistHistory = () => {
  window.instProAPI?.saveHistory(history);
};

const createExpressionRow = (entry) => {
  const row = document.createElement('div');
  row.className = 'expression-row';

  const input = document.createElement('div');
  input.className = 'expression-input';
  input.contentEditable = 'true';
  input.setAttribute('data-placeholder', '请输入算式，例如 5 × 90 + 1200');
  input.textContent = entry.value || '';

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
    const currentText = input.textContent.trim();
    const evaluation = evaluateExpression(currentText);

    if (!currentText) {
      resultValue.textContent = '等待输入';
      resultValue.classList.add('placeholder');
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
  };

  input.addEventListener('input', update);
  input.addEventListener('paste', (e) => {
    e.preventDefault();
    const text = e.clipboardData.getData('text/plain');
    document.execCommand('insertText', false, text);
  });

  row.appendChild(input);
  row.appendChild(resultBox);

  entry.node = row;
  setTimeout(update, 20);

  return row;
};

const renderExpressions = () => {
  expressionList.innerHTML = '';
  expressions = expressions.filter(Boolean);
  if (!expressions.length) {
    expressions.push({ node: null, value: '', result: null });
  }

  expressions.forEach((entry) => {
    const node = createExpressionRow(entry);
    expressionList.appendChild(node);
  });
};

const handleAddExpression = () => {
  const entry = { node: null, value: '', result: null };
  expressions.push(entry);
  const node = createExpressionRow(entry);
  expressionList.appendChild(node);
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
    content.textContent = item.items.map((i) => i.expression).join('\n');

    const summary = document.createElement('div');
    summary.className = 'history-result';
    summary.textContent = `最新结果：${item.items[item.items.length - 1]?.result ?? ''}`;

    const actions = document.createElement('div');
    actions.className = 'history-actions';

    const restoreBtn = document.createElement('button');
    restoreBtn.className = 'ghost';
    restoreBtn.textContent = '恢复到编辑';
    restoreBtn.addEventListener('click', () => {
      expressions = item.items.map((entry) => ({ node: null, value: entry.expression, result: null }));
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
    .map((expr) => expr.node?.querySelector('.expression-input')?.textContent.trim() || '')
    .filter((text) => text.length);

  if (!cleaned.length) return;

  const evaluatedItems = cleaned.map((text) => {
    const evaluation = evaluateExpression(text);
    return {
      expression: evaluation.raw || text,
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

  expressions = storedExpressions.map((value) => ({ node: null, value, result: null }));
  if (!expressions.length) expressions = [{ node: null, value: '', result: null }];
  history = storedHistory;

  renderExpressions();
  renderHistory();
};

addBtn.addEventListener('click', handleAddExpression);
saveBtn.addEventListener('click', saveSessionToHistory);
clearHistory.addEventListener('click', clearAllHistory);

  pasteButton.addEventListener('click', async () => {
    const text = await window.instProAPI?.readClipboard?.();
    if (!text) return;

    const active = document.activeElement;
    if (active && active.classList.contains('expression-input')) {
      document.execCommand('insertText', false, text);
    } else {
      const entry = { node: null, value: sanitizeExpression(text), result: null };
      expressions.push(entry);
      const node = createExpressionRow(entry);
      expressionList.appendChild(node);
      persistExpressions();
    }
  });

restoreState();
