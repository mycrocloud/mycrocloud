- Create infra
- Set up environment variables
```
export DEPLOY_USER="ubuntu"
export DEPLOY_IP="192.168.1.100"
export DEPLOY_KEY="~/.ssh/my_key"
```
- Sync files to server
```
chmod +x ./scripts/sync_files.sh && \
./scripts/sync_files.sh
```

- Make script executable
```
chmod +x ./scripts/deploy.sh
```

- Deploy all services
```bash
./scripts/deploy.sh
```
- Deploy specific service
```bash
./scripts/deploy.sh service_name
```