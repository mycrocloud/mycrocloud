import { Plugin } from "vite";

/**
 * Options for the `injectScriptTag` Vite plugin.
 */
interface InjectScriptTagOptions {
  /**
   * The script `src` path to inject (e.g. `"/_config.js"`).
   * A `?v=<version>` cache-busting query string is appended automatically.
   */
  src: string;

  /**
   * Ordered list of environment variable names used to derive the version string.
   * The first env var that is set wins. `COMMIT_HASH` is automatically shortened
   * to 7 characters. Falls back to `Date.now()` if none are set.
   *
   * @example ["COMMIT_HASH", "BUILD_ID"]
   */
  versionEnvVars?: string[];
}

/**
 * Vite plugin that injects a `<script>` tag into `index.html` immediately after
 * `</title>` during production builds.
 *
 * Useful for loading runtime configuration files (e.g. `/_config.js`) that are
 * served by the web server and cannot be bundled at build time. A cache-busting
 * version query string is derived from env vars or `Date.now()`.
 *
 * Only runs during `build` (not `serve`).
 *
 * @example
 * // vite.config.ts
 * injectScriptTag({ src: "/_config.js", versionEnvVars: ["COMMIT_HASH", "BUILD_ID"] })
 *
 * // Produces:
 * // <script src="/_config.js?v=a1b2c3d"></script>
 */
export function injectScriptTag(options: InjectScriptTagOptions): Plugin {
  const { src, versionEnvVars = [] } = options;

  return {
    name: "inject-script-tag",
    apply: "build",
    transformIndexHtml(html) {
      const version =
        versionEnvVars.reduce<string | undefined>((acc, key) => {
          if (acc) return acc;
          const val = process.env[key];
          return key === "COMMIT_HASH" ? val?.slice(0, 7) : val;
        }, undefined) ?? String(Date.now());

      return html.replace(
        "</title>",
        `</title>\n\t\t<script src="${src}?v=${version}"></script>`,
      );
    },
  };
}
