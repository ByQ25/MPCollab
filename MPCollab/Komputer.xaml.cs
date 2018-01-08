using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MPCollab
{
    /// <summary>
    /// Logika interakcji dla klasy Komputer.xaml
    /// </summary>
    public partial class Komputer : UserControl
    {
        private bool OrangeOrWhite;
        private Random rand;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;

        public Komputer()
        {
            InitializeComponent();

            rect1.Fill = new SolidColorBrush(Colors.White);
            rand = new Random();
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
        
        public void Start()
        {
            dispatcherTimer.Start();
            OrangeOrWhite = false;
        }

        public void Stop()
        {
            dispatcherTimer.Stop();
            rect1.Fill = new SolidColorBrush(Colors.White);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (OrangeOrWhite)
            {
                rect1.Fill = new SolidColorBrush(Colors.Orange);
                OrangeOrWhite = false;
            }
            else
            {
                rect1.Fill = new SolidColorBrush(Colors.White);
                OrangeOrWhite = true;
            }
            dispatcherTimer.Stop();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, rand.Next(500, 4001));
            dispatcherTimer.Start();
        }
    }
}
