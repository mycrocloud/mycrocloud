output "instance_ip" {
  value       = aws_instance.server.public_ip
}

output "instance_user" {
  value       = "ubuntu"
}

output "host" {
  value = data.cloudflare_zone.zone.name
}

output "ssh_command" {
  value = "ssh ubuntu@${aws_instance.server.public_ip} -i <key>"
}
