- Install Ansible (run once)

```
python3 -m venv .venv && \
source .venv/bin/activate && \
pip install --upgrade pip && \
pip install ansible boto3 botocore
```

- Prepare
```
source .venv/bin/activate && \
export OBJC_DISABLE_INITIALIZE_FORK_SAFETY=YES && \
export SERVER_IP=$(cd ../infra && tf output -raw instance_ip)
```

- Update group_vars/\* files with your configuration

- Setup

```
ansible-playbook setup.yml
```

- Create AWS secrets so that below secret files are created

  - [ ] prod/mycrocloud/lb/certs/mycrocloud.info.pem
  - [ ] prod/mycrocloud/Services/WebApp/deployment/.env
  - [ ] prod/mycrocloud/.env
  - [ ] prod/mycrocloud/Services/WebApp/WebApp.Api/gha-mycrocloud.pem

- Sync

```
ansible-playbook sync.yml
```

- Deploy
  All (for first run)

```
ansible-playbook deploy.yml
```

Specific service

```
ansible-playbook deploy.yml -e "service=web"
```
