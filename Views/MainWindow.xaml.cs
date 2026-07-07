using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GroundStation.ViewModels;

namespace GroundStation.Views
{
    public partial class MainWindow : Window
    {
        private GMapMarker _rocketMarker;
        private GMapRoute _rocketRoute;
        
        private GMapMarker _payloadMarker;
        private GMapRoute _payloadRoute;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMap();
            
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void InitializeMap()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
                GMapProvider.WebProxy = null;
                GMapProvider.UserAgent = "AkayGroundStation/1.0";
                GMaps.Instance.Mode = AccessMode.ServerAndCache;
                
                // --- ROCKET MAP ---
                RocketMap.MapProvider = GMapProviders.BingHybridMap; 
                RocketMap.DragButton = System.Windows.Input.MouseButton.Left;
                RocketMap.ShowCenter = false;
                RocketMap.Position = new PointLatLng(38.3686, 33.7225);

                _rocketRoute = new GMapRoute(new System.Collections.Generic.List<PointLatLng>())
                {
                    Shape = new Path { Stroke = Brushes.Cyan, StrokeThickness = 3 }
                };
                RocketMap.Markers.Add(_rocketRoute);

                _rocketMarker = new GMapMarker(RocketMap.Position)
                {
                    Shape = new Ellipse { Width = 14, Height = 14, Fill = Brushes.Red, Stroke = Brushes.Black, StrokeThickness = 2, ToolTip = "ROCKET" },
                    Offset = new Point(-7, -7)
                };
                RocketMap.Markers.Add(_rocketMarker);

                // --- PAYLOAD MAP ---
                PayloadMap.MapProvider = GMapProviders.BingHybridMap; 
                PayloadMap.DragButton = System.Windows.Input.MouseButton.Left;
                PayloadMap.ShowCenter = false;
                PayloadMap.Position = new PointLatLng(38.3686, 33.7225);

                _payloadRoute = new GMapRoute(new System.Collections.Generic.List<PointLatLng>())
                {
                    Shape = new Path { Stroke = Brushes.Magenta, StrokeThickness = 3 }
                };
                PayloadMap.Markers.Add(_payloadRoute);

                _payloadMarker = new GMapMarker(PayloadMap.Position)
                {
                    Shape = new Ellipse { Width = 14, Height = 14, Fill = Brushes.Yellow, Stroke = Brushes.Black, StrokeThickness = 2, ToolTip = "PAYLOAD" },
                    Offset = new Point(-7, -7)
                };
                PayloadMap.Markers.Add(_payloadMarker);
            }
            catch { }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MainWindowViewModel vm)
            {
                if (e.PropertyName == nameof(vm.RocketLatitude) || e.PropertyName == nameof(vm.RocketLongitude))
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (_rocketMarker != null && _rocketRoute != null)
                        {
                            var newPos = new PointLatLng(vm.RocketLatitude, vm.RocketLongitude);
                            _rocketMarker.Position = newPos;
                            _rocketRoute.Points.Add(newPos);
                            RocketMap.Markers.Remove(_rocketRoute);
                            RocketMap.Markers.Add(_rocketRoute);
                            if (vm.IsAutoFollowEnabled)
                            {
                                RocketMap.Position = newPos; 
                            }
                        }
                    });
                }
                else if (e.PropertyName == nameof(vm.PayloadLatitude) || e.PropertyName == nameof(vm.PayloadLongitude))
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (_payloadMarker != null && _payloadRoute != null)
                        {
                            var newPos = new PointLatLng(vm.PayloadLatitude, vm.PayloadLongitude);
                            _payloadMarker.Position = newPos;
                            _payloadRoute.Points.Add(newPos);
                            PayloadMap.Markers.Remove(_payloadRoute);
                            PayloadMap.Markers.Add(_payloadRoute);
                            if (vm.IsAutoFollowEnabled)
                            {
                                PayloadMap.Position = newPos; 
                            }
                        }
                    });
                }
            }
        }
    }
}
