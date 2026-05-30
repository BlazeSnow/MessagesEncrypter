using Windows.ApplicationModel.Resources;

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
}
