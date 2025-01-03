namespace App.Models;

public class Item
{
    public string? Title { get; set; }
    public string? Url { get; set; }
}
public class Breadcrumb
{
    public List<Item> Items { get; set; } = new();
}