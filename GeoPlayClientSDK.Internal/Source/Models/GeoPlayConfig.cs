namespace GeoPlayClientSDK.Internal.Models
{
    [System.Serializable]
    public class GeoPlayConfigRoot
    {
        public GeoPlayConfig geoplay_config;
    }

    [System.Serializable]
    public class GeoPlayConfig
    {
        public string project_id;
        public string api_key;
        public string base_url;
    }
}
