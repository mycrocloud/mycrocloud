resource "aws_vpc" "vpc" {
  cidr_block = "10.0.0.0/16"
}

resource "aws_subnet" "az1" {
  vpc_id = aws_vpc.vpc.id
  availability_zone = "ap-northeast-1a"
  cidr_block = "10.0.0.0/20"
}

resource "aws_subnet" "az2" {
  vpc_id = aws_vpc.vpc.id
  availability_zone = "ap-northeast-1c"
  cidr_block = "10.0.16.0/20"
}

resource "aws_subnet" "az3" {
  vpc_id = aws_vpc.vpc.id
  availability_zone = "ap-northeast-1d"
  cidr_block = "10.0.32.0/20"
}