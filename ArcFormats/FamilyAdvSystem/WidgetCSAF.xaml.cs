using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GameRes.Formats.Properties;
using GameRes.Formats.FamilyAdvSystem;
using GameRes.Formats.Strings;

namespace GameRes.Formats.GUI
{
    /// <summary>
    /// Interaction logic for WidgetCSAF.xaml
    /// </summary>
    public partial class WidgetCSAF : Grid
    {
        public WidgetCSAF (CsafOpener csaf)
        {
            InitializeComponent();
            Title.ItemsSource = csaf.KnownKeys.Keys.OrderBy (x => x);
        }
    }
}
