package main

import (
	"fmt"
	"log"
	"os"
	"path/filepath"

	"github.com/docker/docker/api/types/container"
)

// GetSecureHostConfig returns a HostConfig with security constraints applied
func GetSecureHostConfig(jobLimits JobLimits) *container.HostConfig {
	pidsLimit := jobLimits.PidsLimit
	return &container.HostConfig{
		Resources: container.Resources{
			// Memory limits
			Memory:            jobLimits.MemoryBytes,
			MemoryReservation: jobLimits.MemorySoftBytes,

			// CPU limits
			CPUQuota:  jobLimits.CPUQuota,
			CPUPeriod: jobLimits.CPUPeriod,

			// PID limit to prevent fork bombs
			PidsLimit: &pidsLimit,
		},

		// Security options
		SecurityOpt: []string{
			"no-new-privileges:true", // Prevent privilege escalation
		},

		// Drop all capabilities - builder runs as non-root user
		CapDrop: []string{"ALL"},

		// Network mode - use default bridge
		NetworkMode: "bridge",

		// Disable privileged mode (already default, but explicit)
		Privileged: false,
	}
}

// ArtifactSizeCheck contains the result of an artifact size check
type ArtifactSizeCheck struct {
	Size        int64
	ExceedsHard bool
	ExceedsSoft bool
	Message     string
}

// CheckArtifactSize checks the size of the artifact zip file against limits
func CheckArtifactSize(zipPath string, jobLimits JobLimits) (*ArtifactSizeCheck, error) {
	info, err := os.Stat(zipPath)
	if err != nil {
		return nil, fmt.Errorf("stat artifact: %w", err)
	}

	size := info.Size()
	check := &ArtifactSizeCheck{Size: size}

	if size > jobLimits.MaxArtifactSize {
		check.ExceedsHard = true
		check.Message = fmt.Sprintf("artifact size (%s) exceeds maximum allowed (%s)",
			formatBytes(size), formatBytes(jobLimits.MaxArtifactSize))
	} else if size > jobLimits.WarnArtifactSize {
		check.ExceedsSoft = true
		check.Message = fmt.Sprintf("artifact size (%s) exceeds soft limit (%s)",
			formatBytes(size), formatBytes(jobLimits.WarnArtifactSize))
	}

	return check, nil
}

// CheckOutputDirectory checks for suspicious files in the output directory
func CheckOutputDirectory(outputDir string) error {
	suspicious := []string{
		".git",
		".env",
		".env.local",
		".env.production",
		"node_modules",
		".ssh",
		"id_rsa",
		"id_ed25519",
		".npmrc",
		".yarnrc",
	}

	for _, name := range suspicious {
		path := filepath.Join(outputDir, name)
		if _, err := os.Stat(path); err == nil {
			log.Printf("Warning: suspicious file/directory in output: %s", name)
		}
	}

	return nil
}

// formatBytes formats bytes to human readable string
func formatBytes(bytes int64) string {
	const (
		_KB = 1024
		_MB = _KB * 1024
		_GB = _MB * 1024
	)

	switch {
	case bytes >= _GB:
		return fmt.Sprintf("%.2f GB", float64(bytes)/_GB)
	case bytes >= _MB:
		return fmt.Sprintf("%.2f MB", float64(bytes)/_MB)
	case bytes >= _KB:
		return fmt.Sprintf("%.2f KB", float64(bytes)/_KB)
	default:
		return fmt.Sprintf("%d B", bytes)
	}
}
