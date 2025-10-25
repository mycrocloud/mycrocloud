# Creating infra with AWS and CloudFlare using Terraform
This folder contains Terraform configurations to create the necessary infrastructure on AWS and CloudFlare for deploying the application.

## Prerequisites
- Terraform installed on your local machine. You can download it from [here](https://www.terraform.io/downloads.html).
- An AWS account with appropriate permissions to create resources.
- A CloudFlare account with API access.
- AWS CLI configured with your credentials. You can follow the instructions [here](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-quickstart.html).
- CloudFlare API token with necessary permissions. You can create one by following the instructions [here](https://developers.cloudflare.com/api/tokens/create/).

## Configuration
1. Ensure that you are in the `infra` directory
2. Create a `backend.config` file to configure the remote state backend. You can use the provided `backend.config.example` as a template. Then, fill in the required values.
3. Create a `variables.auto.tfvars` file to define your variables. You can use the provided `variables.auto.tfvars.example` as a template. Then, fill in the required values.
4. Intialize terraform and apply the configuration:
   ```bash
   terraform init -backend-config=backend.config && \
   terraform apply
   ``` 