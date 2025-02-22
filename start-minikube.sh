#!/bin/bash

set -e  # Exit immediately if a command fails

# Get the directory of the script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Change to the "minikube/manifests" subfolder
cd "$SCRIPT_DIR/minikube/manifests"

# Apply the Kubernetes manifests using Minikube's kubectl
minikube kubectl -- apply -k .