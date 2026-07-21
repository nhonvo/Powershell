using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgyTui;

public sealed record Skill(string Name, string Description, string Trigger, SkillStep[] Steps);
public sealed record SkillStep(string Primitive, string Arg);

public static class SkillLoader
{
    public static IEnumerable<Skill> Discover(string workspacePath)
    {
        var dirs = new List<string> {
            Path.Combine(workspacePath, "skills"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agy", "skills")
        };
        
        foreach (var dir in dirs.Where(Directory.Exists))
        {
            foreach (var file in Directory.GetFiles(dir, "*.md"))
            {
                var skill = ParseSkillFile(file);
                if (skill != null) yield return skill;
            }
        }
    }

    public static Skill? ParseSkillFile(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0 || lines[0] != "---") return null;
            
            var fm = lines.Skip(1).TakeWhile(l => l != "---").ToArray();
            string GetField(string key) => fm.FirstOrDefault(l => l.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))?.Split(':', 2)[1].Trim() ?? "";
            
            var name = GetField("name");
            var description = GetField("description");
            var trigger = GetField("trigger");
            
            if (string.IsNullOrEmpty(name)) name = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrEmpty(trigger)) trigger = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
            
            var steps = new List<SkillStep>();
            bool inSteps = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("steps:", StringComparison.OrdinalIgnoreCase))
                {
                    inSteps = true;
                    continue;
                }
                if (inSteps)
                {
                    if (line.StartsWith("---")) break;
                    if (line.TrimStart().StartsWith("-"))
                    {
                        var parts = line.TrimStart('-', ' ').Split(':', 2);
                        if (parts.Length > 0)
                        {
                            var primitive = parts[0].Trim();
                            var arg = parts.Length > 1 ? parts[1].Trim() : "";
                            steps.Add(new SkillStep(primitive, arg));
                        }
                    }
                }
            }
            
            return new Skill(name, description, trigger, steps.ToArray());
        }
        catch
        {
            return null;
        }
    }
}
