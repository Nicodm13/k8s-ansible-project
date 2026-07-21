# Applications

Flux reconciles this directory through the `applications` Kustomization.

Nothing is deployed until an application directory is added to `resources` in `applications/kustomization.yaml`.

Recommended structure for each application:

```text
applications/
  <app-name>/
    namespace.yaml
    deployment.yaml
    service.yaml
    ingress.yaml
    kustomization.yaml
```

To deploy a new application:

1. Copy `applications/_template` to `applications/<app-name>`.
2. Replace names, image, ports, and ingress host/path.
3. Add `- <app-name>` to `applications/kustomization.yaml`.
4. Commit and push.
5. Reconcile Flux.

For Traefik ingress, use:

```yaml
ingressClassName: traefik
```

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
kubectl -n taxsystem get deployments
```
