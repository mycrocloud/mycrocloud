Deploy
```bash
export $(cat .env | xargs) 
helm install mycrocloud \
--set api.secretEnv.ConnectionStrings__DefaultConnection=$ConnectionStrings__DefaultConnection \
--set api.secretEnv.ConnectionStrings__RabbitMq=$ConnectionStrings__RabbitMq \
.
```