const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

async function main() {
  const base = process.env.QA_BASE_URL || 'http://127.0.0.1:5188';
  assert.match(base, /^http:\/\/(localhost|127\.0\.0\.1):\d+$/, 'Use uma API QA local.');
  assert.ok(process.env.QA_PASSWORD, 'Defina QA_PASSWORD.');
  const login = await fetch(base + '/auth/login', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'carlos@petjourney.com', password: process.env.QA_PASSWORD }),
    signal: AbortSignal.timeout(10000)
  });
  assert.equal(login.status, 200, 'Falha no login da fixture QA.');
  const { token } = await login.json();
  assert.ok(token, 'Token ausente.');
  const headers = { Authorization: 'Bearer ' + token };
  const endpoint = base + '/api/pets';
  const initial = await fetch(endpoint, { headers, signal: AbortSignal.timeout(5000) });
  assert.equal(initial.status, 200);
  const expected = await initial.json();
  assert.ok(Array.isArray(expected) && expected.length > 0, 'A fixture deve conter pets.');
  const signature = rows => JSON.stringify(rows.map(p => [p.idPet, p.idTutor, p.nmPet]).sort((a, b) => a[0] - b[0]));
  const expectedSignature = signature(expected);
  const startedAt = new Date().toISOString();
  const stages = [];
  // Limites e critérios fixados antes da execução; não representam SLA de produção.
  for (const [phase, concurrency] of [['baseline', 1], ['ramp', 10], ['ramp', 50], ['peak', 100], ['recovery', 1]]) {
    const started = performance.now();
    const samples = [];
    let issued = 0;
    const maxRequests = phase === 'baseline' || phase === 'recovery' ? 100 : 1000;
    await Promise.all(Array.from({ length: concurrency }, async () => {
      while (performance.now() - started < 3000 && issued < maxRequests) {
        issued++;
        const time = performance.now();
        try {
          const response = await fetch(endpoint, { headers, signal: AbortSignal.timeout(5000) });
          const body = await response.json();
          const ok = response.status === 200 && Array.isArray(body) && signature(body) === expectedSignature;
          samples.push({ ms: +(performance.now() - time).toFixed(2), status: response.status, ok });
        } catch (error) {
          samples.push({ ms: +(performance.now() - time).toFixed(2), status: 0, ok: false, error: error.message });
        }
      }
    }));
    const durationMs = performance.now() - started;
    const sorted = samples.map(s => s.ms).sort((a, b) => a - b);
    const errors = samples.filter(s => !s.ok).length;
    const p95Ms = sorted[Math.ceil(sorted.length * 0.95) - 1];
    const stage = { phase, concurrency, total: samples.length, durationMs, errors, p95Ms,
      requestsPerSecond: samples.length / (durationMs / 1000), passed: samples.length > 0 && errors === 0 && p95Ms < 1000 };
    stages.push({ ...stage, samples });
    console.log(JSON.stringify(stage));
  }
  const report = { startedAt, finishedAt: new Date().toISOString(), endpoint,
    scope: 'Estresse limitado em API local Development/SQLite: leitura autenticada, rampa até 100 clientes e recuperação. Não determina capacidade máxima nem desempenho Oracle/produção.',
    criteria: { maxP95Ms: 1000, maxErrors: 0, maxStageDurationMs: 3000, requestTimeoutMs: 5000, maxConcurrency: 100 },
    expectedPetIds: expected.map(p => p.idPet), stages, passed: stages.every(s => s.passed),
    degradationObserved: stages.some(s => !s.passed) };
  fs.writeFileSync(path.join(__dirname, '../evidencias/estresse.json'), JSON.stringify(report, null, 2));
  console.log('Resultado: ' + (report.passed ? 'aprovado nos limites configurados' : 'degradação observada; consulte o relatório'));
  process.exitCode = report.passed ? 0 : 1;
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
