export $(cat .env | xargs) 
helm upgrade --install mycrocloud \
--set api.secretEnv.ConnectionStrings__DefaultConnection=$ConnectionStrings__DefaultConnection \
--set api.secretEnv.ConnectionStrings__RabbitMq=$ConnectionStrings__RabbitMq \
--set api.secretEnv.OAuthApps__GitHub__ClientSecret=$OAuthApps__GitHub__ClientSecret \
--set api.secretEnv.AppIntegrations__GitHubWebhook__Config__Secret=$AppIntegrations__GitHubWebhook__Config__Secret \
--set api.secretEnv.Elasticsearch__Password=$Elasticsearch__Password \
--set api.secretEnv.ConnectionStrings__Redis=$ConnectionStrings__Redis \
--set gateway.secretEnv.ConnectionStrings__DefaultConnection=$ConnectionStrings__DefaultConnection \
--set gateway.secretEnv.ConnectionStrings__Redis=$ConnectionStrings__Redis \
.