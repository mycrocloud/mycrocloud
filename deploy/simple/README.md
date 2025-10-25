- Create infra
- Set up environment variables
```
export DEPLOY_USER="ubuntu"
export DEPLOY_IP="192.168.1.100"
export DEPLOY_KEY="~/.ssh/my_key"
```
- Make script executable
```
chmod +x deploy.sh
```

- Deploy all services
```bash
./deploy.sh
```
- Deploy specific service
```bash
./deploy.sh service_name
```