using Windows.ApplicationModel.Resources;

namespace MessagesEncrypter.Services;

internal static class AppResources
{
    private static readonly ResourceLoader Loader = ResourceLoader.GetForViewIndependentUse();

    public static string GetString(string key)
    {
        string value = Loader.GetString(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }
}
