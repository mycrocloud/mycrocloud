# ref: https://docs.fluentd.org/container-deployment/docker-compose#step-1-create-fluentd-image-with-your-config--plugin

FROM fluent/fluentd:edge-debian
USER root
RUN ["gem", "install", "fluent-plugin-elasticsearch", "--no-document", "--version", "5.4.3"]
USER fluent