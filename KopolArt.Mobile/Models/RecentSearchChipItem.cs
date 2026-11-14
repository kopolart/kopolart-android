using System.Windows.Input;

namespace KopolArt.Mobile.Models
{
    /// <summary>
    /// Represents a single recent search chip item with self-contained query and interaction commands.
    /// </summary>
    public class RecentSearchChipItem
    {
        public string Query { get; set; } = string.Empty;
        public ICommand? SelectCommand { get; set; }
        public ICommand? RemoveCommand { get; set; }
    }
}
