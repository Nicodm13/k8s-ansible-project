# GitHub SSH Access for Project Collaborators

This document explains how project collaborators configure GitHub access through SSH.

Each collaborator must use their own GitHub account and their own SSH key.

SSH private keys must never be shared between users or devices.

## Repository

```text
git@github.com:Nicodm13/k8s-ansible-project.git
```

GitHub repository:

```text
https://github.com/Nicodm13/k8s-ansible-project
```

## Repository Owner: Add the Collaborator

The repository owner must invite the collaborator before they can push to a private repository.

In GitHub:

```text
Repository
→ Settings
→ Collaborators
→ Add people
```

Search for the collaborator's GitHub username and send the invitation.

The collaborator must accept the invitation before cloning or pushing.

## Collaborator: Generate an SSH Key

Run this on the collaborator's own computer:

```bash
ssh-keygen   -t ed25519   -C "YOUR_EMAIL_OR_DEVICE_DESCRIPTION"   -f ~/.ssh/id_ed25519_github
```

Example:

```bash
ssh-keygen   -t ed25519   -C "student-laptop GitHub"   -f ~/.ssh/id_ed25519_github
```

The command creates:

```text
~/.ssh/id_ed25519_github
~/.ssh/id_ed25519_github.pub
```

The file without `.pub` is the private key.

Do not share it.

## Add the Public Key to GitHub

Display the public key:

```bash
cat ~/.ssh/id_ed25519_github.pub
```

Copy the entire line beginning with:

```text
ssh-ed25519
```

In GitHub:

```text
Profile picture
→ Settings
→ SSH and GPG keys
→ New SSH key
```

Use:

```text
Title: A descriptive device name
Key type: Authentication Key
Key: Paste the public key
```

Only upload:

```text
~/.ssh/id_ed25519_github.pub
```

Never upload:

```text
~/.ssh/id_ed25519_github
```

## Configure SSH

Create or update:

```text
~/.ssh/config
```

Use:

```sshconfig
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_github
    IdentitiesOnly yes
```

Set permissions:

```bash
chmod 700 ~/.ssh
chmod 600 ~/.ssh/config
chmod 600 ~/.ssh/id_ed25519_github
chmod 644 ~/.ssh/id_ed25519_github.pub
```

## Test GitHub Authentication

Run:

```bash
ssh -T git@github.com
```

Expected result:

```text
Hi USERNAME! You've successfully authenticated, but GitHub does not provide shell access.
```

The message confirms that SSH authentication works.

## Clone the Repository

Run:

```bash
git clone git@github.com:Nicodm13/k8s-ansible-project.git
```

Enter the repository:

```bash
cd k8s-ansible-project
```

Verify the remote:

```bash
git remote -v
```

Expected:

```text
origin  git@github.com:Nicodm13/k8s-ansible-project.git (fetch)
origin  git@github.com:Nicodm13/k8s-ansible-project.git (push)
```

## Configure Git Identity

Set the collaborator's own Git identity:

```bash
git config --global user.name "FULL NAME"
git config --global user.email "GITHUB EMAIL"
```

Verify:

```bash
git config --global user.name
git config --global user.email
```

A GitHub noreply address may be used instead of a personal email.

## Normal Collaboration Workflow

Fetch the latest changes:

```bash
git pull
```

Create a branch:

```bash
git switch -c feature/example-change
```

Stage changes:

```bash
git add .
```

Commit:

```bash
git commit -m "Describe the change"
```

Push the branch:

```bash
git push -u origin feature/example-change
```

Create a pull request on GitHub instead of pushing directly to `main`.

## Using Multiple Devices

A separate SSH key should normally be created for each device.

Example:

```text
Desktop computer → one SSH key
Laptop → another SSH key
Control node → another SSH key
```

Each public key is added separately to the collaborator's GitHub account.

## Using HTTPS Instead

SSH is not mandatory.

A collaborator can clone through HTTPS:

```bash
git clone https://github.com/Nicodm13/k8s-ansible-project.git
```

GitHub account passwords cannot be used for Git authentication over HTTPS.

Use one of:

- GitHub CLI
- Git Credential Manager
- Personal access token

Do not store personal access tokens in the repository.

## Security Rules

- Each collaborator uses their own GitHub account.
- Each collaborator uses their own SSH key.
- Do not share SSH private keys.
- Do not copy another user's `.ssh` directory.
- Do not commit private keys, tokens, passwords, or kubeconfig files.
- Remove obsolete SSH keys from GitHub when a device is retired or lost.
- Remove repository access when a collaborator no longer needs it.
- Use branches and pull requests for changes.
