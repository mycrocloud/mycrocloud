terraform {
  required_providers {
    neon = {
      source = "kislerdm/neon"
    }
  }
}


resource "neon_project" "this" {
  name = var.project_name
}

resource "neon_branch" "main" {
  project_id = neon_project.this.id
  name       = "main"
}

resource "neon_endpoint" "primary" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
}

resource "neon_role" "api_runtime" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = "api_runtime"
}

resource "neon_role" "migrator" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = "migrator"
}

resource "neon_role" "webapp_gateway" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = "webapp_gateway"
}

resource "neon_role" "monitoring_grafana" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = "grafana_reader"
}

# NOTE: Someday change owner from "nphamvn" to something more generic like "admin" or "terraform"
resource "neon_role" "nphamvn" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = "nphamvn"
}

resource "neon_database" "this" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = var.project_name
  owner_name = neon_role.nphamvn.name
}

# Import block was moved to root main.tf because it is only allowed in the root module.
