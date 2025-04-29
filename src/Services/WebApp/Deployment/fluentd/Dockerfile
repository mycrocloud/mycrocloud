FROM fluent/fluentd:v1.12.0-debian-1.0

USER root

RUN gem install \
    excon:0.109.0 \
    elasticsearch:7.10.1 \
    fluent-plugin-elasticsearch:5.0.3 \
    --no-document

USER fluent