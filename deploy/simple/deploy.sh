#!/bin/bash
USER=ubuntu
IP=
KEY_PATH=

# Copy application files to remote server
scp -r -i $KEY_PATH -o StrictHostKeychecking=no app $USER@$IP:app

# Connect to remote server and run deployment commands
ssh $USER@$IP -i $KEY_PATH -o StrictHostKeychecking=no << EOF
  cd app && docker compose up -d
EOF