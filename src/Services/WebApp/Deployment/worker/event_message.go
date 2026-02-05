package main

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
	ArtifactsUploadUrl string            `json:"artifacts_upload_url"`
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
}
