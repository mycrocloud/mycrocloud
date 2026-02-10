package main

// PlanLimits contains resource limits based on account plan
type PlanLimits struct {
	MemoryMB       int `json:"memory_mb"`        // Container memory limit in MB
	CPUPercent     int `json:"cpu_percent"`      // CPU limit as percentage (100 = 1 core)
	BuildTimeoutS  int `json:"build_timeout_s"`  // Build timeout in seconds
	ArtifactSizeMB int `json:"artifact_size_mb"` // Max artifact size in MB
}

type BuildMessage struct {
	BuildId            string            `json:"build_id"`
	RepoFullName       string            `json:"repo_full_name"`
	CloneUrl           string            `json:"clone_url"`
	Directory          string            `json:"directory"`
	OutDir             string            `json:"out_dir"`
	InstallCommand     string            `json:"install_command"`
	BuildCommand       string            `json:"build_command"`
	NodeVersion        string            `json:"node_version"`
	EnvVars            map[string]string `json:"env_vars"`
	ArtifactsUploadPath string            `json:"artifacts_upload_path"`
	Limits             *PlanLimits       `json:"limits,omitempty"`
}
type BuildStatus int

const (
	Started BuildStatus = iota
	Done
	Failed
)

type BuildStatusChangedEventMessage struct {
	BuildId     string      `json:"build_id"`
	Status      BuildStatus `json:"status"`
	ContainerId string      `json:"container_id,omitempty"`
	ArtifactId  string      `json:"artifact_id,omitempty"`
}
