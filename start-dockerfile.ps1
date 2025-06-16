& ".\docker-build.ps1"
docker compose -p asars-resources-sample down
docker compose -p asars-resources-sample -f samples/dockerfile/compose.yaml up -d