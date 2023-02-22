namespace scraper.salamnews.org
{
    public class Read
    {
        public string Url { get; }
        public string Category { get; }
        public string Title { get; }
        public string Date { get; }
        public string Body { get; }
        public string Image { get; }
        public Read(string url, string category, string title, string date, string body, string image)
        {
            this.Url = url;
            this.Category = category;
            this.Title = title;
            this.Date = date;
            this.Body = body;
            this.Image = image;
        }
    }
}
