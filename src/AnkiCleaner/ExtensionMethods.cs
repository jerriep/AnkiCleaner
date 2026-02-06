namespace AnkiCleaner;

public static class ExtensionMethods
{
    extension(string[] list)
    {
        public string AsBulletedList()
        {
            return string.Join("<br>", list.Select(x => $"- {x}"));
        }

        public string AsHtmlUnorderedList()
        {
            return $"<ul>{string.Join("", list.Select(x => $"<li>{x}</li>"))}</ul>";
        }
    }
}
