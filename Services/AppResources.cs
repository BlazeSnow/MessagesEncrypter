using ResourceLoader = Windows.ApplicationModel.Resources.ResourceLoader;
using ResourceManager = Microsoft.Windows.ApplicationModel.Resources.ResourceManager;

namespace MessagesEncrypter.Services;

internal static class AppResources
{
    private static readonly ResourceLoader Loader = ResourceLoader.GetForViewIndependentUse();

    public static string GetString(string key)
    {
        string value = Loader.GetString(key);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        int propertySeparatorIndex = key.LastIndexOf('.');
        if (propertySeparatorIndex > 0 && propertySeparatorIndex < key.Length - 1)
        {
            string propertyResourceKey = string.Concat(
                key.Substring(0, propertySeparatorIndex),
                "/",
                key.Substring(propertySeparatorIndex + 1));
            value = Loader.GetString(propertyResourceKey);
        }

        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static string GetString(string key, string language)
    {
        try
        {
            var resourceManager = new ResourceManager();
            var resourceMap = resourceManager.MainResourceMap
                .GetSubtree("MessagesEncrypter.Pages")
                .GetSubtree("Resources");
            var context = resourceManager.CreateResourceContext();
            context.QualifierValues["Language"] = language;
            return resourceMap.GetValue(key, context)?.ValueAsString ?? GetString(key);
        }
        catch
        {
            return GetString(key);
        }
    }
}
