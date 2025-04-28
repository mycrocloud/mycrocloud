FROM fluent/fluentd:edge-debian
USER root
RUN gem uninstall -I elasticsearch && \
    gem install elasticsearch -v 7.10.1 --no-document && \
    gem install fluent-plugin-elasticsearch --version 5.4.3 --no-document

USER fluent