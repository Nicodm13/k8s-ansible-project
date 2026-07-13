# GitHub Actions Self-Hosted Runner

This document describes the GitHub Actions self-hosted runner installed on the control node for the `k8s-ansible-project` repository.

## Repository

```text
https://github.com/Nicodm13/k8s-ansible-project
```

## Runner Identity

| Setting | Value |
|---|---|
| Linux user | `gha-runner` |
| Runner name | `ControlNode` |
| Runner group | `Default` |
| Operating system label | `linux` |
| Architecture label | `x64` |
| Custom labels | `ansible`, `kubespray` |
| Installation directory | `/home/gha-runner/actions-runner` |
| Work directory | `/home/gha-runner/actions-runner/_work` |

The runner is registered at repository scope.

## Runner User

The dedicated runner account is:

```text
gha-runner
```

The account is used only for GitHub Actions jobs and related credentials.

Do not run the GitHub Actions service as `freb` or `root`.

Inspect the account:

```bash
id gha-runner
sudo -iu gha-runner groups
```

The user should belong to the shared automation group:

```text
automation
```

## Directory Structure

```text
/home/gha-runner/
├── actions-runner/
│   ├── bin/
│   ├── externals/
│   ├── _diag/
│   ├── _work/
│   ├── config.sh
│   ├── run.sh
│   └── svc.sh
├── infrastructure/
├── secrets/
├── .ssh/
└── .kube/
```

The directory:

```text
/home/gha-runner/infrastructure
```

is not used as the workflow checkout location.

GitHub Actions checks out repositories into:

```text
/home/gha-runner/actions-runner/_work/
```

A typical job workspace is:

```text
/home/gha-runner/actions-runner/_work/k8s-ansible-project/k8s-ansible-project
```

## Systemd Service

The registered systemd service is:

```text
actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Check status:

```bash
sudo systemctl status   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

The runner helper script can also be used:

```bash
cd /home/gha-runner/actions-runner

sudo ./svc.sh status
```

## Service Management

Start the runner:

```bash
sudo systemctl start   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Stop the runner:

```bash
sudo systemctl stop   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Restart the runner:

```bash
sudo systemctl restart   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Enable automatic startup:

```bash
sudo systemctl enable   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Check whether it is enabled:

```bash
sudo systemctl is-enabled   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

## Verify the Running Process

```bash
ps -ef | grep '[R]unner.Listener'
```

Expected process owner:

```text
gha-runner
```

Expected command includes:

```text
Runner.Listener run --startuptype service
```

The username may appear truncated as:

```text
gha-run+
```

in narrow terminal output.

Use this command to display the full process information:

```bash
ps -efww | grep '[R]unner.Listener'
```

## GitHub Runner Status

In GitHub:

```text
Repository
→ Settings
→ Actions
→ Runners
```

The runner should be listed as:

```text
ControlNode
```

Expected status when no workflow is running:

```text
Idle
```

Expected labels:

```text
self-hosted
linux
x64
ansible
kubespray
```

A workflow job using all of these labels can run only on a runner that matches every label.

## Shared Ansible and Kubespray Runtime

The runner uses the shared root-managed runtime:

```text
/opt/automation
```

The environment must be activated explicitly in every workflow step that runs Ansible or Kubespray:

```bash
source /opt/automation/bin/activate-kubespray
```

Expected Ansible executable:

```text
/opt/automation/bin/ansible
```

Verify as the runner user:

```bash
sudo -iu gha-runner bash <<'EOF'
set -e

source /opt/automation/bin/activate-kubespray

whoami
command -v ansible
ansible --version
EOF
```

Expected:

```text
gha-runner
/opt/automation/bin/ansible
```

Do not install a separate Ansible environment for `gha-runner`.

## Workflow Runner Selection

A workflow targeting this runner should use:

```yaml
runs-on:
  - self-hosted
  - linux
  - x64
  - ansible
```

The `kubespray` label can be added for jobs that specifically deploy or maintain the Kubernetes cluster:

```yaml
runs-on:
  - self-hosted
  - linux
  - x64
  - ansible
  - kubespray
```

## Validation Workflow

The validation workflow belongs at:

```text
.github/workflows/ansible-validate.yml
```

Example:

```yaml
name: Validate Ansible

