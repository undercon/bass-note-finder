using System.Windows;
using System.Windows.Controls;

namespace BassNoteFinder.Views;

public partial class MenuView : UserControl
{
    public event Action? TeacherModeSelected;
    public event Action? StudentModeSelected;

    public MenuView()
    {
        InitializeComponent();
    }

    private void TeacherModeButton_Click(object sender, RoutedEventArgs e)
    {
        TeacherModeSelected?.Invoke();
    }

    private void StudentModeButton_Click(object sender, RoutedEventArgs e)
    {
        StudentModeSelected?.Invoke();
    }
}
