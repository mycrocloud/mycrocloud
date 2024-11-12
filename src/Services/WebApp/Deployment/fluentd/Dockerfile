FROM fluent/fluentd:v1.12.0-debian-1.0
USER root
RUN gem install excon -v 0.109.0 --no-document
RUN gem uninstall -I elasticsearch && gem install elasticsearch -v 7.10.1
RUN ["gem", "install", "fluent-plugin-elasticsearch", "--no-document", "--version", "5.0.3"]
USER fluent