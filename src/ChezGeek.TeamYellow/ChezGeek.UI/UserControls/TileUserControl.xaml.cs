using ChezGeek.UI.ViewModels;

namespace ChezGeek.UI.UserControls
{
    /// <summary>
    /// Interaction logic for TileUserControl.xaml
    /// </summary>
    public partial class TileUserControl
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
