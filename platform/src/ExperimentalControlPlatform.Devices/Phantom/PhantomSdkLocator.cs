using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExperimentalControlPlatform.Devices.Phantom;

public interface IPhantomSdkLocator
{
    bool TryLocate(out PhantomSdkInstall? install);
}

public sealed class PhantomSdkLocator : IPhantomSdkLocator
{
    private static readonly string[] DefaultCandidateRoots =
    [
        @"C:\Program Files\Phantom",
    ];

    private readonly IReadOnlyList<string> _candidateRoots;

    public PhantomSdkLocator(IEnumerable<string>? candidateRoots = null)
    {
        _candidateRoots = (candidateRoots ?? DefaultCandidateRoots)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryLocate(out PhantomSdkInstall? install)
    {
        foreach (var root in _candidateRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var managedAssemblyPath = Path.Combine(root, "PhSharp.Dll");
            if (File.Exists(managedAssemblyPath))
            {
                install = new PhantomSdkInstall(root, managedAssemblyPath);
                return true;
            }
        }

        install = null;
        return false;
    }
}
