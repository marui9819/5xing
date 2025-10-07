const COLS = 10;
const ROWS = 20;
const BLOCK_SIZE = 32;
const CLEAR_DURATION = 260;

const difficulties = {
  easy: { dropInterval: 950, levelLines: 12, speedFactor: 0.92, softDrop: 80 },
  normal: { dropInterval: 750, levelLines: 10, speedFactor: 0.9, softDrop: 65 },
  hard: { dropInterval: 520, levelLines: 8, speedFactor: 0.85, softDrop: 55 },
  zen: { dropInterval: 900, levelLines: Infinity, speedFactor: 1, softDrop: 70 },
};

const PIECES = {
  I: [
    [
      [0, 0, 0, 0],
      [1, 1, 1, 1],
      [0, 0, 0, 0],
      [0, 0, 0, 0],
    ],
    [
      [0, 0, 1, 0],
      [0, 0, 1, 0],
      [0, 0, 1, 0],
      [0, 0, 1, 0],
    ],
  ],
  J: [
    [
      [2, 0, 0],
      [2, 2, 2],
      [0, 0, 0],
    ],
    [
      [0, 2, 2],
      [0, 2, 0],
      [0, 2, 0],
    ],
    [
      [0, 0, 0],
      [2, 2, 2],
      [0, 0, 2],
    ],
    [
      [0, 2, 0],
      [0, 2, 0],
      [2, 2, 0],
    ],
  ],
  L: [
    [
      [0, 0, 3],
      [3, 3, 3],
      [0, 0, 0],
    ],
    [
      [0, 3, 0],
      [0, 3, 0],
      [0, 3, 3],
    ],
    [
      [0, 0, 0],
      [3, 3, 3],
      [3, 0, 0],
    ],
    [
      [3, 3, 0],
      [0, 3, 0],
      [0, 3, 0],
    ],
  ],
  O: [
    [
      [4, 4],
      [4, 4],
    ],
  ],
  S: [
    [
      [0, 5, 5],
      [5, 5, 0],
      [0, 0, 0],
    ],
    [
      [0, 5, 0],
      [0, 5, 5],
      [0, 0, 5],
    ],
  ],
  T: [
    [
      [0, 6, 0],
      [6, 6, 6],
      [0, 0, 0],
    ],
    [
      [0, 6, 0],
      [0, 6, 6],
      [0, 6, 0],
    ],
    [
      [0, 0, 0],
      [6, 6, 6],
      [0, 6, 0],
    ],
    [
      [0, 6, 0],
      [6, 6, 0],
      [0, 6, 0],
    ],
  ],
  Z: [
    [
      [7, 7, 0],
      [0, 7, 7],
      [0, 0, 0],
    ],
    [
      [0, 0, 7],
      [0, 7, 7],
      [0, 7, 0],
    ],
  ],
};

const PIECE_STYLES = {
  1: { color: '#4BE7FF', glow: 'rgba(75, 231, 255, 0.45)' },
  2: { color: '#4E5BFF', glow: 'rgba(78, 91, 255, 0.45)' },
  3: { color: '#F5A623', glow: 'rgba(245, 166, 35, 0.45)' },
  4: { color: '#F72585', glow: 'rgba(247, 37, 133, 0.45)' },
  5: { color: '#6FFFB0', glow: 'rgba(111, 255, 176, 0.5)' },
  6: { color: '#B388FF', glow: 'rgba(179, 136, 255, 0.45)' },
  7: { color: '#FF6F91', glow: 'rgba(255, 111, 145, 0.45)' },
};

function createMatrix(width, height) {
  return Array.from({ length: height }, () => Array(width).fill(0));
}

function randomPieceKey() {
  const keys = Object.keys(PIECES);
  return keys[(keys.length * Math.random()) | 0];
}

function cloneMatrix(matrix) {
  return matrix.map((row) => row.slice());
}

