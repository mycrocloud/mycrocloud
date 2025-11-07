#!/bin/sh
# https://docs.docker.com/engine/install/ubuntu/
# Add Docker's official GPG key:
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

# Add the repository to Apt sources:
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# https://docs.docker.com/engine/install/linux-postinstall/
sudo usermod -aG docker ubuntu  # Use explicit username
sudo systemctl enable docker    # Ensure Docker starts on boot
sudo systemctl start docker     # Start Docker immediately

# Deployment prerequisites. TODO: fix this?
sudo mkdir -p /var/run/fluentd /tmp/build-outputs && sudo chmod 0777 /var/run/fluentd /tmp/build-outputs