namespace NetSuiteCertAuthTest;

public class NsFindIdsResponse
{
    public int count { get; set; }

    public List<Item> items { get; set; }

    public class Item
    {
        public string id { get; set; }
    }
}