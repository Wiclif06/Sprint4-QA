const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
const json=p=>JSON.parse(fs.readFileSync(path.join(root,p),'utf8').replace(/^\uFEFF/,''));
const c=json('testes-manuais/plano_casos_teste.json');
assert.equal(c.length,49);assert.equal(new Set(c.map(x=>x.ID)).size,49);assert.equal(new Set(c.map(x=>x['PBI Azure'])).size,34);
for(const x of c)for(const key of ['Objetivo / cenário','Dados de entrada controlados','Saída esperada','Procedimento detalhado','Pré-condições','ID Azure','PBI Azure'])assert.ok(x[key],`${x.ID}: ${key} ausente`);
const collection=json('postman/PetJourney_QA_S4.postman_collection.json');assert.equal(collection.item.length,33);
assert.ok(collection.item.every(x=>x.event.some(e=>e.listen==='test')));
const env=json('postman/PetJourney_QA_Local.postman_environment.json');assert.equal(env.values.find(x=>x.key==='password').value,'');
const r=json('evidencias/newman-resultados.json');assert.equal(r.requests,33);assert.equal(r.assertions,53);assert.equal(r.failures.length,0);assert.equal(r.failedAssertions,0);
const serialized=JSON.stringify(r);assert.ok(!/eyJ[A-Za-z0-9_-]{15,}\./.test(serialized),'JWT indevido no relatório');
const ct=new Set(r.executions.flatMap(e=>e.name.match(/CT\d{3}/g)||[]));assert.equal(ct.size,19);
assert.ok(r.executions.every(e=>e.assertions.every(a=>a.passed)));
const load=json('evidencias/desempenho.json');assert.equal(load.stages.reduce((n,s)=>n+s.total,0),330);assert.ok(load.passed);
assert.ok(fs.statSync(path.join(root,'evidencias/execucao-qa.mp4')).size>0);
const stress=json('evidencias/estresse.json');
assert.deepEqual(stress.stages.map(s=>s.concurrency),[1,10,50,100,1]);
assert.equal(stress.stages.at(-1).phase,'recovery');
assert.ok(stress.passed);
for(const s of stress.stages){
 assert.ok(s.total>0);assert.equal(s.samples.length,s.total);
 assert.equal(s.errors,0);assert.ok(s.samples.every(x=>x.ok&&x.status===200));
 const times=s.samples.map(x=>x.ms).sort((a,b)=>a-b);
 assert.equal(s.p95Ms,times[Math.ceil(times.length*.95)-1]);
 assert.ok(s.p95Ms<stress.criteria.maxP95Ms);
}
console.log('Pacote conferido: 49 casos / 34 PBIs / 33 requisições / 53 asserções / carga e estresse limitado aprovados / vídeo presente.');
