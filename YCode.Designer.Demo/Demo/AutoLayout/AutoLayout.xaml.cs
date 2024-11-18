using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using YCode.Designer.Fluxo;

namespace YCode.Designer.Demo;

public partial class AutoLayout : UserControl
{
    public AutoLayout()
    {
        InitializeComponent();
    }

    private void OnAutoLayoutClick(object sender, RoutedEventArgs e)
    {
        Designer.AutoLayout();
    }
}