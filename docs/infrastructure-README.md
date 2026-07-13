# Infrastructure Automation Repository

This repository contains the project-specific infrastructure configuration for Ansible, Kubespray, and GitHub Actions.

The shared Ansible and Kubespray runtime is installed separately under:

```text
/opt/automation
```

Project playbooks, inventory, variables, and workflows belong in this repository. They must not be stored under `/etc/ansible`.

## Repository Location

The manual administration checkout is expected to be located at:

```text
/home/freb/infrastructure
```

GitHub Actions will later check out the repository into its own runner workspace under:

```text
/home/gha-runner/actions-runner/_work/
```

Do not use `/home/gha-runner/infrastructure` as the GitHub Actions workspace.

## Repository Structure

```text
infrastructure/
├── .github/
│   └── workflows/
├── ansible.cfg
├── ansible/
│   ├── inventory/
│   │   └── hosts.ini
│   ├── playbooks/
│   │   └── setup.yml
│   ├── roles/
│   ├── group_vars/
│   │   └── all.yml
│   └── host_vars/
├── kubespray/
│   ├── KUBESPRAY_VERSION
│   ├── README.md
│   └── inventory/
│       └── mycluster/
├── .gitignore
└── README.md
```

## Shared Runtime

Activate the shared runtime explicitly before running Ansible or Kubespray commands:

```bash
source /opt/automation/bin/activate-kubespray
```

Verify:

```bash
command -v ansible
ansible --version
```

Expected executable:

```text
/opt/automation/bin/ansible
```

The environment is not automatically activated through `.bashrc`.

## Repository-Local Ansible Configuration

The repository contains its own `ansible.cfg`:

```ini
[defaults]
inventory = ./ansible/inventory/hosts.ini
roles_path = ./ansible/roles
collections_path = ~/.ansible/collections:/usr/share/ansible/collections
host_key_checking = True
retry_files_enabled = False
interpreter_python = auto_silent
forks = 10
timeout = 30

[privilege_escalation]
become = True
become_method = sudo
become_ask_pass = False

[ssh_connection]
pipelining = True
ssh_args = -o ControlMaster=auto -o ControlPersist=60s
```

Run Ansible commands from the repository root so this configuration is discovered.

Verify:

```bash
cd /home/freb/infrastructure
source /opt/automation/bin/activate-kubespray

ansible --version
ansible-config dump --only-changed
```

The output should include:

```text
config file = /home/freb/infrastructure/ansible.cfg
```

## Normal Ansible Inventory

The normal Ansible inventory is stored at:

```text
ansible/inventory/hosts.ini
```

Initial structure:

```ini
[managed_nodes]

# Add managed machines here.
#
# Example:
# node1 ansible_host=192.0.2.10 ansible_user=ansible
# node2 ansible_host=192.0.2.11 ansible_user=ansible
```

Replace the example values with real hosts before deployment.

## Initial Playbook

The initial playbook is stored at:

```text
ansible/playbooks/setup.yml
```

Example:

```yaml
---
- name: Validate managed nodes
  hosts: managed_nodes
  gather_facts: true
  become: true

  tasks:
    - name: Verify Ansible connectivity
      ansible.builtin.ping:
```

Validate it:

```bash
source /opt/automation/bin/activate-kubespray

ansible-playbook   ansible/playbooks/setup.yml   --syntax-check
```

## Shared Variables

Variables shared by all normal Ansible-managed hosts are stored in:

```text
ansible/group_vars/all.yml
```

Initial contents:

```yaml
---
# Variables shared by all normal Ansible-managed hosts.
```

Host-specific variables belong under:

```text
ansible/host_vars/
```

Custom roles belong under:

```text
ansible/roles/
```

## Kubespray Version

The repository records the required Kubespray version in:

```text
kubespray/KUBESPRAY_VERSION
```

Current value:

```text
v2.31.0
```

The shared Kubespray source is installed at:

```text
/opt/automation/sources/kubespray-v2.31.0
```

The repository stores only project-specific Kubespray inventory and configuration.

Do not copy or modify the full Kubespray source inside this repository.

## Kubespray Inventory

The project-specific Kubespray inventory is stored under:

