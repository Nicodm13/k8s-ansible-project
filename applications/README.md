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
