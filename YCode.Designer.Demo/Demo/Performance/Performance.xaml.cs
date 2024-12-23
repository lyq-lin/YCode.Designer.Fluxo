using System.Diagnostics;
using System.Windows.Controls;
using YCode.Designer.Fluxo;

namespace YCode.Designer.Demo;

public partial class Performance : UserControl
{
    public Performance()
    {
        InitializeComponent();
    }
    
    private void OnMounting(object? sender, FluxoMountingEventArgs e)
    {
        Debug.WriteLine($"{nameof(OnMounting)} Trigger...");
    }

    private void OnMounted(object? sender, FluxoMountedEventArgs e)
    {
        Debug.WriteLine($"{nameof(OnMounted)} Trigger...");
    }
}