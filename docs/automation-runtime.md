# Shared Ansible and Kubespray Automation Runtime

This document describes the shared Ansible and Kubespray runtime installed on the control node.

## Purpose

The control node uses one shared, root-managed automation environment for:

- Manual administration through the `freb` user
- GitHub Actions through the `gha-runner` user
- Ansible playbooks
- Kubespray cluster deployment and maintenance

The runtime is installed under `/opt/automation`.

Project-specific playbooks, inventory, variables, and workflows belong in Git repositories. They must not be stored under `/etc/ansible`.

## Users

| User | Purpose |
|---|---|
| `freb` | Manual administration |
| `gha-runner` | GitHub Actions self-hosted runner |

Both users belong to the `automation` Linux group.

Verify:

```bash
getent group automation
id freb
id gha-runner
```

## Installed Version

Kubespray version:

```text
v2.31.0
```

Ansible Core version supplied by the Kubespray requirements:

```text
2.18.18
```

The selected Kubespray version is stored in:

```text
/opt/automation/versions/KUBESPRAY_VERSION
```

The checked-out Git commit is stored in:

```text
/opt/automation/versions/KUBESPRAY_COMMIT
```

Check both:

```bash
cat /opt/automation/versions/KUBESPRAY_VERSION
cat /opt/automation/versions/KUBESPRAY_COMMIT
```

## Directory Structure

```text
/opt/automation/
├── versions/
│   ├── KUBESPRAY_VERSION
│   └── KUBESPRAY_COMMIT
├── sources/
│   └── kubespray-v2.31.0/
├── venvs/
│   ├── kubespray-v2.31.0/
│   └── kubespray-current -> kubespray-v2.31.0/
└── bin/
    ├── activate-kubespray
    ├── ansible
    ├── ansible-playbook
    ├── ansible-inventory
    ├── ansible-config
    └── ansible-galaxy
```

## Ownership and Permissions

The runtime is owned by:

```text
root:automation
```

The Kubespray source and Python virtual environment are readable and executable by both users, but only root can modify them.

Inspect permissions:

```bash
find /opt/automation \
  -maxdepth 3 \
  -printf '%M %u:%g %p -> %l\n' |
  sort
```

Verify that normal users cannot modify the runtime:

```bash
sudo -iu freb test ! -w /opt/automation/venvs/kubespray-v2.31.0 &&
  echo "freb cannot modify runtime"

sudo -iu gha-runner test ! -w /opt/automation/venvs/kubespray-v2.31.0 &&
  echo "gha-runner cannot modify runtime"
```

## Activating the Runtime

The environment is not automatically activated in `.bashrc`.

Activate it explicitly before running Ansible or Kubespray commands:

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

Expected Python environment:

```text
/opt/automation/venvs/kubespray-v2.31.0
```

The stable virtual-environment path is:

```text
/opt/automation/venvs/kubespray-current
```

## Activation Script

The activation script is located at:

```text
/opt/automation/bin/activate-kubespray
```

Its contents are:

```bash
#!/usr/bin/env bash

AUTOMATION_ROOT="/opt/automation"
VENV="${AUTOMATION_ROOT}/venvs/kubespray-current"

if [[ ! -x "${VENV}/bin/ansible" ]]; then
    echo "Ansible runtime not found at ${VENV}" >&2
    return 1 2>/dev/null || exit 1
fi

export VIRTUAL_ENV="${VENV}"
export PATH="${AUTOMATION_ROOT}/bin:${VENV}/bin:${PATH}"
unset PYTHONHOME
```

## Manual Usage

Run commands as `freb`:

```bash
source /opt/automation/bin/activate-kubespray

ansible --version
ansible-inventory --graph
ansible-playbook ansible/playbooks/setup.yml
```

The commands should normally be executed from the root of the infrastructure repository so that the repository-local `ansible.cfg` is discovered.

## GitHub Actions Usage

GitHub Actions runs as `gha-runner`.

Every workflow step that uses Ansible must activate the shared environment explicitly:

```yaml
- name: Verify Ansible environment
  shell: bash
  run: |
    source /opt/automation/bin/activate-kubespray
    ansible --version
```

Do not depend on interactive shell startup files.

## Validation Tests

Test as `freb`:

```bash
sudo -iu freb bash <<'EOF'
set -e

source /opt/automation/bin/activate-kubespray

echo "User: $(whoami)"
echo "Ansible: $(command -v ansible)"
echo "Virtual environment: ${VIRTUAL_ENV}"

ansible --version

ansible \
  localhost \
  -i localhost, \
  -c local \
  -m ping
EOF
```

