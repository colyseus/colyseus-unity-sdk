using System;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Colyseus.Editor
{
    /// <summary>
    /// Paths, settings files and preference keys shared by the editor tools.
    ///
    /// Settings are committed to git, so paths are stored relative to the project
    /// root with forward slashes — "../server" resolves on every teammate's
    /// machine, "/Users/someone/server" does not.
    /// </summary>
    internal static class ProjectPaths
    {
        private static string _projectRoot;
        private static string _projectHash;

        // Cached: Application.dataPath is a native call that allocates each time,
        // and these are read on every repaint. Main thread only.
        public static string ProjectRoot =>
            _projectRoot ?? (_projectRoot = Directory.GetParent(Application.dataPath).FullName);

        /// <summary>
        /// An EditorPrefs key scoped to this project, so two projects on one
        /// machine don't share a Node directory or a running-server record.
        /// </summary>
        public static string PrefKey(string scope, string name)
        {
            if (_projectHash == null) _projectHash = Application.dataPath.GetHashCode().ToString("X8");
            return $"Colyseus.{scope}.{_projectHash}.{name}";
        }

        // ------------------------------------------------------------------
        // Committed settings files
        // ------------------------------------------------------------------

        public static string SettingsFile(string fileName) => Path.Combine(
            ProjectRoot, "ProjectSettings", "Packages", "io.colyseus.sdk", fileName);

        public static T LoadJson<T>(string fileName) where T : new()
        {
            try
            {
                var path = SettingsFile(fileName);
                if (File.Exists(path))
                {
                    var data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                    if (data != null) return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Colyseus] Failed to read {fileName}: {ex.Message}");
            }
            return new T();
        }

        /// <summary>
        /// Writes <paramref name="data"/>, except that an unconfigured object never
        /// creates the file — nobody wants an empty settings file in their commit.
        /// </summary>
        public static void SaveJson(string fileName, object data, bool isEmpty)
        {
            try
            {
                var path = SettingsFile(fileName);
                if (isEmpty && !File.Exists(path)) return;

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Colyseus] Failed to write {fileName}: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Stored (project-relative) paths
        // ------------------------------------------------------------------

        /// <summary>Absolute path to the project-root-relative form written to disk.</summary>
        public static string ToStored(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string full;
            try { full = Path.GetFullPath(path); }
            catch { return path; }
            return MakeRelative(ProjectRoot, full).Replace('\\', '/');
        }

        /// <summary>The stored form back to an absolute path on this machine.</summary>
        public static string FromStored(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            var p = stored.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(p)) return p;
            try { return Path.GetFullPath(Path.Combine(ProjectRoot, p)); }
            catch { return p; }
        }

        private static string MakeRelative(string fromDir, string toPath)
        {
            try
            {
                var fromUri = new Uri(AppendSlash(fromDir));
                var toUri = new Uri(toPath);
                if (fromUri.Scheme != toUri.Scheme) return toPath;
                var rel = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
                if (string.IsNullOrEmpty(rel)) return toPath;
                // A different drive yields an absolute "file:///D:/..." URI rather
                // than a relative path; keep the original absolute path instead.
                if (rel.IndexOf("://", StringComparison.Ordinal) >= 0 || Path.IsPathRooted(rel)) return toPath;
                return rel;
            }
            catch { return toPath; }
        }

        private static string AppendSlash(string dir)
        {
            return dir.EndsWith(Path.DirectorySeparatorChar.ToString()) || dir.EndsWith("/")
                ? dir : dir + Path.DirectorySeparatorChar;
        }

        // ------------------------------------------------------------------
        // Misc
        // ------------------------------------------------------------------

        public static bool IsUnderAssets(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                var full = Path.GetFullPath(path).Replace('\\', '/');
                var assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
                return full.StartsWith(assets, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>A sensible directory to open a file panel at.</summary>
        public static string StartDir(string path)
        {
            if (string.IsNullOrEmpty(path)) return Application.dataPath;
            if (Directory.Exists(path)) return path;
            try
            {
                var dir = Path.GetDirectoryName(path);
                return Directory.Exists(dir) ? dir : Application.dataPath;
            }
            catch { return Application.dataPath; }
        }

        public static bool HasPackageJson(string dir) =>
            !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "package.json"));

        /// <summary>
        /// Walks up from <paramref name="start"/> to the nearest directory holding a
        /// package.json, so npm/npx resolve the project's own install.
        /// </summary>
        public static string NearestPackageRoot(string start, int maxDepth = 8)
        {
            if (string.IsNullOrEmpty(start)) return null;
            var probe = Directory.Exists(start) ? start : Path.GetDirectoryName(start);
            for (var i = 0; i < maxDepth && !string.IsNullOrEmpty(probe); i++)
            {
                if (HasPackageJson(probe)) return probe;
                probe = Path.GetDirectoryName(probe);
            }
            return null;
        }

        /// <summary>Shortens an absolute path for display, relative to the project root.</summary>
        public static string ForDisplay(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            var relative = ToStored(path);
            return relative.Length > 0 && relative.Length < path.Length ? relative : path;
        }
    }
}
