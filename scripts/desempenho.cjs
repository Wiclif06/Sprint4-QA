const fs=require('node:fs');const path=require('node:path');
async function main(){
 const base=process.env.QA_BASE_URL||'http://127.0.0.1:5188';
 if(!/^http:\/\/(localhost|127\.0\.0\.1):\d+$/.test(base))throw Error('Carga permitida somente no ambiente QA local.');
 const stages=[];
 // Critérios definidos antes da execução para o ensaio local, não são SLA de produção.
 for(const [concurrency,total] of [[1,30],[5,100],[20,200]]){
  let index=0;const samples=[];const started=performance.now();
  await Promise.all(Array.from({length:concurrency},async()=>{while(index++<total){const t=performance.now();try{const r=await fetch(base+'/health/live',{signal:AbortSignal.timeout(5000)});const body=await r.text();samples.push({ms:Math.round((performance.now()-t)*100)/100,status:r.status,ok:r.status===200&&body==='Healthy'});}catch(e){samples.push({ms:performance.now()-t,status:0,ok:false,error:e.message});}}}));
  const sorted=samples.map(s=>s.ms).sort((a,b)=>a-b);const durationMs=performance.now()-started;const errors=samples.filter(s=>!s.ok).length;
  stages.push({concurrency,total,durationMs,p95Ms:sorted[Math.ceil(sorted.length*.95)-1],errors,requestsPerSecond:total/(durationMs/1000),samples});
 }
 const report={at:new Date().toISOString(),endpoint:base+'/health/live',scope:'Ensaio incremental de carga local no endpoint de liveness; não mede consultas ao banco nem capacidade máxima de produção.',criteria:{maxP95Ms:1000,maxErrors:0},stages,passed:stages.every(s=>s.p95Ms<1000&&s.errors===0)};
 fs.writeFileSync(path.join(__dirname,'../evidencias/desempenho.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({...report,stages:stages.map(({samples,...s})=>s)},null,2));process.exitCode=report.passed?0:1;
}main().catch(e=>{console.error(e);process.exitCode=1});
