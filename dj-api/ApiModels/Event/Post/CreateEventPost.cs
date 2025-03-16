namespace dj_api.ApiModels.Event.Post
{
    public class CreateEventPost
    {
        

        

        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime Date { get; set; } = new DateTime()!;
        public string Location { get; set; } = null!;
        public bool Active { get; set; } = false;
    }
}
