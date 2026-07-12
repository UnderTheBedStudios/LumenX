using System.ComponentModel;
using System.Runtime.Serialization;

namespace LumenX
{
    [DataContract(IsReference = true)]
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}