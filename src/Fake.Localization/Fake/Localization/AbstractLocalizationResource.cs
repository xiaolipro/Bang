using System.Collections.Generic;
using System.Linq;
using Fake.Localization.Contributors;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Fake.Localization;

public abstract class AbstractLocalizationResource
{
    [NotNull] 
    public string ResourceName { get; }
    [CanBeNull]
    public string DefaultCultureName { get; set; }
    public List<string> BaseResourceNames { get; }
    
    [NotNull]
    public List<ILocalizationResourceContributor> Contributors { get; }
    public AbstractLocalizationResource(
        [NotNull] string resourceName,
        [CanBeNull] string defaultCultureName = null)
    {
        ThrowHelper.ThrowIfNull(resourceName, nameof(resourceName));
        ResourceName = resourceName;
        DefaultCultureName = defaultCultureName;
        
        BaseResourceNames = new();
        Contributors = new();
    }
    
    public void Fill(
        string cultureName, 
        Dictionary<string, LocalizedString> dictionary)
    {
        foreach (var contributor in Contributors)
        {
            contributor.Fill(cultureName, dictionary);
        }
    }

    public LocalizedString GetOrNull(string cultureName, string name)
    {
        // εθδΌε
        foreach (var contributor in Contributors.Select(x => x).Reverse())
        {
            var localizedString = contributor.GetOrNull(cultureName, name);

            if (localizedString != null) return localizedString;
        }

        return null;
    }
}