using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GroundStation.ViewModels
{
    /// <summary>
    /// MVVM deseni için özellik değişim bildirimlerini (Property Change Notification) yöneten temel sınıf.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Özellik değerini günceller ve arayüze bildirim gönderir.
        /// </summary>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Arayüzü güncellemek için PropertyChanged olayını tetikler.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