```text
kubespray/inventory/mycluster/
```

It is created from the sample inventory belonging to the pinned Kubespray version.

The files must be customized before deploying a cluster.

Typical contents include:

```text
kubespray/inventory/mycluster/
├── inventory.ini
├── group_vars/
│   ├── all/
│   ├── etcd.yml
│   └── k8s_cluster/
└── patches/
```

The exact structure depends on the selected Kubespray release.

## Running Kubespray

Kubespray commands must run from the pinned Kubespray source directory because Kubespray uses its own `ansible.cfg`, roles, plugins, and playbooks.

From the infrastructure repository root:

```bash
source /opt/automation/bin/activate-kubespray

REPOSITORY_ROOT="$(pwd)"
KUBESPRAY_VERSION="$(cat kubespray/KUBESPRAY_VERSION)"
KUBESPRAY_SOURCE="/opt/automation/sources/kubespray-${KUBESPRAY_VERSION}"

cd "$KUBESPRAY_SOURCE"
```

Run the cluster playbook:

```bash
ansible-playbook   -i "$REPOSITORY_ROOT/kubespray/inventory/mycluster/inventory.ini"   cluster.yml
```

Do not run Kubespray's `cluster.yml` from the infrastructure repository root.

## Validation Commands

Run these from the repository root:

```bash
cd /home/freb/infrastructure
source /opt/automation/bin/activate-kubespray
```

Validate the active Ansible runtime:

```bash
command -v ansible
ansible --version
```

Validate the repository configuration:

```bash
ansible-config dump --only-changed
```

Validate the normal inventory:

```bash
ansible-inventory --graph
```

Validate the playbook syntax:

```bash
ansible-playbook   ansible/playbooks/setup.yml   --syntax-check
```

The `managed_nodes` group may initially be empty. That is expected until real hosts are added.

## Git Setup

Initialize the repository:

```bash
cd /home/freb/infrastructure
git init -b main
```

Inspect the repository:

```bash
git status
```

Stage files:

```bash
git add .
```

Review staged changes:

```bash
git status
git diff --cached --stat
```

Do not commit secrets.

## Credentials

Credentials remain user-specific.

Manual administration credentials belong to:

```text
/home/freb/.ssh/
/home/freb/.kube/
```

GitHub Actions credentials belong to:

```text
/home/gha-runner/.ssh/
/home/gha-runner/.kube/
/home/gha-runner/secrets/
```

Do not store credentials in:

```text
/opt/automation
```

Do not commit credentials to Git.

## Git Ignore Rules

The repository should ignore temporary files, local environments, generated credentials, and secrets.

Recommended `.gitignore`:

```gitignore
# Ansible temporary files
*.retry
.ansible/

# Python
__pycache__/
*.py[cod]
.venv/
venv/

# Secrets and credentials
*.pem
*.key
*.p12
*.pfx
*.kubeconfig
vault-password*
secrets/
.env
.env.*

# Generated Kubernetes credentials
admin.conf
artifacts/

# Editor and operating-system files
.vscode/
.idea/
.DS_Store
Thumbs.db
```

## GitHub Actions

GitHub Actions will run as the `gha-runner` user.

The runner will check out the repository into a workspace similar to:

```text
/home/gha-runner/actions-runner/_work/<repository>/<repository>
```

Every workflow step that uses Ansible or Kubespray must activate the shared runtime explicitly:

```yaml
- name: Verify Ansible environment
  shell: bash
  run: |
    source /opt/automation/bin/activate-kubespray
    ansible --version
```

Do not depend on interactive shell startup files.

## Rules

- Keep project files in Git.
- Do not store playbooks or inventory under `/etc/ansible`.
- Use the shared runtime under `/opt/automation`.
- Activate the runtime explicitly.
- Keep the runtime root-owned and read-only.
- Keep Kubespray pinned to an approved release.
- Do not track Kubespray `master`.
- Keep SSH and Kubernetes credentials user-specific.
- Do not commit secrets.
- Run normal Ansible commands from the infrastructure repository root.
- Run Kubespray playbooks from the pinned Kubespray source directory.
- Let GitHub Actions use its own runner workspace.