on:
  workflow_dispatch:
  pull_request:
    paths:
      - "ansible/**"
      - "ansible.cfg"
      - ".github/workflows/ansible-validate.yml"
  push:
    branches:
      - main
    paths:
      - "ansible/**"
      - "ansible.cfg"
      - ".github/workflows/ansible-validate.yml"

permissions:
  contents: read

jobs:
  validate:
    name: Validate infrastructure
    runs-on:
      - self-hosted
      - linux
      - x64
      - ansible

    steps:
      - name: Checkout repository
        uses: actions/checkout@v7

      - name: Verify execution context
        shell: bash
        run: |
          set -euo pipefail

          echo "User: $(whoami)"
          echo "Host: $(hostname)"
          echo "Workspace: ${GITHUB_WORKSPACE}"

      - name: Verify Ansible environment
        shell: bash
        run: |
          set -euo pipefail

          source /opt/automation/bin/activate-kubespray

          command -v ansible
          ansible --version
          ansible-config dump --only-changed

      - name: Validate inventory
        shell: bash
        run: |
          set -euo pipefail

          source /opt/automation/bin/activate-kubespray

          ansible-inventory --graph

      - name: Check playbook syntax
        shell: bash
        run: |
          set -euo pipefail

          source /opt/automation/bin/activate-kubespray

          ansible-playbook             ansible/playbooks/setup.yml             --syntax-check
```

## Repository Checkout

The workflow action:

```yaml
uses: actions/checkout@v7
```

checks out the repository into the job workspace.

Do not manually clone or update the repository in:

```text
/home/gha-runner/infrastructure
```

for normal workflow execution.

Each job should use:

```text
${GITHUB_WORKSPACE}
```

as its repository root.

## Credentials

Runner credentials remain user-specific.

SSH credentials:

```text
/home/gha-runner/.ssh/
```

Kubernetes credentials:

```text
/home/gha-runner/.kube/
```

Other protected runner secrets:

```text
/home/gha-runner/secrets/
```

Set strict permissions:

```bash
sudo chown -R gha-runner:gha-runner   /home/gha-runner/.ssh   /home/gha-runner/.kube   /home/gha-runner/secrets

sudo chmod 700   /home/gha-runner/.ssh   /home/gha-runner/.kube   /home/gha-runner/secrets
```

Private SSH keys should normally use:

```bash
sudo chmod 600 /home/gha-runner/.ssh/PRIVATE_KEY
```

Kubeconfig should normally use:

```bash
sudo chmod 600 /home/gha-runner/.kube/config
```

Do not copy `freb`'s GitHub SSH key to `gha-runner`.

The GitHub Actions runner does not need the repository owner's GitHub SSH key for `actions/checkout`.

## Managed-Node SSH Access

SSH keys under:

```text
/home/gha-runner/.ssh/
```

should be limited to access from the runner to managed infrastructure nodes.

Example SSH configuration:

```sshconfig
Host node1
    HostName 192.0.2.10
    User ansible
    IdentityFile ~/.ssh/node1
    IdentitiesOnly yes
```

Replace documentation addresses and values with the actual nodes.

## Sudo Access

Do not give `gha-runner` unrestricted passwordless root access unless the project explicitly requires it.

The runner normally requires:

- Read and execute access to `/opt/automation`
- SSH access to managed nodes
- Access to its own `.ssh`, `.kube`, and `secrets` directories
- No write access to the shared Ansible runtime

Verify runtime protection:

```bash
sudo -iu gha-runner test ! -w /opt/automation/venvs/kubespray-current &&
  echo "gha-runner cannot modify runtime"
```

## Logs

Systemd logs:

```bash
sudo journalctl   -u actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service   --no-pager   -n 100
```

Follow live logs:

```bash
sudo journalctl   -u actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service   -f
```

Runner diagnostic logs:

```text
/home/gha-runner/actions-runner/_diag/
```

List recent diagnostic logs:

```bash
sudo -iu gha-runner   ls -lt /home/gha-runner/actions-runner/_diag
```

Workflow logs are also available in GitHub:

```text
Repository
→ Actions
→ Select workflow run
→ Select job
```

## Troubleshooting

### Runner is offline

Check the service:

```bash
sudo systemctl status   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Restart it:

```bash
sudo systemctl restart   actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service
```

Read the logs:

```bash
sudo journalctl   -u actions.runner.Nicodm13-k8s-ansible-project.ControlNode.service   --no-pager   -n 200
```

### Workflow remains queued

