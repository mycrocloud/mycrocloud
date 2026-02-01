import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function generateAppName() {
  const adjectives = ["swift", "bright", "cool", "fast", "smart", "quick", "bold", "calm"];
  const nouns = ["app", "api", "hub", "lab", "box", "cloud", "dev", "io"];
  const adj = adjectives[Math.floor(Math.random() * adjectives.length)];
  const noun = nouns[Math.floor(Math.random() * nouns.length)];
  const suffix = Math.random().toString(36).substring(2, 6);
  return `${adj}-${noun}-${suffix}`;
}
