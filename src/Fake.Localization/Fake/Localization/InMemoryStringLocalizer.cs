using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Fake.Localization;

public class InMemoryStringLocalizer : IFakeStringLocalizer
{
    private readonly AbstractLocalizationResource _resource;
    private readonly List<IStringLocalizer> _inheritsLocalizer;
    private readonly FakeLocalizationOptions _options;
    
    public virtual LocalizedString this[string name] => GetLocalizedString(name, CultureInfo.CurrentCulture.Name);

    public virtual LocalizedString this[string name, params object[] arguments] =>
        GetLocalizedStringFormatted(name, CultureInfo.CurrentCulture.Name, arguments);

    public InMemoryStringLocalizer(AbstractLocalizationResource resource,List<IStringLocalizer> inheritsLocalizer,FakeLocalizationOptions options)
    {
        _resource = resource;
        _inheritsLocalizer = inheritsLocalizer;
        _options = options;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return GetAllStrings(CultureInfo.CurrentCulture.Name, includeParentCultures);
    }

    protected virtual LocalizedString GetLocalizedStringFormatted(string name, string cultureName, params object[] arguments)
    {
        var localizedString = GetLocalizedString(name, cultureName);
        return new LocalizedString(name, string.Format(localizedString.Value, arguments),
            localizedString.ResourceNotFound, localizedString.SearchedLocation);
    }
    protected virtual LocalizedString GetLocalizedString(string name, string cultureName)
    {
        var localizedString = _resource.GetOrNull(cultureName, name);
        if (localizedString != null) return localizedString;

        if (_options.TryGetFromParentCulture)
        {
            if (cultureName.Contains("-"))
            {
                localizedString = _resource.GetOrNull(CultureHelper.GetParentCultureName(cultureName), name);
                if (localizedString != null) return localizedString;
            }
        }

        if (_options.TryGetFromDefaultCulture)
        {
            if (_resource.DefaultCultureName.NotBeNullOrWhiteSpace())
            {
                localizedString = _resource.GetOrNull(_resource.DefaultCultureName, name);
                if (localizedString != null) return localizedString;
            }
        }

        
        // ??????????????????????????????????????????
        foreach (var stringLocalizer in _inheritsLocalizer)
        {
            using (CultureHelper.UseCulture(cultureName))
            {
                if (!stringLocalizer[name].ResourceNotFound)
                {
                    return stringLocalizer[name];
                }
            }
        }

        return new LocalizedString(name, name, resourceNotFound: true);
    }


    protected virtual IReadOnlyList<LocalizedString> GetAllStrings(string cultureName,
        bool includeParentCultures = true, bool includeInheritLocalizers = true)
    {
        var allStrings = new Dictionary<string, LocalizedString>();

        // ???????????????strings
        if (includeInheritLocalizers)
        {
            foreach (var localizer in _inheritsLocalizer)
            {
                using (CultureHelper.UseCulture(cultureName))
                {
                    var strings = localizer.GetAllStrings(includeParentCultures, true);

                    foreach (var localizedString in strings)
                    {
                        allStrings[localizedString.Name] = localizedString;
                    }
                }
            }
        }

        if (includeParentCultures)
        {
            // ????????????culture strings
            if (_resource.DefaultCultureName.NotBeNullOrWhiteSpace())
            {
                _resource.Fill(_resource.DefaultCultureName, allStrings);

            }

            // ??????parent culture strings
            if (cultureName.Contains("-"))
            {
                _resource.Fill(CultureHelper.GetParentCultureName(cultureName), allStrings);
            }
        }
        
        _resource.Fill(cultureName, allStrings);

        return allStrings.Values.ToImmutableList();
    }
}