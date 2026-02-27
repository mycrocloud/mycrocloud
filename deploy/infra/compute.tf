resource "aws_key_pair" "ssh_key" {
  key_name   = "${local.project_name}-key"
  public_key = var.public_key

  tags = {
    Name = "${local.project_name}-key"
  }
}

resource "aws_instance" "server" {
  ami           = "ami-025ece6a3a0e7558f"
  instance_type = "t3.small"

  root_block_device {
    volume_size = 20
  }

  key_name                    = aws_key_pair.ssh_key.key_name
  associate_public_ip_address = true
  subnet_id                   = aws_subnet.subnet.id
  vpc_security_group_ids      = [aws_security_group.sg.id]

  tags = {
    Name = "${local.project_name}-server"
  }
}
