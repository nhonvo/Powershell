namespace AgyTui.Tests.Unit.Architecture;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

public class ArchitectureTests
{
    [Fact]
    public void Infrastructure_Namespace_DoesNotReferenceUI_Namespace()
    {
        var infraAssembly = typeof(AgyAccountStore).Assembly;

        var infraTypes = infraAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("AgyTui.Infrastructure"))
            .ToList();

        var invalidReferences = new List<string>();

        foreach (var type in infraTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var param in method.GetParameters())
                {
                    if (param.ParameterType.Namespace != null && param.ParameterType.Namespace.StartsWith("AgyTui.UI"))
                    {
                        invalidReferences.Add($"{type.FullName}.{method.Name}({param.Name}) -> {param.ParameterType.FullName}");
                    }
                }
                if (method.ReturnType.Namespace != null && method.ReturnType.Namespace.StartsWith("AgyTui.UI"))
                {
                    invalidReferences.Add($"{type.FullName}.{method.Name}() -> {method.ReturnType.FullName}");
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType.Namespace != null && field.FieldType.Namespace.StartsWith("AgyTui.UI"))
                {
                    invalidReferences.Add($"{type.FullName}.{field.Name} -> {field.FieldType.FullName}");
                }
            }
        }

        Assert.True(invalidReferences.Count == 0, "Found invalid references from Infrastructure to UI:\n" + string.Join("\n", invalidReferences));
    }
}
