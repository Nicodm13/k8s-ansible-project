# Cloudflare Tunnel

This directory deploys `cloudflared` into the existing `traefik` namespace.

The tunnel token is intentionally not committed to Git. Create it in the cluster before reconciling this component:

```bash
kubectl create secret generic cloudflare-tunnel-token \
  -n traefik \
  --from-literal=token='<cloudflare-tunnel-token>'
```

In Cloudflare Zero Trust, configure the tunnel public hostname to route to Traefik:

```text
Service: http://traefik.traefik.svc.cluster.local:80
```

Cloudflare terminates public traffic and `cloudflared` forwards it to Traefik inside the cluster.
