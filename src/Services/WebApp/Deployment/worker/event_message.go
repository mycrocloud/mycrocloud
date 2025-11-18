package main

type BuildMessage struct {
	JobId              string `json:"job_id"`
	RepoFullName       string `json:"repo_full_name"`
	CloneUrl           string `json:"clone_url"`
	Directory          string `json:"directory"`
	OutDir             string `json:"out_dir"`
	InstallCommand     string `json:"install_command"`
	BuildCommand       string `json:"build_command"`
	ArtifactsUploadUrl string `json:"artifacts_upload_url"`
}
type JobStatus int

const (
	Started JobStatus = iota
	Done
	Failed
)

type JobStatusChangedEventMessage struct {
	JobId       string    `json:"job_id"`
	Status      JobStatus `json:"status"`
	ContainerId string    `json:"container_id,omitempty"`
}
