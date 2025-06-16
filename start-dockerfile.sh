#! /bin/bash
bash docker-build.sh
docker compose -p asars-resources-sample -f samples/dockerfile/compose.yaml up -d