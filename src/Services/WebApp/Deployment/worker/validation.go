package main

import (
	"fmt"
	"net/url"
	"path/filepath"
	"regexp"
	"strings"
)

// ValidationError contains details about validation failures
type ValidationError struct {
	Field   string
	Message string
}

func (e ValidationError) Error() string {
	return fmt.Sprintf("%s: %s", e.Field, e.Message)
}

// ValidationResult holds all validation errors
type ValidationResult struct {
	Errors []ValidationError
}

func (r *ValidationResult) AddError(field, message string) {
	r.Errors = append(r.Errors, ValidationError{Field: field, Message: message})
}

func (r *ValidationResult) IsValid() bool {
	return len(r.Errors) == 0
}

func (r *ValidationResult) Error() string {
	if r.IsValid() {
		return ""
	}
	var msgs []string
	for _, e := range r.Errors {
		msgs = append(msgs, e.Error())
	}
	return strings.Join(msgs, "; ")
}

var (
	// UUID format for build ID
	uuidRegex = regexp.MustCompile(`^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$`)

	// Safe characters for paths (alphanumeric, dash, underscore, dot, slash)
	safePathRegex = regexp.MustCompile(`^[a-zA-Z0-9_\-./]+$`)

	// Node version format (e.g., "18", "18.17", "18.17.0", "lts", "latest")
	nodeVersionRegex = regexp.MustCompile(`^(lts|latest|[0-9]+(\.[0-9]+){0,2})$`)

	// Safe env var key (alphanumeric and underscore, starting with letter or underscore)
	envKeyRegex = regexp.MustCompile(`^[a-zA-Z_][a-zA-Z0-9_]*$`)
)

// ValidateBuildMessage validates all fields of a BuildMessage
func ValidateBuildMessage(msg *BuildMessage, limits Limits) *ValidationResult {
	result := &ValidationResult{}

	// Required fields
	if msg.BuildId == "" {
		result.AddError("build_id", "required")
	} else if !uuidRegex.MatchString(msg.BuildId) {
		result.AddError("build_id", "must be a valid UUID")
	}

	if msg.CloneUrl == "" {
		result.AddError("clone_url", "required")
	} else if err := validateCloneURL(msg.CloneUrl); err != nil {
		result.AddError("clone_url", err.Error())
	}

	if msg.ArtifactsUploadUrl == "" {
		result.AddError("artifacts_upload_url", "required")
	} else if err := validateUploadURL(msg.ArtifactsUploadUrl); err != nil {
		result.AddError("artifacts_upload_url", err.Error())
	}

	// Directory validation
	if msg.Directory != "" {
		if len(msg.Directory) > limits.MaxDirectoryLen {
			result.AddError("directory", fmt.Sprintf("exceeds max length of %d", limits.MaxDirectoryLen))
		} else if err := validateSafePath(msg.Directory); err != nil {
			result.AddError("directory", err.Error())
		}
	}

	// OutDir validation
	if msg.OutDir != "" {
		if len(msg.OutDir) > limits.MaxDirectoryLen {
			result.AddError("out_dir", fmt.Sprintf("exceeds max length of %d", limits.MaxDirectoryLen))
		} else if err := validateSafePath(msg.OutDir); err != nil {
			result.AddError("out_dir", err.Error())
		}
	}

	// Command validation
	if msg.InstallCommand != "" && len(msg.InstallCommand) > limits.MaxCommandLen {
		result.AddError("install_command", fmt.Sprintf("exceeds max length of %d", limits.MaxCommandLen))
	}

	if msg.BuildCommand != "" && len(msg.BuildCommand) > limits.MaxCommandLen {
		result.AddError("build_command", fmt.Sprintf("exceeds max length of %d", limits.MaxCommandLen))
	}

	// Node version validation
	if msg.NodeVersion != "" {
		if len(msg.NodeVersion) > limits.MaxNodeVersionLen {
			result.AddError("node_version", fmt.Sprintf("exceeds max length of %d", limits.MaxNodeVersionLen))
		} else if !nodeVersionRegex.MatchString(msg.NodeVersion) {
			result.AddError("node_version", "invalid format")
		}
	}

	// Environment variables validation
	if len(msg.EnvVars) > limits.MaxEnvVars {
		result.AddError("env_vars", fmt.Sprintf("exceeds max count of %d", limits.MaxEnvVars))
	} else {
		for key, value := range msg.EnvVars {
			if len(key) > limits.MaxEnvKeyLen {
				result.AddError("env_vars", fmt.Sprintf("key '%s' exceeds max length of %d", key, limits.MaxEnvKeyLen))
				break
			}
			if !envKeyRegex.MatchString(key) {
				result.AddError("env_vars", fmt.Sprintf("key '%s' contains invalid characters", key))
				break
			}
			if len(value) > limits.MaxEnvValueLen {
				result.AddError("env_vars", fmt.Sprintf("value for '%s' exceeds max length of %d", key, limits.MaxEnvValueLen))
				break
			}
		}
	}

	return result
}

// validateCloneURL validates that the clone URL is a valid git URL
func validateCloneURL(cloneURL string) error {
	parsed, err := url.Parse(cloneURL)
	if err != nil {
		return fmt.Errorf("invalid URL format")
	}

	// Only allow https for security
	if parsed.Scheme != "https" {
		return fmt.Errorf("only HTTPS URLs are allowed")
	}

	// Check for allowed hosts (GitHub, GitLab, Bitbucket)
	allowedHosts := []string{
		"github.com",
		"gitlab.com",
		"bitbucket.org",
	}

	host := strings.ToLower(parsed.Host)
	isAllowed := false
	for _, allowed := range allowedHosts {
		if host == allowed || strings.HasSuffix(host, "."+allowed) {
			isAllowed = true
			break
		}
	}

	if !isAllowed {
		return fmt.Errorf("host '%s' is not allowed", parsed.Host)
	}

	return nil
}

// validateUploadURL validates the artifacts upload URL
func validateUploadURL(uploadURL string) error {
	parsed, err := url.Parse(uploadURL)
	if err != nil {
		return fmt.Errorf("invalid URL format")
	}

	if parsed.Scheme != "https" && parsed.Scheme != "http" {
		return fmt.Errorf("invalid scheme")
	}

	return nil
}

// validateSafePath checks that a path doesn't contain path traversal attacks
func validateSafePath(path string) error {
	// Clean the path
	cleaned := filepath.Clean(path)

	// Check for path traversal
	if strings.Contains(cleaned, "..") {
		return fmt.Errorf("path traversal not allowed")
	}

	// Check for absolute paths
	if filepath.IsAbs(cleaned) {
		return fmt.Errorf("absolute paths not allowed")
	}

	// Check for safe characters only
	if !safePathRegex.MatchString(cleaned) {
		return fmt.Errorf("contains invalid characters")
	}

	return nil
}
