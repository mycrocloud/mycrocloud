FROM fluent/fluentd:v1.16-debian

USER root

RUN gem install \
    excon:0.109.0 \
    elasticsearch:7.10.1 \
    fluent-plugin-elasticsearch:5.0.3 \
    --no-document

# TODO: use published gem when available
COPY fluent-plugin-rabbitmq-0.1.6.gem /tmp/
RUN fluent-gem install /tmp/fluent-plugin-rabbitmq-0.1.6.gem --no-document

USER fluent