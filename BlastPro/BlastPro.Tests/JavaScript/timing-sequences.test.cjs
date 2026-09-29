const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');

const source = fs.readFileSync(path.resolve(__dirname, '../../BlastPro/wwwroot/js/pattern-design.js'), 'utf8');
const start = source.indexOf('    function timingRowIndex(row) {');
const end = source.indexOf('    function describeTiming() {', start);
assert.ok(start >= 0 && end > start, 'timing implementation is present');
const implementation = source.slice(start, end);

function delays(order, rowCount, columnCount, omit = []) {
    const inputs = {
        Burden: { value: '4' }, Spacing: { value: '5' },
        PatternType: { value: 'Rectangular' }, TimingOrder: { value: order },
        TimingStepMs: { value: '25' }
    };
    const holes = [];
    for (let r = 0; r < rowCount; r++) {
        for (let c = 0; c < columnCount; c++) {
            if (!omit.some(([y, x]) => y === r && x === c))
                holes.push({ r, c, X: 2.5 + 5 * c, Y: 2 + 4 * r });
        }
    }
    const context = {
        get: id => inputs[id],
        num: id => Number.parseFloat(inputs[id].value),
        cell: (hole, key) => hole[key],
        rows: () => [...holes]
    };
    const result = vm.runInNewContext(implementation + '\n timingAssignments()', context);
    return Object.fromEntries(result.map(({ row, delay }) => [`${row.r},${row.c}`, delay]));
}

test('chevron produces symmetric V wavefronts, including an even-width centre pair', () => {
    const grid = delays('Chevron', 3, 5);
    assert.deepEqual([0, 1, 2, 3, 4].map(c => grid[`0,${c}`]), [50, 25, 0, 25, 50]);
    assert.deepEqual([0, 1, 2, 3, 4].map(c => grid[`1,${c}`]), [75, 50, 25, 50, 75]);
    const even = delays('Chevron', 1, 4);
    assert.deepEqual([0, 1, 2, 3].map(c => even[`0,${c}`]), [25, 0, 0, 25]);
});

test('interleaved rows start a following row at the preceding row midpoint', () => {
    const grid = delays('HalfRowOverlap', 3, 5);
    assert.deepEqual([0, 1, 2, 3, 4].map(c => grid[`0,${c}`]), [0, 25, 50, 75, 100]);
    assert.deepEqual([0, 1, 2, 3, 4].map(c => grid[`1,${c}`]), [50, 75, 100, 125, 150]);
    assert.equal(grid['2,0'], 100);
});

test('echelon advances diagonally from the front-right free faces', () => {
    const grid = delays('Echelon', 3, 5);
    assert.deepEqual([0, 1, 2, 3, 4].map(c => grid[`0,${c}`]), [100, 75, 50, 25, 0]);
    assert.deepEqual([0, 1, 2, 3, 4].map(c => grid[`1,${c}`]), [125, 100, 75, 50, 25]);
    assert.equal(grid['2,4'], 50);
});

test('removed holes are omitted and remaining row times are recalculated', () => {
    const grid = delays('HalfRowOverlap', 2, 5, [[0, 1]]);
    assert.equal(grid['0,1'], undefined);
    assert.deepEqual([0, 2, 3, 4].map(c => grid[`0,${c}`]), [0, 25, 50, 75]);
    assert.equal(grid['1,0'], 50);
});
