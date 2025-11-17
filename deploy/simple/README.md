- Create infra

- Setup
```
ansible-playbook -i ansible/inventory.ini ansible/setup.yml
```

- Create AWS secrets so that below secret files are created
    - [ ] prod/mycrocloud/lb/certs/mycrocloud.info.pem
    - [ ] prod/mycrocloud/Services/WebApp/deployment/.env
    - [ ] prod/mycrocloud/.env
    - [ ] prod/mycrocloud/Services/WebApp/WebApp.Api/gha-mycrocloud.pem

```
export OBJC_DISABLE_INITIALIZE_FORK_SAFETY=YES
```
- Setup
```
ansible-playbook -i ansible/inventory.ini ansible/sync.yml
```
- Deploy
All (for first run)
```
ansible-playbook -i ansible/inventory.ini ansible/deploy.yml
```
Specific service
```
ansible-playbook -i ansible/inventory.ini ansible/deploy.yml -e "service=web"
```