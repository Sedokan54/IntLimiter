using System.Windows;
using NetLimiterClone.ViewModels;

namespace NetLimiterClone.Views
{
    public partial class BandwidthLimitDialog : Window
    {
        public BandwidthLimitDialog(BandwidthLimitViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            viewModel.CloseRequested += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}