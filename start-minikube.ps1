$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

Set-Location -Path (Join-Path -Path $ScriptDir -ChildPath "minikube/manifests")

minikube kubectl -- apply -k .