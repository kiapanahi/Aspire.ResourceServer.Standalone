#!/bin/sh

set -e

# Only enable 'pipefail' if the shell supports it (i.e., bash or zsh)
if (echo "$0" | grep -qE 'bash|zsh') || (test -n "$BASH_VERSION") || (set -o pipefail 2>/dev/null); then
  set -o pipefail
fi

current_date=$(date +%Y%m%d)

docker pull mcr.microsoft.com/dotnet/sdk:9.0
docker pull mcr.microsoft.com/dotnet/aspnet:9.0

docker buildx build --platform linux/amd64 -f Dockerfile \
  -t aspire-resource-server:latest \
  -t aspire-resource-server:$current_date \
  .