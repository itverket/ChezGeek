using Geek2k16.UI.ViewModels;
using System.Windows.Controls;

namespace Geek2k16.UI.UserControls
{
    /// <summary>
    /// Interaction logic for TileUserControl.xaml
    /// </summary>
    public partial class TileUserControl : UserControl
    {
        private Tile _viewModel;

        public TileUserControl()
        {
            InitializeComponent();
        }

        public Tile ViewModel
        {
            get { return _viewModel; }
            set
            {
                _viewModel = value;

                DataContext = value;
            }
        }
    }
}
