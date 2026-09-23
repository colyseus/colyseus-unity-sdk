using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameDevWare.Serialization;

namespace Colyseus.Editor
{
    /// <summary>
    /// Where and how to run the project's game server. Committed to git (paths are
    /// project-root relative) so everyone on the team gets the same Start button.
    /// </summary>
    [Serializable]
    internal class ServerSettings
    {
        public string workingDir = "";
        public string command = "";
        public int port = 2567;
        public bool autoStartOnPlay = false;

        private const string FileName = "Server.json";

        /// <summary>Absolute working directory on this machine.</summary>
        public string ResolvedWorkingDir => ProjectPaths.FromStored(workingDir);

        public bool IsConfigured =>
            !string.IsNullOrEmpty(workingDir) && !string.IsNullOrEmpty(command);

        public static ServerSettings Load() => ProjectPaths.LoadJson<ServerSettings>(FileName);

        public void Save() => ProjectPaths.SaveJson(FileName, this, !IsConfigured);

        public void SetWorkingDir(string absolutePath) => workingDir = ProjectPaths.ToStored(absolutePath);
    }

    /// <summary>
    /// What a server project's package.json says, read and parsed once.
    /// </summary>
    internal sealed class PackageInfo
    {
        /// <summary>"npm", "pnpm", "yarn" or "bun".</summary>
        public string Manager = "npm";

        /// <summary>Script names, sorted.</summary>
        public readonly List<string> Scripts = new List<string>();

        public bool DependsOnColyseus;

        /// <summary>The command to start the server, or null when no script fits.</summary>
        public string DefaultCommand =>
            Scripts.Contains("dev") ? RunScript("dev")
            : Scripts.Contains("start") ? RunScript("start")
            : null;

        public string InstallCommand => Manager == "yarn" ? "yarn" : Manager + " install";

        public string RunScript(string script)
        {
            switch (Manager)
            {
                // npm and bun need the `run` verb for arbitrary scripts.
                case "pnpm": return "pnpm " + script;
                case "yarn": return "yarn " + script;
                case "bun": return "bun run " + script;
                default: return script == "start" ? "npm start" : "npm run " + script;
            }
        }

        /// <summary>Reads <paramref name="dir"/>/package.json, or null when there is none.</summary>
        public static PackageInfo Read(string dir)
        {
            if (!ProjectPaths.HasPackageJson(dir)) return null;

            var info = new PackageInfo();
            IDictionary root = null;
            try
            {
                var text = File.ReadAllText(Path.Combine(dir, "package.json"));
                info.DependsOnColyseus = text.IndexOf("\"colyseus\"", StringComparison.OrdinalIgnoreCase) >= 0;
                root = Json.Deserialize(typeof(Dictionary<string, object>), text) as IDictionary;
            }
            catch { /* unreadable or malformed: fall back to defaults */ }

            if (root != null && root.Contains("scripts") && root["scripts"] is IDictionary scripts)
            {
                foreach (DictionaryEntry entry in scripts)
                {
                    if (entry.Key is string name && name.Length > 0) info.Scripts.Add(name);
                }
                info.Scripts.Sort(StringComparer.OrdinalIgnoreCase);
            }

            info.Manager = DetectManager(dir, root != null && root.Contains("packageManager")
                ? root["packageManager"] as string
                : null);
            return info;
        }

        /// <summary>An explicit "packageManager" field beats lockfile guessing.</summary>
        private static string DetectManager(string dir, string declared)
        {
            if (!string.IsNullOrEmpty(declared))
            {
                var at = declared.IndexOf('@');
                var name = (at > 0 ? declared.Substring(0, at) : declared).Trim();
                if (name == "pnpm" || name == "yarn" || name == "bun" || name == "npm") return name;
            }

            if (File.Exists(Path.Combine(dir, "pnpm-lock.yaml"))) return "pnpm";
            if (File.Exists(Path.Combine(dir, "yarn.lock"))) return "yarn";
            if (File.Exists(Path.Combine(dir, "bun.lock")) || File.Exists(Path.Combine(dir, "bun.lockb"))) return "bun";
            return "npm";
        }
    }

    /// <summary>Finds the server project next to the Unity project.</summary>
    internal static class ServerProject
    {
        /// <summary>
        /// Likely locations for a Colyseus server, best first. A project that
        /// depends on colyseus wins over one that merely has a package.json.
        /// </summary>
        public static string DetectWorkingDir()
        {
            var root = ProjectPaths.ProjectRoot;
            var parent = Directory.GetParent(root)?.FullName;
            var projectName = new DirectoryInfo(root).Name;

            var candidates = new List<string>();

            // The schema source already points into the server project, so it is
            // the strongest signal we have.
            var packageRoot = ProjectPaths.NearestPackageRoot(CodegenSettings.Load().ResolvedSource);
            if (!string.IsNullOrEmpty(packageRoot)) candidates.Add(packageRoot);

            candidates.Add(Path.Combine(root, "server"));
            candidates.Add(Path.Combine(root, "Server"));
            if (!string.IsNullOrEmpty(parent))
            {
                candidates.Add(Path.Combine(parent, "server"));
                candidates.Add(Path.Combine(parent, "Server"));
                candidates.Add(Path.Combine(parent, projectName + "-server"));
            }

            string firstWithPackage = null;
            foreach (var candidate in candidates)
            {
                var info = PackageInfo.Read(candidate);
                if (info == null) continue;
                if (info.DependsOnColyseus) return candidate;
                if (firstWithPackage == null) firstWithPackage = candidate;
            }

            return firstWithPackage;
        }

        public static bool HasNodeModules(string dir) =>
            !string.IsNullOrEmpty(dir) && Directory.Exists(Path.Combine(dir, "node_modules"));
    }
}
