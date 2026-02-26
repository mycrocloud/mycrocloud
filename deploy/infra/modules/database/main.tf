terraform {
  required_providers {
    neon = {
      source = "kislerdm/neon"
    }
  }
}


resource "neon_project" "this" {
  name = var.project_name
  pg_version = "18"
  history_retention_seconds = 21600 # TODO: confirm this value
}

resource "neon_branch" "main" {
  project_id = neon_project.this.id
  name       = "production"
}

resource "neon_endpoint" "primary" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
}

resource "neon_role" "db_admin" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = "db_admin"
}

resource "neon_database" "this" {
  project_id = neon_project.this.id
  branch_id  = neon_branch.main.id
  name       = var.project_name
  owner_name = neon_role.db_admin.name
}
