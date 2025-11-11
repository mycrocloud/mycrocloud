#!/bin/bash

RABBITMQ_USER="guest"
RABBITMQ_PASS="guest"
RABBITMQ_HOST="localhost"
RABBITMQ_PORT="15672"
EXCHANGE="amq.default"
ROUTING_KEY="job_queue"
MESSAGE_FILE="./messages/BuildMessage.json"

PAYLOAD=$(cat "$MESSAGE_FILE" | tr -d '\n' | sed 's/"/\\"/g')

curl -u "$RABBITMQ_USER:$RABBITMQ_PASS" \
  -H "content-type:application/json" \
  -X POST "http://$RABBITMQ_HOST:$RABBITMQ_PORT/api/exchanges/%2f/$EXCHANGE/publish" \
  -d "{
    \"properties\": {},
    \"routing_key\": \"$ROUTING_KEY\",
    \"payload\": \"$PAYLOAD\",
    \"payload_encoding\": \"string\"
  }"

echo -e "\n✅ Published message to queue '$ROUTING_KEY'"