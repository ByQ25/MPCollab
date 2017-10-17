using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MPCollab
{
    /// <summary>
    /// Logika interakcji dla klasy Komputer.xaml
    /// </summary>
    public partial class Komputer : UserControl
    {
        private bool OrangeOrWhite;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        public Komputer()
        {
            InitializeComponent();

            rect1.Fill = new SolidColorBrush(Colors.White);
            //System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
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
            
            
        }

    }
}
