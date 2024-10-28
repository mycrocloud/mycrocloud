FROM node:20-alpine

RUN apk update && apk add --no-cache git

RUN mkdir -p /repo /output

WORKDIR /repo

COPY build.sh /build.sh

RUN chmod +x /build.sh

CMD ["/build.sh"]
