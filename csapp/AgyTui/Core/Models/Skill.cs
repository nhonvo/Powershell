namespace AgyTui.Core.Models;

public sealed record Skill(string Name, string Description, string Trigger, SkillStep[] Steps);
public sealed record SkillStep(string Primitive, string Arg);
