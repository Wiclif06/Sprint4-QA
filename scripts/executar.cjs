const fs=require('node:fs');
const path=require('node:path');
const newman=require('newman');
const root=path.resolve(__dirname,'..');
const escape=s=>String(s).replace(/[&<>\"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
function execute({password=process.env.QA_PASSWORD,baseUrl=process.env.QA_BASE_URL||'http://127.0.0.1:5188',delay=0,onEvent=()=>{}}={}){
 if(!password)throw Error('Defina QA_PASSWORD com a senha do ambiente QA descartável.');
 if(!/^http:\/\/(127\.0\.0\.1|localhost):\d+$/.test(baseUrl))throw Error('Este executor é restrito a localhost. Revise a coleção antes de testar outro ambiente.');
 const startedAt=new Date().toISOString();
 const collection=JSON.parse(fs.readFileSync(path.join(root,'postman/PetJourney_QA_S4.postman_collection.json')));
 const environment=JSON.parse(fs.readFileSync(path.join(root,'postman/PetJourney_QA_Local.postman_environment.json')));
 return new Promise((resolve,reject)=>{
  newman.run({collection,environment,envVar:[{key:'password',value:password},{key:'allowWriteTests',value:'true'},{key:'baseUrl',value:baseUrl}],delayRequest:delay,timeoutRequest:10000,reporters:[]},(err,s)=>{
   if(err)return reject(err);
   const executions=s.run.executions.map(e=>({name:e.item.name,method:e.request.method,url:e.request.url.toString(),status:e.response?.code,timeMs:e.response?.responseTime,body:e.item.name.includes('Login')?'[Token omitido]':e.response?.stream?.toString(),assertions:(e.assertions||[]).map(a=>({name:a.assertion,passed:!a.error,error:a.error?.message}))}));
   const report={startedAt,finishedAt:new Date().toISOString(),environment:'Development / SQLite / Demo isolado; API clínica ausente intencionalmente em CT018; sem Oracle real',tool:'Newman '+require('newman/package.json').version,requests:s.run.stats.requests.total,assertions:s.run.stats.assertions.total,failedAssertions:s.run.stats.assertions.failed,failures:s.run.failures.map(f=>({name:f.source?.name,error:f.error?.message})),executions};
   const dir=path.join(root,'evidencias');fs.mkdirSync(dir,{recursive:true});
   fs.writeFileSync(path.join(dir,'newman-resultados.json'),JSON.stringify(report,null,2));
   const rows=executions.map(e=>`<tr><td>${escape(e.name)}</td><td>${e.method} ${escape(new URL(e.url).pathname+new URL(e.url).search)}</td><td>${e.status}</td><td>${e.timeMs} ms</td><td>${e.assertions.every(a=>a.passed)?'Aprovado':'Falhou'}</td></tr>`).join('');
   fs.writeFileSync(path.join(dir,'relatorio.html'),`<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>PetJourney QA — resultados</title><style>body{font:16px system-ui;margin:40px;color:#192b3b}table{border-collapse:collapse;width:100%}td,th{border:1px solid #ccc;padding:10px;text-align:left}th{background:#edf2f5}small{color:#456}</style><h1>PetJourney QA Sprint 4</h1><p>Execução real: ${escape(report.startedAt)}. ${report.requests} requisições, ${report.assertions} asserções, ${report.failures.length} falhas.</p><p>${escape(report.environment)}</p><table><thead><tr><th>Cenário</th><th>Requisição</th><th>HTTP</th><th>Tempo</th><th>Resultado</th></tr></thead><tbody>${rows}</tbody></table><p>Conteúdo gerado a partir do retorno do Newman. Não é registro de execução manual nem comprovação de produção.</p></html>`);
   onEvent({type:'done',report});resolve(report);
  }).on('beforeRequest',(err,a)=>onEvent({type:'request',name:a.item.name,method:a.request.method,url:a.request.url.toString()}))
   .on('assertion',(err,a)=>onEvent({type:'assertion',name:a.assertion,passed:!err,error:err?.message}));
 });
}
module.exports={execute};
if(require.main===module)execute({onEvent:e=>{if(e.type==='request')console.log(e.name);if(e.type==='assertion')console.log((e.passed?'OK ':'FALHOU ')+e.name);}}).then(r=>{console.log(`${r.requests} requisições; ${r.assertions} asserções; ${r.failures.length} falhas.`);process.exitCode=r.failures.length?1:0}).catch(e=>{console.error(e.message);process.exitCode=1});
