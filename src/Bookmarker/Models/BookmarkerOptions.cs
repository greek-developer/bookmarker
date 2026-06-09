public class BookmarkerOptions
{
    public BookmarkerTabsOptions[] Tabs { get; set; } = Array.Empty<BookmarkerTabsOptions>();    
}

public class BookmarkerTabsOptions
{
    public string Name { get; set; } = "";
    public string[] Files { get; set; } = Array.Empty<string>();
}