# SigNoz for See Sharp

Self-hosted SigNoz that the API exports OpenTelemetry to. Traces, metrics,
and logs from `see-sharp-api` land here.

This stack is installed and managed with Foundry, the official SigNoz CLI.
The old vendored docker-compose files from the SigNoz repo are deprecated,
so we keep a small `casting.yaml` here and let Foundry generate the real
compose files into `pours/`.

## What runs

- SigNoz UI and query service on http://localhost:8080
- OTLP ingester on ports 4317 (gRPC) and 4318 (HTTP)
- ClickHouse for telemetry storage
- Postgres for SigNoz metadata
- ClickHouse Keeper for coordination

Plan for a few GB of memory for Docker while this is up.

## Start

```bash
foundryctl cast -f deploy/signoz/casting.yaml
```

Or, once `pours/` exists, plain compose works too:

```bash
docker compose -f deploy/signoz/pours/deployment/compose.yaml up -d
```

First start takes a minute. The ingester waits for schema migrations, so
give it a moment before judging missing traces.

## Use it

- UI: http://localhost:8080
- The API reads `Otel:Endpoint`, which defaults to `http://localhost:4317`
- Run the API, hit a few endpoints, then look for the `see-sharp-api`
  service in the UI under Services

Note: the original plan mentioned UI port 3301. Foundry publishes 8080
instead, so 8080 is the real URL.

## Stop and remove

```bash
docker compose -f deploy/signoz/pours/deployment/compose.yaml down
```

Add `-v` to also delete the data volumes if you want a clean slate.

## Reinstall from scratch

Delete `pours/` and the SigNoz volumes, then run `foundryctl cast` again.
