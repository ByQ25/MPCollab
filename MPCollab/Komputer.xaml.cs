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
        private double speed;
        public Komputer()
        {
            InitializeComponent();
            speed = 0.1;
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            test();
        }
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private void test()
        {
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (rect1.Opacity==1)
            {
                speed = -speed;
            }
            if (rect1.Opacity==0)
            {
                speed = -speed;
            }
            rect1.Opacity += speed;            
        }

    }
}
