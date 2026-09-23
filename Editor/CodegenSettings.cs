using System;

namespace Colyseus.Editor
{
    /// <summary>
    /// Schema codegen configuration, committed to git so the whole team generates
    /// with the same source, output, namespace and version.
    ///
    /// The Node bin directory is deliberately not here: it is specific to each
    /// developer's install and lives in EditorPrefs (see <see cref="NodeLocator"/>).
    /// </summary>
    [Serializable]
    internal class CodegenSettings
    {
        public string source = "";
        public string output = "";
        public string @namespace = "";
        public string version = "";
        public bool bundle = false;

        public const string FileName = "SchemaCodegen.json";

        public static string FilePath => ProjectPaths.SettingsFile(FileName);

        public string ResolvedSource => ProjectPaths.FromStored(source);
        public string ResolvedOutput => ProjectPaths.FromStored(output);

        public bool IsConfigured => !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(output);

        private bool IsEmpty =>
            string.IsNullOrEmpty(source) && string.IsNullOrEmpty(output) &&
            string.IsNullOrEmpty(@namespace) && string.IsNullOrEmpty(version) && !bundle;

        public static CodegenSettings Load() => ProjectPaths.LoadJson<CodegenSettings>(FileName);

        public void Save() => ProjectPaths.SaveJson(FileName, this, IsEmpty);

        public void SetSource(string absolutePath) => source = ProjectPaths.ToStored(absolutePath);
        public void SetOutput(string absolutePath) => output = ProjectPaths.ToStored(absolutePath);
    }
}
