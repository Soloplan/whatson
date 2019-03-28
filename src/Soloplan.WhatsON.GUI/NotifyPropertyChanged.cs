namespace Soloplan.WhatsON.GUI
{
  using System.ComponentModel;
  using System.Runtime.CompilerServices;

  public class NotifyPropertyChanged : INotifyPropertyChanged
  {
    /// <summary>
    /// Occurs when property is changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Called when property is changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}