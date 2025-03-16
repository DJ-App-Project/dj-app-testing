namespace dj_api.ApiModels.Event.Post
{
    public class SetEnableUserRecommendationPost
    {
        public string EventId { get; set; } = null!;

        public bool EnableUserRecommendation { get; set; } = false;

    }
}
