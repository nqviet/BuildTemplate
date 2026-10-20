using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Laboon.Mobile.Build
{
    /// <summary>
    /// Validates project Gradle templates (kept by the project for Firebase/EDM4U). Never writes them.
    /// Missing templates are fine: Unity then uses its own defaults.
    /// </summary>
    public static class GradleTemplateValidator
    {
        public const string MainTemplatePath = "Assets/Plugins/Android/mainTemplate.gradle";
        public const string SettingsTemplatePath = "Assets/Plugins/Android/settingsTemplate.gradle";

        public static readonly string[] RequiredMainTokens = { "**APPLY_PLUGINS**", "**DEPS**", "**VERSIONCODE**", "**VERSIONNAME**" };
        public static readonly string[] RequiredSettingsTokens = { "**INCLUDES**", "google()", "mavenCentral()" };

        /// <summary>Validates template text. Returns one message per missing token.</summary>
        public static List<string> ValidateText(string fileName, string text, IEnumerable<string> requiredTokens)
        {
            return requiredTokens.Where(token => text == null || !text.Contains(token))
                .Select(token => $"{fileName} is missing '{token}'. Restore it from Unity's default template " +
                    "(Project Settings > Player > Android > Publishing Settings) and re-add project changes.")
                .ToList();
        }

        /// <summary>Validates the templates present under <paramref name="projectRoot"/>.</summary>
        public static List<string> Validate(string projectRoot = null)
        {
            var errors = new List<string>();
            Check(projectRoot, MainTemplatePath, RequiredMainTokens, errors);
            Check(projectRoot, SettingsTemplatePath, RequiredSettingsTokens, errors);
            return errors;
        }

        public static void ThrowIfInvalid(string projectRoot = null)
        {
            var errors = Validate(projectRoot);
            if (errors.Count > 0)
                throw new InvalidOperationException("Gradle template validation failed:\n- " + string.Join("\n- ", errors));
        }

        private static void Check(string root, string relative, IEnumerable<string> tokens, List<string> errors)
        {
            string path = string.IsNullOrEmpty(root) ? relative : Path.Combine(root, relative);
            if (!File.Exists(path)) return;
            errors.AddRange(ValidateText(Path.GetFileName(relative), File.ReadAllText(path), tokens));
        }
    }
}
