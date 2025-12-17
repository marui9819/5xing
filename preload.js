const { contextBridge, clipboard } = require('electron');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { create, all } = require('mathjs');
const Store = require('electron-store');

const store = new Store({ name: 'instpro-calculator' });
const math = create(all, {
  number: 'number',
  precision: 14
});

const ensureDir = async (dirPath) => {
  await fs.promises.mkdir(dirPath, { recursive: true });
};

const findOneDrive = () => {
  const home = os.homedir();
  const direct = path.join(home, 'OneDrive');
  if (fs.existsSync(direct)) return direct;

  try {
    const candidates = fs
      .readdirSync(home, { withFileTypes: true })
      .filter((entry) => entry.isDirectory() && entry.name.startsWith('OneDrive'))
      .map((entry) => path.join(home, entry.name));

    return candidates.find((dir) => fs.existsSync(dir)) || null;
  } catch (error) {
    return null;
  }
};

const findDropbox = () => {
  const home = os.homedir();
  const direct = path.join(home, 'Dropbox');
  if (fs.existsSync(direct)) return direct;
  return null;
};

const resolveProviderRoot = (provider, customPath) => {
  const custom = customPath ? path.resolve(customPath) : '';
  if (custom) return custom;

  const home = os.homedir();
  if (provider === 'icloud') {
    const icloudDocs = path.join(home, 'Library', 'Mobile Documents', 'com~apple~CloudDocs');
    if (!fs.existsSync(icloudDocs)) return null;
    return path.join(icloudDocs, '5XingCalculator');
  }

  if (provider === 'onedrive') {
    const dir = findOneDrive();
    return dir ? path.join(dir, '5XingCalculator') : null;
  }

  if (provider === 'dropbox') {
    const dir = findDropbox();
    return dir ? path.join(dir, '5XingCalculator') : null;
  }

  return path.join(home, '.5xing-calculator', 'backups');
};

const checkCloudAvailability = async ({ provider, customPath }) => {
  const root = resolveProviderRoot(provider || 'local', customPath);
  if (!root) return { available: false, message: '未检测到可用的云盘目录，请先安装并登录对应云服务。' };

  try {
    await ensureDir(root);
    await fs.promises.access(root, fs.constants.W_OK);
    return { available: true, path: root };
  } catch (error) {
    return { available: false, message: error.message };
  }
};

const syncToCloud = async ({ provider, customPath, data, targetPath }) => {
  const root = targetPath || resolveProviderRoot(provider || 'local', customPath);
  if (!root) return { success: false, message: '未检测到可用的云盘目录，请先安装并登录对应云服务。' };

  try {
    await ensureDir(root);
    const filePath = path.join(root, '5xing-calculator-sync.json');
    await fs.promises.writeFile(filePath, JSON.stringify(data, null, 2), 'utf8');
    return { success: true, path: filePath, timestamp: new Date().toISOString() };
  } catch (error) {
    return { success: false, message: error.message };
  }
};

contextBridge.exposeInMainWorld('instProAPI', {
  saveHistory: (history) => store.set('history', history),
  loadHistory: () => store.get('history', []),
  saveExpressions: (expressions) => store.set('expressions', expressions),
  loadExpressions: () => store.get('expressions', []),
  saveSettings: (settings) => store.set('cloudSettings', settings),
  loadSettings: () => store.get('cloudSettings', null),
  readClipboard: () => clipboard.readText(),
  evaluate: (expression) => math.evaluate(expression),
  syncToCloud,
  checkCloudAvailability
});
