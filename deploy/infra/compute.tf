data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"] # Canonical

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-*-24.04-amd64-server-*"]
  }
}

resource "aws_instance" "server" {
  ami           = data.aws_ami.ubuntu.id
  instance_type = "t3.small"

  root_block_device {
    volume_size = 20
  }

  iam_instance_profile        = aws_iam_instance_profile.server.name
  associate_public_ip_address = true
  subnet_id                   = aws_subnet.subnet.id
  vpc_security_group_ids      = [aws_security_group.sg.id]

  lifecycle {
    ignore_changes = [ami]
  }

  tags = {
    Name = "${local.project_name}-server"
  }
}