Verify that the workflow labels match the runner labels.

Workflow:

```yaml
runs-on:
  - self-hosted
  - linux
  - x64
  - ansible
```

Runner labels must include all four values.

Check the labels in GitHub:

```text
Repository
→ Settings
→ Actions
→ Runners
→ ControlNode
```

### Ansible command not found

The workflow step did not activate the shared runtime.

Add:

```bash
source /opt/automation/bin/activate-kubespray
```

before running Ansible commands.

### Wrong Ansible configuration

Run the command from the repository workspace and verify:

```bash
ansible --version
```

Expected configuration path during a workflow:

```text
<GITHUB_WORKSPACE>/ansible.cfg
```

Inspect effective settings:

```bash
ansible-config dump --only-changed
```

### Empty inventory warning

This warning is expected while no managed nodes are defined:

```text
provided hosts list is empty
```

Add real hosts to:

```text
ansible/inventory/hosts.ini
```

### Runner process is owned by the wrong user

Check:

```bash
ps -efww | grep '[R]unner.Listener'
```

If the service was installed incorrectly, stop and reinstall it using:

```bash
cd /home/gha-runner/actions-runner

sudo ./svc.sh stop
sudo ./svc.sh uninstall
sudo ./svc.sh install gha-runner
sudo ./svc.sh start
```

## Preventing Package Maintenance Interruptions

On Debian-based systems, configure `needrestart` not to restart the GitHub Actions runner during package maintenance:

```bash
echo '$nrconf{override_rc}{qr(^actions\.runner\..+\.service$)} = 0;' |
  sudo tee /etc/needrestart/conf.d/actions_runner_services.conf
```

Verify:

```bash
cat /etc/needrestart/conf.d/actions_runner_services.conf
```

## Updating the Runner

The runner normally supports automatic updates when GitHub requires a newer compatible version.

Before manual maintenance:

```bash
cd /home/gha-runner/actions-runner

sudo ./svc.sh stop
```

Check the installed version:

```bash
sudo -iu gha-runner   /home/gha-runner/actions-runner/bin/Runner.Listener --version
```

Use the current runner download and checksum commands shown by GitHub under:

```text
Repository
→ Settings
→ Actions
→ Runners
→ New self-hosted runner
```

Do not overwrite an active installation without first stopping the service and backing up the runner configuration files.

## Backup

Back up the runner registration and service-related files:

```bash
sudo tar -czf   /root/actions-runner-registration-backup.tar.gz   -C /home/gha-runner/actions-runner   .runner   .credentials   .credentials_rsaparams   .service
```

These files contain sensitive registration information.

Store the backup securely and do not commit it to Git.

Do not back up the `_work` directory as a source of truth. Workflow workspaces are temporary and can be recreated.

## Removing the Runner

Remove it from the system service first:

```bash
cd /home/gha-runner/actions-runner

sudo ./svc.sh stop
sudo ./svc.sh uninstall
```

Then remove the GitHub registration.

In GitHub:

```text
Repository
→ Settings
→ Actions
→ Runners
→ ControlNode
→ Remove
```

GitHub provides a removal token and command.

Run the exact command shown by GitHub as `gha-runner`:

```bash
sudo -iu gha-runner
cd ~/actions-runner

./config.sh remove --token TEMPORARY_REMOVAL_TOKEN
```

Do not delete the runner directory before removing the registration.

## Re-registering the Runner

Open:

```text
Repository
→ Settings
→ Actions
→ Runners
→ New self-hosted runner
```

Select:

```text
Linux
x64
```

Run the generated registration command as `gha-runner`.

Use:

```text
Runner group: Default
Runner name: ControlNode
Additional labels: ansible,kubespray
Work folder: _work
```

Install the service again:

```bash
cd /home/gha-runner/actions-runner

sudo ./svc.sh install gha-runner
sudo ./svc.sh start
sudo ./svc.sh status
```

## Security Rules

- Keep the repository private.
- Do not execute untrusted pull-request code on the self-hosted runner.
- Do not run the service as root.
- Do not share SSH private keys between `freb` and `gha-runner`.
- Do not store credentials in the Git repository.
- Do not give the runner write access to `/opt/automation`.
- Use minimal workflow permissions.
- Pin or review third-party GitHub Actions before use.
- Keep credentials under `/home/gha-runner`.
- Treat workflow code as executable code on the control node.
- Remove runner access when the project ends.
