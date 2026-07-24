# Applications

Flux reconciles this directory through the `applications` Kustomization.

Nothing is deployed until an application directory is added to `resources` in `applications/kustomization.yaml`.

Current applications:

- `grafana`: Grafana dashboard exposed through Traefik.
- `TaxSystem`: .NET microservice system with RabbitMQ and CloudNativePG/PostgreSQL.

Typical structure for a simple application:

```text
applications/
  <app-name>/
    namespace.yaml
    deployment.yaml or statefulset.yaml
    service.yaml
    ingress.yaml
    kustomization.yaml
```

To create and deploy a new application:

1. Create a new directory under `applications/<app-name>`.
2. Add a `kustomization.yaml` that lists the manifests for the application.
3. Add the Kubernetes resources the application needs, usually a namespace, workload, service, and optional ingress.
4. Use a `Deployment` for stateless workloads and a `StatefulSet` when the workload needs stable pod identity or per-replica persistent storage.
5. Add `- <app-name>` to `applications/kustomization.yaml`.
6. Commit and push to `main`.
7. Reconcile Flux or wait for the next automatic reconciliation.

Minimum application example:

```text
applications/
  example-app/
    namespace.yaml
    deployment.yaml
    service.yaml
    ingress.yaml
    kustomization.yaml
```

Example `applications/example-app/kustomization.yaml`:

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - namespace.yaml
  - deployment.yaml
  - service.yaml
  - ingress.yaml
```

For Traefik ingress, use:

```yaml
ingressClassName: traefik
```

## TaxSystem Workloads

TaxSystem is deployed from `applications/TaxSystem`.

The current workload layout is:

- `client`: Deployment, exposed through `taxsystem.kvikit.dk`.
- `citizen-service`: Deployment.
- `company-service`: Deployment.
- `bank-service`: Deployment.
- `statementgenerator-service`: Deployment.
- `taxsystem-db`: CloudNativePG PostgreSQL cluster.
- `rabbitmq`: Helm-managed RabbitMQ release from `infrastructure/messaging`.

## Reset TaxSystem Database

TaxSystem uses a CloudNativePG cluster named `taxsystem-db` in the `taxsystem` namespace. To completely reset the database in the deployed cluster, delete the CNPG cluster and its PVCs, then let Flux recreate it from Git.

Find the Flux application Kustomization if needed:

```bash
kubectl get kustomizations.kustomize.toolkit.fluxcd.io -A \
  -o custom-columns='NAMESPACE:.metadata.namespace,NAME:.metadata.name,PATH:.spec.path,SOURCE:.spec.sourceRef.name'
```

Current application reconciliation command:

```bash
flux reconcile kustomization applications -n flux-system --with-source
```

Full reset:

```bash
kubectl -n taxsystem delete cluster taxsystem-db
kubectl -n taxsystem delete pvc -l cnpg.io/cluster=taxsystem-db
flux reconcile kustomization applications -n flux-system --with-source
kubectl -n taxsystem wait --for=condition=Ready cluster/taxsystem-db --timeout=180s
kubectl -n taxsystem rollout restart deployment/citizen-service deployment/company-service deployment/bank-service deployment/statementgenerator-service deployment/client
```

The service restart is required. The reset creates empty databases, and each service creates its own tables during startup with Entity Framework `EnsureCreated`. If the services are not restarted after the new CNPG cluster is ready, requests can fail with PostgreSQL errors such as `relation "companies" does not exist` or `relation "citizens" does not exist`.

Wait for the restarted services before seeding data:

```bash
kubectl -n taxsystem rollout status deployment/citizen-service
kubectl -n taxsystem rollout status deployment/company-service
kubectl -n taxsystem rollout status deployment/bank-service
kubectl -n taxsystem rollout status deployment/statementgenerator-service
kubectl -n taxsystem rollout status deployment/client
```

Verify:

```bash
kubectl -n taxsystem get cluster taxsystem-db
kubectl -n taxsystem get pods -l cnpg.io/cluster=taxsystem-db
kubectl -n taxsystem get deployments,svc,ingress
```
