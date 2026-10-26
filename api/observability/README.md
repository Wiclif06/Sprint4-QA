# Coleta de telemetria

O modo console é suficiente para conferir localmente as emissões reais. Para monitoramento contínuo, configure um OpenTelemetry Collector Contrib com `otel-collector.yaml`. O arquivo recebe OTLP gRPC em 4317 e HTTP em 4318 e expõe métricas Prometheus em 9464; traces aparecem no exportador debug do coletor. Se houver Jaeger, Tempo ou Application Insights no ambiente, substitua ou acrescente o exportador correspondente no pipeline de traces.

Executável do coletor instalado no ambiente:

```text
otelcol-contrib --config observability/otel-collector.yaml
```

Na API, defina `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`. Um Prometheus do ambiente deve fazer scrape em `localhost:9464`, ajustando o endereço quando coletor e Prometheus estiverem em máquinas/containers diferentes. As portas do exemplo são para acesso local; exposição remota deve seguir a configuração de rede do ambiente.

As séries customizadas convertidas para Prometheus seguem a normalização do coletor. Confira os nomes efetivos no endpoint 9464/metrics. Com os nomes normalizados usuais:

```promql
sum(rate(petjourney_errors_total[5m])) / clamp_min(sum(rate(petjourney_requests_total[5m])), 1e-9)
```

A fórmula representa proporção de respostas 5xx; multiplicar por 100 exibe porcentagem. Calcule p95 pelo histograma `petjourney_request_duration_milliseconds_bucket` quando esse for o nome produzido pelo coletor:

```promql
histogram_quantile(0.95, sum by (le) (rate(petjourney_request_duration_milliseconds_bucket[5m])))
```

As requisições de saúde também entram no contador customizado; filtre `http_route` para comparar apenas rotas de negócio quando necessário. O enunciado exige métricas de tempo de resposta e taxa de erro, sem impor um SLA numérico. A meta antiga de 700 ms é objetivo do produto, não resultado de teste de carga desta entrega.

A exportação console foi exercitada localmente. O arquivo do coletor é configuração para o ambiente alvo; um servidor Prometheus remoto não foi provisionado ou declarado validado.
