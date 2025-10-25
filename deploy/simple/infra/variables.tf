variable "project_name" {
  type        = string
  description = "Project name used for resource naming"
  default     = "mycrocloud"
}

variable "availability_zone" {
  type        = string
  description = "Availability zone for subnet"
  default     = "ap-northeast-1a"
}

variable "vpc_cidr" {
  type        = string
  description = "CIDR block for VPC"
  default     = "10.0.0.0/16"
}

variable "subnet_cidr" {
  type        = string
  description = "CIDR block for subnet"
  default     = "10.0.1.0/24"
}

variable "instance_type" {
  type        = string
  description = "EC2 instance type"
  default     = "t3.micro"
}
