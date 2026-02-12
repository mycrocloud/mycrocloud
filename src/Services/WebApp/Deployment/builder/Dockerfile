ARG NODE_VERSION=20
FROM node:${NODE_VERSION}-alpine

RUN apk add --no-cache git bash curl jq zip

# Create non-root user for security (UID 1001 to avoid conflict with existing node user)
RUN addgroup -g 1001 builder && \
    adduser -u 1001 -G builder -s /bin/bash -D builder

# Create directories with proper permissions
RUN mkdir -p /repo /output && \
    chown -R builder:builder /repo /output

WORKDIR /repo

COPY build.sh /usr/local/bin/build.sh
RUN chmod +x /usr/local/bin/build.sh

ENV REPO_URL="" \
    WORK_DIR="." \
    OUT_DIR="dist" \
    INSTALL_CMD="npm ci" \
    BUILD_CMD="npm run build" \
    ENV_VARS=""

USER builder

ENTRYPOINT ["/usr/local/bin/build.sh"]
