FROM node:20-alpine

RUN apk add --no-cache git bash curl jq

# Install nvm for node version management
ENV NVM_DIR="/root/.nvm"
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash

WORKDIR /repo

COPY build.sh /usr/local/bin/build.sh
RUN chmod +x /usr/local/bin/build.sh

ENV REPO_URL="" \
    WORK_DIR="." \
    OUT_DIR="dist" \
    INSTALL_CMD="npm ci" \
    BUILD_CMD="npm run build" \
    NODE_VERSION="" \
    ENV_VARS=""

ENTRYPOINT ["/usr/local/bin/build.sh"]
