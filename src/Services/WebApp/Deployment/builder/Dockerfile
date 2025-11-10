FROM node:20-alpine

RUN apk add --no-cache git bash curl

WORKDIR /repo

COPY build.sh /usr/local/bin/build.sh
RUN chmod +x /usr/local/bin/build.sh

ENV REPO_URL="" \
    WORK_DIR="." \
    OUT_DIR="dist" \
    INSTALL_CMD="npm ci" \
    BUILD_CMD="npm run build"

ENTRYPOINT ["/usr/local/bin/build.sh"]
