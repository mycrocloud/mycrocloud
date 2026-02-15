package main

import (
	"os"
	"strconv"
)

const (
	MB = 1024 * 1024
	GB = 1024 * MB
)

// SystemLimits contains maximum limits enforced by the system (cannot be exceeded by plans)
type SystemLimits struct {
	// Maximum allowed values (security ceiling)
	MaxMemoryBytes   int64
	MaxCPUPercent    int
	MaxBuildDuration int // seconds
	MaxArtifactSize  int64

	// Fixed system limits
	ContainerPidsLimit int64
	MaxConcurrentJobs  int
}

// JobLimits contains effective limits for a specific job (from plan, capped by system)
type JobLimits struct {
	MemoryBytes      int64
	MemorySoftBytes  int64
	CPUQuota         int64 // microseconds per 100ms period
	CPUPeriod        int64
	PidsLimit        int64
	BuildDuration    int   // seconds
	MaxArtifactSize  int64
	WarnArtifactSize int64
}

// Limits contains all configurable limits for the worker
type Limits struct {
	System SystemLimits
	// Default job limits (used when plan doesn't specify)
	DefaultJob        JobLimits
	MaxConcurrentJobs int
}

// DefaultLimits returns sensible default limits
func DefaultLimits() Limits {
	return Limits{
		System: SystemLimits{
			// Maximum allowed by system (security ceiling)
			MaxMemoryBytes:   4 * GB,      // 4GB max
			MaxCPUPercent:    400,         // 4 CPUs max
			MaxBuildDuration: 3600,        // 1 hour max
			MaxArtifactSize:  1 * GB,      // 1GB max
			ContainerPidsLimit: 512,
			MaxConcurrentJobs:  3,
		},
		DefaultJob: JobLimits{
			// Default for free tier
			MemoryBytes:      1 * GB,
			MemorySoftBytes:  768 * MB,
			CPUQuota:         100000, // 100% = 1 CPU
			CPUPeriod:        100000,
			PidsLimit:        256,
			BuildDuration:    600,        // 10 min
			MaxArtifactSize:  100 * MB,
			WarnArtifactSize: 50 * MB,
		},
		MaxConcurrentJobs: 3,
	}
}

// LoadLimitsFromEnv loads limits from environment variables, falling back to defaults
func LoadLimitsFromEnv() Limits {
	l := DefaultLimits()

	// System max limits (can be increased for self-hosted)
	if v := os.Getenv("SYSTEM_MAX_MEMORY"); v != "" {
		if n, err := strconv.ParseInt(v, 10, 64); err == nil && n > 0 {
			l.System.MaxMemoryBytes = n
		}
	}
	if v := os.Getenv("SYSTEM_MAX_CPU_PERCENT"); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			l.System.MaxCPUPercent = n
		}
	}
	if v := os.Getenv("SYSTEM_MAX_BUILD_DURATION"); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			l.System.MaxBuildDuration = n
		}
	}
	if v := os.Getenv("SYSTEM_MAX_ARTIFACT_SIZE"); v != "" {
		if n, err := strconv.ParseInt(v, 10, 64); err == nil && n > 0 {
			l.System.MaxArtifactSize = n
		}
	}

	// Default job limits (used when plan doesn't specify)
	if v := os.Getenv("DEFAULT_MEMORY_LIMIT"); v != "" {
		if n, err := strconv.ParseInt(v, 10, 64); err == nil && n > 0 {
			l.DefaultJob.MemoryBytes = n
		}
	}
	if v := os.Getenv("DEFAULT_CPU_PERCENT"); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			l.DefaultJob.CPUQuota = int64(n) * 1000
		}
	}
	if v := os.Getenv("DEFAULT_BUILD_DURATION"); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			l.DefaultJob.BuildDuration = n
		}
	}
	if v := os.Getenv("DEFAULT_ARTIFACT_SIZE"); v != "" {
		if n, err := strconv.ParseInt(v, 10, 64); err == nil && n > 0 {
			l.DefaultJob.MaxArtifactSize = n
		}
	}

	if v := os.Getenv("MAX_CONCURRENT_JOBS"); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			l.MaxConcurrentJobs = n
		}
	}

	return l
}

// GetJobLimits returns effective limits for a job, applying plan limits capped by system max
func (l *Limits) GetJobLimits(planLimits *PlanLimits) JobLimits {
	job := l.DefaultJob

	if planLimits == nil {
		return job
	}

	// Apply plan limits, capped by system max
	if planLimits.MemoryMB > 0 {
		memBytes := int64(planLimits.MemoryMB) * MB
		if memBytes > l.System.MaxMemoryBytes {
			memBytes = l.System.MaxMemoryBytes
		}
		job.MemoryBytes = memBytes
		job.MemorySoftBytes = memBytes * 3 / 4 // 75% as soft limit
	}

	if planLimits.CPUPercent > 0 {
		cpuPercent := planLimits.CPUPercent
		if cpuPercent > l.System.MaxCPUPercent {
			cpuPercent = l.System.MaxCPUPercent
		}
		job.CPUQuota = int64(cpuPercent) * 1000 // convert to microseconds
	}

	if planLimits.BuildTimeoutS > 0 {
		timeout := planLimits.BuildTimeoutS
		if timeout > l.System.MaxBuildDuration {
			timeout = l.System.MaxBuildDuration
		}
		job.BuildDuration = timeout
	}

	if planLimits.ArtifactSizeMB > 0 {
		artifactBytes := int64(planLimits.ArtifactSizeMB) * MB
		if artifactBytes > l.System.MaxArtifactSize {
			artifactBytes = l.System.MaxArtifactSize
		}
		job.MaxArtifactSize = artifactBytes
		job.WarnArtifactSize = artifactBytes / 2 // 50% as warning
	}

	return job
}