Test as `gha-runner`:

```bash
sudo -iu gha-runner bash <<'EOF'
set -e

source /opt/automation/bin/activate-kubespray

echo "User: $(whoami)"
echo "Ansible: $(command -v ansible)"
echo "Virtual environment: ${VIRTUAL_ENV}"

ansible --version

ansible \
  localhost \
  -i localhost, \
  -c local \
  -m ping
EOF
```

Expected result:

```text
localhost | SUCCESS
```

## Python Interpreter Warning During Localhost Tests

A localhost test may produce a warning similar to:

```text
Platform linux on host localhost is using the discovered Python interpreter at
/opt/automation/venvs/kubespray-current/bin/python3.11
```

This is expected for the local test. Ansible selected the Python interpreter from the active virtual environment.

For managed hosts, the Python interpreter is determined on the remote machine.

## Why `config file = None` May Appear

Running:

```bash
ansible --version
```

from a home directory may show:

```text
config file = None
```

This is expected when no `ansible.cfg` exists in the current directory or another configured search location.

When Ansible is run from the infrastructure repository root, it should use the repository-local configuration.

Verify from the repository root:

```bash
source /opt/automation/bin/activate-kubespray
ansible --version
ansible-config dump --only-changed
```

## Target Repository Structure

Project-specific automation should use this structure:

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
│   └── host_vars/
├── kubespray/
└── README.md
```

The repository belongs in the GitHub Actions workspace during workflow execution.

Do not use `/etc/ansible` for project files.

## Credentials

Credentials remain user-specific.

Manual administration credentials:

```text
/home/freb/.ssh/
/home/freb/.kube/
```

GitHub Actions credentials:

```text
/home/gha-runner/.ssh/
/home/gha-runner/.kube/
/home/gha-runner/secrets/
```

Do not store SSH private keys, kubeconfig files, tokens, or passwords in `/opt/automation`.

Do not commit credentials to Git.

## Kubespray Source

The pinned Kubespray source is stored at:

```text
/opt/automation/sources/kubespray-v2.31.0
```

Verify the checked-out tag:

```bash
git \
  -C /opt/automation/sources/kubespray-v2.31.0 \
  describe --tags --exact-match
```

Expected:

```text
v2.31.0
```

The source is treated as immutable. Do not edit it directly.

Project-specific Kubespray inventory and configuration belong in the infrastructure repository.

## Stable Symlinks

The runtime exposes stable paths:

```text
/opt/automation/venvs/kubespray-current
/opt/automation/bin/ansible
/opt/automation/bin/ansible-playbook
/opt/automation/bin/ansible-inventory
/opt/automation/bin/ansible-config
/opt/automation/bin/ansible-galaxy
```

Scripts and workflows should use the stable paths rather than hard-coding the versioned virtual-environment path.

## Upgrading Kubespray

Do not modify the existing versioned environment in place.

Use this process:

1. Select and approve a new Kubespray release.
2. Clone it into a new versioned source directory.
3. Create a new versioned Python virtual environment.
4. Install the new release requirements.
5. Test it as both users.
6. Update `kubespray-current`.
7. Update the version and commit files.
8. Keep the previous environment temporarily for rollback.

Example layout after an upgrade:

```text
/opt/automation/venvs/
├── kubespray-v2.31.0/
├── kubespray-vX.Y.Z/
└── kubespray-current -> kubespray-vX.Y.Z/
```

Do not track Kubespray `master`.

## Rollback

To switch back to the previous environment:

```bash
sudo ln -sfn \
  /opt/automation/venvs/kubespray-v2.31.0 \
  /opt/automation/venvs/kubespray-current
```

Verify:

```bash
readlink -f /opt/automation/venvs/kubespray-current

source /opt/automation/bin/activate-kubespray
ansible --version
```

The stable command links under `/opt/automation/bin` automatically follow the selected current environment.

## Rules

- Use one shared Ansible runtime.
- Do not install separate Ansible versions for `freb` and `gha-runner`.
- Keep the runtime root-owned.
- Activate the environment explicitly.
- Keep playbooks, inventory, variables, and workflows in Git.
- Do not use `/etc/ansible` for project files.
- Do not auto-activate the environment in `.bashrc`.
- Do not edit the pinned Kubespray source directly.
- Do not track Kubespray `master`.
- Keep `.ssh`, `.kube`, and other credentials user-specific.
- Let GitHub Actions check out the repository into its own workspace.