function drawRoundedRect(ctx, x, y, width, height, radius) {
  const r = Math.min(radius, width / 2, height / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.lineTo(x + width - r, y);
  ctx.quadraticCurveTo(x + width, y, x + width, y + r);
  ctx.lineTo(x + width, y + height - r);
  ctx.quadraticCurveTo(x + width, y + height, x + width - r, y + height);
  ctx.lineTo(x + r, y + height);
  ctx.quadraticCurveTo(x, y + height, x, y + height - r);
  ctx.lineTo(x, y + r);
  ctx.quadraticCurveTo(x, y, x + r, y);
  ctx.closePath();
}

class Piece {
  constructor(key) {
    this.key = key;
    this.rotations = PIECES[key];
    this.rotationIndex = 0;
    this.matrix = cloneMatrix(this.rotations[this.rotationIndex]);
    this.pos = { x: 0, y: 0 };
  }

  rotate(dir) {
    const len = this.rotations.length;
    this.rotationIndex = (this.rotationIndex + dir + len) % len;
    this.matrix = cloneMatrix(this.rotations[this.rotationIndex]);
  }
}

class TetrisGame {
  constructor() {
    this.canvas = document.getElementById('board');
    this.ctx = this.canvas.getContext('2d');

    this.nextCanvas = document.getElementById('next');
    this.nextCtx = this.nextCanvas.getContext('2d');

    this.overlay = document.getElementById('overlay');
    this.startBtn = document.getElementById('startBtn');
    this.resumeBtn = document.getElementById('resumeBtn');
    this.pauseBtn = document.getElementById('pauseBtn');
    this.difficultySelect = document.getElementById('difficulty');

    this.scoreEl = document.getElementById('score');
    this.levelEl = document.getElementById('level');
    this.linesEl = document.getElementById('lines');

    this.bindEvents();
    this.reset();
  }

  bindEvents() {
    this.startBtn.addEventListener('click', () => this.start());
    this.resumeBtn.addEventListener('click', () => this.resume());
    this.pauseBtn.addEventListener('click', () => this.togglePause());
    this.difficultySelect.addEventListener('change', () => {
      this.setDifficulty();
    });

    document.addEventListener('keydown', (event) => {
      if (this.paused) {
        if (event.code === 'Space') {
          this.resume();
        }
        return;
      }

      switch (event.code) {
        case 'ArrowLeft':
          this.move(-1);
          break;
        case 'ArrowRight':
          this.move(1);
          break;
        case 'ArrowDown':
          this.softDrop();
          break;
        case 'ArrowUp':
        case 'KeyX':
          this.rotate(1);
          break;
        case 'KeyZ':
          this.rotate(-1);
          break;
        case 'Space':
          this.hardDrop();
          break;
      }
    });
  }

  reset() {
    this.board = createMatrix(COLS, ROWS);
    this.activePiece = null;
    this.nextPiece = new Piece(randomPieceKey());
    this.dropCounter = 0;
    this.settings = difficulties.normal;
    this.baseDropInterval = this.settings.dropInterval;
    this.dropInterval = this.baseDropInterval;
    this.lastTime = 0;
    this.score = 0;
    this.lines = 0;
    this.level = 1;
    this.paused = true;
    this.clearAnimation = null;
    this.updateScoreboard();
    this.draw();
    this.drawNext();
    this.showOverlay(true, true);
    this.startBtn.textContent = '开始游戏';
  }

  setDifficulty() {
    const choice = this.difficultySelect.value;
    this.settings = difficulties[choice] || difficulties.normal;
    this.baseDropInterval = this.settings.dropInterval;
    this.recalculateSpeed();
  }

  start() {
    this.reset();
    this.setDifficulty();
    this.paused = false;
    this.spawnPiece();
    this.showOverlay(false);
    this.lastTime = 0;
    cancelAnimationFrame(this.animationId);
    this.animationId = requestAnimationFrame((time) => this.update(time));
  }

  resume() {
    if (!this.activePiece) {
      this.start();
      return;
    }
    this.paused = false;
    this.showOverlay(false);
    this.lastTime = 0;
    cancelAnimationFrame(this.animationId);
    this.animationId = requestAnimationFrame((time) => this.update(time));
  }

  togglePause() {
    if (!this.activePiece) return;
    this.paused = !this.paused;
    if (this.paused) {
      this.showOverlay(true, false);
      cancelAnimationFrame(this.animationId);
    } else {
      this.resume();
    }
  }

  showOverlay(visible, showStart = false) {
    this.overlay.classList.toggle('visible', visible);
    this.startBtn.style.display = showStart ? 'inline-flex' : 'none';
    this.resumeBtn.style.display = showStart ? 'none' : 'inline-flex';
  }

  spawnPiece() {
    this.activePiece = this.nextPiece || new Piece(randomPieceKey());
    this.activePiece.pos.y = 0;
    this.activePiece.pos.x = ((COLS / 2) | 0) - ((this.activePiece.matrix[0].length / 2) | 0);
    this.nextPiece = new Piece(randomPieceKey());

    if (this.collide(this.board, this.activePiece)) {
      this.gameOver();
    }

    this.drawNext();
  }

  gameOver() {
    this.paused = true;
    this.showOverlay(true, true);
    this.overlay.querySelector('#startBtn').textContent = '再战一局';
  }

  update(time = 0) {
    if (this.paused) {
      this.draw();
      return;
    }

    const deltaTime = time - this.lastTime;
    this.lastTime = time;

    if (this.clearAnimation) {
      this.clearAnimation.time += deltaTime;
      if (this.clearAnimation.time >= CLEAR_DURATION) {
        this.finishClear();
      }
    } else {
      this.dropCounter += deltaTime;
      if (this.dropCounter > this.dropInterval) {
        this.drop();
      }
    }

    this.draw();
    this.animationId = requestAnimationFrame((t) => this.update(t));
  }

  drop() {
    this.activePiece.pos.y++;
    if (this.collide(this.board, this.activePiece)) {
      this.activePiece.pos.y--;
      this.merge();
      this.processLines();
      if (!this.clearAnimation) {
        this.spawnPiece();
      }
      this.dropCounter = 0;
      return;
    }
    this.dropCounter = 0;
  }

  softDrop() {
    this.dropCounter += this.settings?.softDrop || 65;
    this.drop();
  }

  hardDrop() {
    while (!this.collide(this.board, this.activePiece)) {
      this.activePiece.pos.y++;
    }
    this.activePiece.pos.y--;
    this.merge();
    this.processLines();
    if (!this.clearAnimation) {
      this.spawnPiece();
    }
    this.dropCounter = 0;
  }

  move(dir) {
    this.activePiece.pos.x += dir;
    if (this.collide(this.board, this.activePiece)) {
      this.activePiece.pos.x -= dir;
    }
  }

  rotate(dir) {
    const pos = this.activePiece.pos.x;
    let offset = 1;
    this.activePiece.rotate(dir);
    while (this.collide(this.board, this.activePiece)) {
      this.activePiece.pos.x += offset;
      offset = -(offset + (offset > 0 ? 1 : -1));
      if (offset > this.activePiece.matrix[0].length) {
        this.activePiece.rotate(-dir);
        this.activePiece.pos.x = pos;
        return;
      }
    }
  }

  collide(board, piece) {
    const { matrix, pos } = piece;
    for (let y = 0; y < matrix.length; y++) {
      for (let x = 0; x < matrix[y].length; x++) {
        if (matrix[y][x] !== 0) {
          const boardY = y + pos.y;
          const boardX = x + pos.x;
          if (
            boardX < 0 ||
            boardX >= COLS ||
            boardY >= ROWS ||
            (boardY >= 0 && board[boardY][boardX] && !board[boardY][boardX].clearing)
          ) {
            return true;
          }
        }
      }
    }
    return false;
  }

  merge() {
    this.activePiece.matrix.forEach((row, y) => {
      row.forEach((value, x) => {
        if (value !== 0) {
          const boardY = y + this.activePiece.pos.y;
          if (boardY < 0) return;
          this.board[boardY][x + this.activePiece.pos.x] = {
            value,
            clearing: false,
          };
        }
      });
    });
  }

  processLines() {
    const rowsToClear = [];
    for (let y = ROWS - 1; y >= 0; y--) {
      if (this.board[y].every((cell) => cell && !cell.clearing)) {
        rowsToClear.push(y);
      }
    }

    if (rowsToClear.length) {
      rowsToClear.forEach((y) => {
        this.board[y].forEach((cell) => {
          if (cell) cell.clearing = true;
        });
      });
      this.clearAnimation = { rows: rowsToClear, time: 0 };
      const lineScore = [0, 100, 300, 500, 800];
      this.score += lineScore[rowsToClear.length] * this.level;
      this.lines += rowsToClear.length;

      if (this.settings.levelLines !== Infinity) {
        const targetLevel = Math.floor(this.lines / this.settings.levelLines) + 1;
        if (targetLevel > this.level) {
          this.level = targetLevel;
          this.recalculateSpeed();
        }
      }
      this.updateScoreboard();
    }
  }

  finishClear() {
    const rows = this.clearAnimation.rows;
    rows.sort((a, b) => a - b).forEach((rowIndex) => {
      this.board.splice(rowIndex, 1);
      this.board.unshift(Array(COLS).fill(0));
    });
    this.clearAnimation = null;
    this.spawnPiece();
  }

  draw() {
    this.clearCanvas(this.ctx, this.canvas);
    this.drawGrid();
    this.drawMatrix(this.board, { x: 0, y: 0 });
    if (this.activePiece) {
      this.drawGhost();
      this.drawMatrix(this.activePiece.matrix, this.activePiece.pos, { active: true });
    }
    if (this.clearAnimation) {
      this.drawClearingEffect();
    }
  }

  clearCanvas(ctx, canvas) {
    ctx.save();
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.restore();
  }

  drawGrid() {
    const ctx = this.ctx;
    ctx.save();
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.globalAlpha = 0.2;
    ctx.strokeStyle = '#341b67';
    ctx.lineWidth = 1;
    for (let x = 0; x <= COLS; x++) {
      ctx.beginPath();
      ctx.moveTo(x * BLOCK_SIZE, 0);
      ctx.lineTo(x * BLOCK_SIZE, ROWS * BLOCK_SIZE);
      ctx.stroke();
    }
    for (let y = 0; y <= ROWS; y++) {
      ctx.beginPath();
      ctx.moveTo(0, y * BLOCK_SIZE);
      ctx.lineTo(COLS * BLOCK_SIZE, y * BLOCK_SIZE);
      ctx.stroke();
    }
    ctx.restore();
  }

  drawMatrix(matrix, offset, options = {}) {
    matrix.forEach((row, y) => {
      row.forEach((value, x) => {
        const cell = typeof value === 'object' ? value : null;
        const cellValue = cell ? cell.value : value;
        if (cellValue) {
          const drawX = (x + offset.x) * BLOCK_SIZE;
          const drawY = (y + offset.y) * BLOCK_SIZE;
          this.drawCell(drawX, drawY, cellValue, options.active, cell?.clearing);
        }
      });
    });
  }

  drawCell(x, y, value, active = false, clearing = false) {
    const style = PIECE_STYLES[value];
    const ctx = this.ctx;
    const gradient = ctx.createLinearGradient(x, y, x + BLOCK_SIZE, y + BLOCK_SIZE);
    const baseColor = style?.color || '#ffffff';
    gradient.addColorStop(0, clearing ? '#ffffff' : baseColor);
    gradient.addColorStop(1, clearing ? style?.color || '#ffffff' : '#0a1124');

    ctx.save();
    drawRoundedRect(ctx, x + 2, y + 2, BLOCK_SIZE - 4, BLOCK_SIZE - 4, 8);
    ctx.fillStyle = gradient;
    ctx.fill();

    if (active) {
      ctx.shadowColor = style.glow;
      ctx.shadowBlur = 18;
    }
    ctx.lineWidth = active ? 2.2 : 1.2;
    ctx.strokeStyle = clearing ? '#ffffff' : style.glow;
    ctx.stroke();

    ctx.restore();
  }

  drawGhost() {
    const ghost = new Piece(this.activePiece.key);
    ghost.rotationIndex = this.activePiece.rotationIndex;
    ghost.matrix = cloneMatrix(this.activePiece.matrix);
    ghost.pos = { ...this.activePiece.pos };
    while (!this.collide(this.board, ghost)) {
      ghost.pos.y++;
    }
    ghost.pos.y--;
    this.ctx.save();
    this.ctx.globalAlpha = 0.35;
    this.drawMatrix(ghost.matrix, ghost.pos, { active: false });
    this.ctx.restore();
  }

  drawClearingEffect() {
    const progress = this.clearAnimation.time / CLEAR_DURATION;
    const pulse = 0.6 + Math.sin(progress * Math.PI) * 0.4;
    this.ctx.save();
    this.ctx.globalAlpha = pulse;
    this.clearAnimation.rows.forEach((rowIndex) => {
      this.ctx.fillStyle = 'rgba(255, 255, 255, 0.25)';
      this.ctx.fillRect(0, rowIndex * BLOCK_SIZE, COLS * BLOCK_SIZE, BLOCK_SIZE);
    });
    this.ctx.restore();
  }

  drawNext() {
    const ctx = this.nextCtx;
    this.clearCanvas(ctx, this.nextCanvas);
    ctx.save();
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.fillStyle = 'rgba(8, 2, 30, 0.8)';
    ctx.fillRect(0, 0, this.nextCanvas.width, this.nextCanvas.height);
    ctx.restore();

    if (!this.nextPiece) return;

    const matrix = this.nextPiece.matrix;
    const offsetX = ((4 - matrix[0].length) / 2) * BLOCK_SIZE * 0.75;
    const offsetY = ((4 - matrix.length) / 2) * BLOCK_SIZE * 0.75;

    matrix.forEach((row, y) => {
      row.forEach((value, x) => {
        if (value) {
          const drawX = offsetX + x * BLOCK_SIZE * 0.75 + 20;
          const drawY = offsetY + y * BLOCK_SIZE * 0.75 + 20;
          const style = PIECE_STYLES[value];
          const gradient = ctx.createLinearGradient(drawX, drawY, drawX + 22, drawY + 22);
          gradient.addColorStop(0, style.color);
          gradient.addColorStop(1, '#0a1124');

          ctx.save();
          drawRoundedRect(ctx, drawX, drawY, 22, 22, 6);
          ctx.fillStyle = gradient;
          ctx.fill();
          ctx.lineWidth = 1.6;
          ctx.strokeStyle = style.glow;
          ctx.stroke();
          ctx.restore();
        }
      });
    });
  }

  updateScoreboard() {
    this.scoreEl.textContent = this.score;
    this.linesEl.textContent = this.lines;
    this.levelEl.textContent = this.level;
  }

  recalculateSpeed() {
    const levelFactor = Math.pow(this.settings.speedFactor, Math.max(0, this.level - 1));
    this.dropInterval = Math.max(120, this.baseDropInterval * levelFactor);
  }
}

window.addEventListener('DOMContentLoaded', () => {
  new TetrisGame();
});
