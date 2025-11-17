- Install Ansible
```
python3 -m venv .venv && \
source .venv/bin/activate && \
pip install --upgrade pip && \
pip install ansible boto3 botocore
```

- (macOS only) Set environment variable to avoid fork safety issues
```
export OBJC_DISABLE_INITIALIZE_FORK_SAFETY=YES
```

- Setup
```
ansible-playbook -i inventory.ini setup.yml
```

- Create AWS secrets so that below secret files are created
    - [ ] prod/mycrocloud/lb/certs/mycrocloud.info.pem
    - [ ] prod/mycrocloud/Services/WebApp/deployment/.env
    - [ ] prod/mycrocloud/.env
    - [ ] prod/mycrocloud/Services/WebApp/WebApp.Api/gha-mycrocloud.pem

- Sync
```
ansible-playbook -i inventory.ini sync.yml
```
- Deploy
All (for first run)
```
ansible-playbook -i inventory.ini deploy.yml
```
Specific service
```
ansible-playbook -i inventory.ini deploy.yml -e "service=web"
```