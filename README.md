# Kubernetes Ansible Project

School project for learning Kubernetes infrastructure automation using Ansible, Kubespray, and GitHub Actions.

## Technology

- Ansible
- Kubespray
- Kubernetes
- GitHub Actions
- Self-hosted GitHub Actions runner
- Linux virtual machines

## Repository Structure

```text
.
├── .github/
│   └── workflows/
├── ansible/
│   ├── inventory/
│   ├── playbooks/
│   ├── roles/
│   ├── group_vars/
│   └── host_vars/
├── kubespray/
│   └── inventory/
├── docs/
└── ansible.cfg
```

Detailed documentation:

- [Infrastructure repository structure](docs/infrastructure-README.md)
- [Shared Ansible and Kubespray runtime](docs/automation-runtime.md)
- [GitHub Actions self-hosted runner](docs/github-actions-runner.md)
- [GitHub SSH access for collaborators](docs/github-ssh.md)

## Current Status

Completed:

- Shared Ansible runtime installed under `/opt/automation`
- Kubespray `v2.31.0` installed with pinned dependencies
- Repository-local Ansible configuration created
- Kubespray inventory template added
- GitHub Actions self-hosted runner configured
- Ansible validation workflow tested successfully

Remaining:

- Create Kubernetes node virtual machines
- Configure SSH access to the nodes
- Add real hosts to the Ansible and Kubespray inventories
- Run Kubespray prechecks
- Deploy and validate the Kubernetes cluster

## Validate Ansible

Run from the repository root:

```bash
source /opt/automation/bin/activate-kubespray

ansible-inventory --graph

ansible-playbook   ansible/playbooks/setup.yml   --syntax-check
```
