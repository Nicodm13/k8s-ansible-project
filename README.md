# Kubernetes Ansible Project

Final project for deploying and operating a Kubernetes-based microservice system on Linux virtual machines using Ansible, Kubespray, Flux, and GitHub Actions.

## Overview

This repository contains both the infrastructure automation and the application manifests for a small production-style Kubernetes environment.

The cluster is provisioned with Kubespray and managed through GitOps. Flux watches this repository and reconciles the infrastructure and application manifests into the cluster.

The deployed application is **TaxSystem**, an event-driven .NET microservice system that uses RabbitMQ for messaging and PostgreSQL through CloudNativePG for persistence.

## Architecture

```text
GitHub repository
        |
        | push to main
        v
GitHub Actions
        |
        | build/test/publish images and update manifests
        v
Flux in Kubernetes
        |
        | reconcile repository state
        v
Azure Linux VMs running Kubernetes
```

## Cluster

The Kubernetes cluster is built with Kubespray.

Current node layout:

```text
controlnode   control plane + etcd
worker1       worker node
worker2       worker node
```

The cluster uses:

- Debian Linux virtual machines
- Kubernetes managed by Kubespray
- containerd as the container runtime
- Calico for pod networking
- Flux for GitOps reconciliation
- Traefik as the ingress controller
- Cloudflare Tunnel for external access
- Local Path Provisioner for persistent volumes

## Applications

Applications are reconciled from `applications/`.

Current applications:

- `applications/grafana`: Grafana dashboard exposed through Traefik.
- `applications/TaxSystem`: .NET tax processing system.

TaxSystem consists of:

- `TaxSystem.Client`: HTTP API gateway.
- `TaxSystem.CitizenService`: citizen registration and lookup service.
- `TaxSystem.CompanyService`: company registration and salary reporting service.
- `TaxSystem.BankService`: bank transfer service.
- `TaxSystem.StatementGeneratorService`: tax statement generation service.
- `TaxSystem.Shared`: shared models, messaging contracts, RabbitMQ setup, and persistence helpers.

Runtime dependencies:

- RabbitMQ, installed through a Flux-managed HelmRelease.
- PostgreSQL, managed by CloudNativePG.
- Traefik ingress for HTTP routing.

## Infrastructure

Infrastructure is reconciled from `infrastructure/`.

```text
infrastructure/
├── database/       CloudNativePG operator
├── messaging/      RabbitMQ Helm release
├── monitoring/     kube-prometheus-stack
└── networking/     Traefik and Cloudflared
```

Flux entry points live under `clusters/azure/flux/`:

- `infrastructure.yaml` reconciles `./infrastructure`.
- `applications.yaml` reconciles `./applications` after infrastructure is ready.

## Automation

The repository contains automation for both cluster operations and application delivery.

```text
.github/workflows/
├── ansible-validate.yml
├── kubespray.yml
├── taxsystem-run-tests.yml
└── build-application-image.yml
```

The application image workflow:

- Detects which TaxSystem services changed.
- Builds only the affected .NET projects.
- Publishes container images to GitHub Container Registry.
- Updates the Kubernetes manifests with the new image tags.
- Commits the manifest update back to `main` for Flux to deploy.

## Repository Layout

```text
.
├── .github/workflows/          GitHub Actions workflows
├── applications/               Flux-managed applications
│   ├── grafana/
│   └── TaxSystem/
├── clusters/azure/             Azure cluster inventory, Kubespray config, and Flux bootstrap
│   ├── ansible/
│   ├── flux/
│   └── kubespray/
├── infrastructure/             Flux-managed platform components
└── README.md
```

## Local Development

The TaxSystem source code is under `applications/TaxSystem/src`.

Build the solution:

```bash
cd applications/TaxSystem/src
dotnet build TaxSystem.sln
```

Run service-level tests:

```bash
dotnet test TaxSystem.Tests/TaxSystem.Tests.csproj
```

Run the full local test flow with Minikube:

```bash
./run-tests.sh
```

The full test runner builds the services, deploys the Kubernetes manifests to Minikube, waits for RabbitMQ/PostgreSQL/application readiness, and runs E2E tests.

## GitOps Operations

Check Flux state:

```bash
kubectl get kustomizations -A
kubectl get helmreleases -A
```

Force reconciliation:

```bash
flux reconcile source git flux-system -n flux-system
flux reconcile kustomization infrastructure -n flux-system --with-source
flux reconcile kustomization applications -n flux-system --with-source
```

Check TaxSystem:

```bash
kubectl -n taxsystem get pods,svc,ingress
kubectl -n taxsystem get cluster taxsystem-db
```

## Public Endpoints

The cluster exposes services through Cloudflare Tunnel and Traefik.

Current ingress hosts:

- `taxsystem.kvikit.dk`
- `grafana.kvikit.dk`

## Notes

- The desired cluster and application state lives in Git.
- Manual cluster changes should be avoided unless they are temporary debugging steps.
- Long-term changes should be made in this repository and deployed by Flux.
- Kubernetes Secrets in this repository are for the project environment and should be replaced with a stronger secret-management approach for real production use.
