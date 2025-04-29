FROM fluent/fluentd:v1.12.0-debian-1.0

USER root

RUN gem install \
    excon -v 0.109.0 \
    elasticsearch -v 7.10.1 \
    fluent-plugin-elasticsearch -v 5.0.3 \
    --no-document && \
    gem uninstall -I elasticsearch -v '>= 0' -v '!=7.10.1'

USER fluent